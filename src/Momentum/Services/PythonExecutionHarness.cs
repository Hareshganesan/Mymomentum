using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Momentum.Models;

namespace Momentum.Services
{
    public class PythonExecutionHarness
    {
        private static string? _cachedPythonPath;

        public static string? FindPythonExecutable()
        {
            if (!string.IsNullOrEmpty(_cachedPythonPath) && File.Exists(_cachedPythonPath))
                return _cachedPythonPath;

            // 1. Check PATH
            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var paths = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in paths)
            {
                try
                {
                    var full = Path.Combine(p.Trim('\"'), "python.exe");
                    if (File.Exists(full))
                    {
                        _cachedPythonPath = full;
                        return full;
                    }
                }
                catch { }
            }

            // 2. Check standard Windows LocalAppData Python paths
            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var progDir = Path.Combine(localApp, "Programs", "Python");
            if (Directory.Exists(progDir))
            {
                foreach (var sub in Directory.GetDirectories(progDir))
                {
                    var exe = Path.Combine(sub, "python.exe");
                    if (File.Exists(exe))
                    {
                        _cachedPythonPath = exe;
                        return exe;
                    }
                }
            }

            // 3. Fallback check for Program Files
            var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var pfPy = Path.Combine(progFiles, "Python");
            if (Directory.Exists(pfPy))
            {
                foreach (var sub in Directory.GetDirectories(pfPy))
                {
                    var exe = Path.Combine(sub, "python.exe");
                    if (File.Exists(exe))
                    {
                        _cachedPythonPath = exe;
                        return exe;
                    }
                }
            }

            return null;
        }

        public async Task<AssessmentRunReport> ExecuteAssessmentAsync(
            AssessmentQuestion question,
            string userCode,
            CancellationToken cancellationToken = default,
            int timeoutMs = 3000)
        {
            var pythonExe = FindPythonExecutable();
            if (string.IsNullOrEmpty(pythonExe))
            {
                return new AssessmentRunReport
                {
                    Success = false,
                    RawError = "Python executable was not found on your system PATH or local application directories. Please install Python 3.8+ to execute tests in the Assessment Lab."
                };
            }

            // Create temporary runner script
            var tempDir = Path.Combine(Path.GetTempPath(), "MomentumAssessmentLab", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var scriptPath = Path.Combine(tempDir, "runner.py");

            try
            {
                var scriptContent = BuildRunnerScript(question, userCode);
                await File.WriteAllTextAsync(scriptPath, scriptContent, Encoding.UTF8, cancellationToken);

                var psi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"\"{scriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = tempDir
                };

                // Clear sensitive environment variables to isolate process from user keys
                psi.EnvironmentVariables.Remove("APPDATA");
                psi.EnvironmentVariables.Remove("LOCALAPPDATA");
                psi.EnvironmentVariables.Remove("USERPROFILE");
                psi.EnvironmentVariables.Remove("MOMENTUM_DATA_DIR");

                var sw = Stopwatch.StartNew();
                using var process = new Process { StartInfo = psi };

                const int MaxOutputChars = 65536; // 64 KB per-channel buffer limit
                var stdoutBuilder = new StringBuilder();
                var stderrBuilder = new StringBuilder();
                bool stdoutCapped = false;
                bool stderrCapped = false;

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        if (stdoutBuilder.Length < MaxOutputChars)
                            stdoutBuilder.AppendLine(e.Data);
                        else if (!stdoutCapped)
                        {
                            stdoutCapped = true;
                            stdoutBuilder.AppendLine("[OUTPUT TRUNCATED: Exceeded 64KB limit]");
                        }
                    }
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        if (stderrBuilder.Length < MaxOutputChars)
                            stderrBuilder.AppendLine(e.Data);
                        else if (!stderrCapped)
                        {
                            stderrCapped = true;
                            stderrBuilder.AppendLine("[STDERR TRUNCATED: Exceeded 64KB limit]");
                        }
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var waitTask = process.WaitForExitAsync(cts.Token);
                var timeoutTask = Task.Delay(timeoutMs, cts.Token);

                var completed = await Task.WhenAny(waitTask, timeoutTask);
                sw.Stop();

                if (cancellationToken.IsCancellationRequested)
                {
                    try { process.Kill(entireProcessTree: true); } catch { }
                    return new AssessmentRunReport
                    {
                        Success = false,
                        RawError = "Execution was cancelled by the user."
                    };
                }

                if (completed == timeoutTask)
                {
                    try { process.Kill(entireProcessTree: true); } catch { }
                    return new AssessmentRunReport
                    {
                        Success = false,
                        TimedOut = true,
                        TotalExecutionMs = sw.ElapsedMilliseconds,
                        RawError = $"Execution exceeded maximum time limit of {timeoutMs}ms. Possible infinite loop or unbounded recursion."
                    };
                }

                cts.Cancel(); // Cancel timeout delay
                await waitTask;

                var rawStdout = stdoutBuilder.ToString().Trim();
                var rawStderr = stderrBuilder.ToString().Trim();

                return ParseRunnerOutput(question, rawStdout, rawStderr, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                return new AssessmentRunReport
                {
                    Success = false,
                    RawError = "Execution was cancelled by the user."
                };
            }
            catch (Exception ex)
            {
                return new AssessmentRunReport
                {
                    Success = false,
                    RawError = $"Internal harness execution exception: {ex.Message}"
                };
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, recursive: true);
                }
                catch { }
            }
        }

        private static string BuildRunnerScript(AssessmentQuestion question, string userCode)
        {
            var sb = new StringBuilder();
            sb.AppendLine("import sys");
            sb.AppendLine("import json");
            sb.AppendLine("import time");
            sb.AppendLine("import traceback");
            sb.AppendLine("import copy");
            sb.AppendLine("import io");
            sb.AppendLine();
            sb.AppendLine("real_stdout = sys.stdout");
            sb.AppendLine();
            sb.AppendLine("def emit(obj):");
            sb.AppendLine("    real_stdout.write('__MOMENTUM_OUTPUT_START__\\n' + json.dumps(obj) + '\\n__MOMENTUM_OUTPUT_END__\\n')");
            sb.AppendLine("    real_stdout.flush()");
            sb.AppendLine();
            sb.AppendLine("# Pre-compilation syntax validation");
            sb.AppendLine("user_code = \"\"\"" + EscapePythonMultiline(userCode) + "\"\"\"");
            sb.AppendLine();
            sb.AppendLine("try:");
            sb.AppendLine("    compile(user_code, '<user_code>', 'exec')");
            sb.AppendLine("except SyntaxError as se:");
            sb.AppendLine("    emit({");
            sb.AppendLine("        'syntax_error': True,");
            sb.AppendLine("        'line': se.lineno,");
            sb.AppendLine("        'offset': se.offset,");
            sb.AppendLine("        'msg': se.msg,");
            sb.AppendLine("        'text': se.text");
            sb.AppendLine("    })");
            sb.AppendLine("    sys.exit(0)");
            sb.AppendLine();
            sb.AppendLine("# Execute user code in isolated module dictionary with captured stdout");
            sb.AppendLine("user_scope = {}");
            sb.AppendLine("init_out = io.StringIO()");
            sb.AppendLine("sys.stdout = init_out");
            sb.AppendLine("try:");
            sb.AppendLine("    exec(user_code, user_scope)");
            sb.AppendLine("except BaseException as e:");
            sb.AppendLine("    sys.stdout = real_stdout");
            sb.AppendLine("    emit({");
            sb.AppendLine("        'runtime_init_error': True,");
            sb.AppendLine("        'msg': str(e),");
            sb.AppendLine("        'traceback': traceback.format_exc()");
            sb.AppendLine("    })");
            sb.AppendLine("    sys.exit(0)");
            sb.AppendLine("finally:");
            sb.AppendLine("    sys.stdout = real_stdout");
            sb.AppendLine();
            sb.AppendLine($"fn_name = '{question.FunctionName}'");
            sb.AppendLine("if fn_name not in user_scope or not callable(user_scope[fn_name]):");
            sb.AppendLine("    emit({");
            sb.AppendLine("        'missing_function': True,");
            sb.AppendLine("        'msg': f\"Function '{fn_name}' is not defined or is not callable.\"");
            sb.AppendLine("    })");
            sb.AppendLine("    sys.exit(0)");
            sb.AppendLine();
            sb.AppendLine("fn = user_scope[fn_name]");
            sb.AppendLine();
            sb.AppendLine("# Test cases execution");
            sb.AppendLine("test_cases = [");
            for (int i = 0; i < question.TestCases.Count; i++)
            {
                var tc = question.TestCases[i];
                var rawInput = tc.InputJson.Trim();
                var rawExp = tc.ExpectedOutputJson.Trim();
                sb.AppendLine($"    {{'index': {tc.Index}, 'input_json': '{EscapeStringForPythonLiteral(rawInput)}', 'expected_json': '{EscapeStringForPythonLiteral(rawExp)}', 'is_hidden': {(tc.IsHidden ? "True" : "False")}, 'edge_tag': '{EscapeStringForPythonLiteral(tc.EdgeCaseTag ?? "")}'}},");
            }
            sb.AppendLine("]");
            sb.AppendLine();
            sb.AppendLine(@"results = []
def normalize(val):
    if isinstance(val, (int, float, str, bool)) or val is None:
        return val
    if isinstance(val, (list, tuple)):
        return [normalize(x) for x in val]
    if isinstance(val, set):
        return sorted([normalize(x) for x in val])
    if isinstance(val, dict):
        return {str(k): normalize(v) for k, v in sorted(val.items())}
    return str(val)

def canonical_compare(actual, expected):
    if type(actual) != type(expected):
        # Allow tuple and list comparison
        if isinstance(actual, (list, tuple)) and isinstance(expected, (list, tuple)):
            if len(actual) != len(expected):
                return False
            return all(canonical_compare(a, b) for a, b in zip(actual, expected))
        return False
    if isinstance(actual, float) and isinstance(expected, float):
        return abs(actual - expected) < 1e-6
    if isinstance(actual, (list, tuple)):
        if len(actual) != len(expected):
            return False
        return all(canonical_compare(a, b) for a, b in zip(actual, expected))
    if isinstance(actual, dict):
        if set(actual.keys()) != set(expected.keys()):
            return False
        return all(canonical_compare(actual[k], expected[k]) for k in actual)
    return actual == expected

for tc in test_cases:
    idx = tc['index']
    raw_in = tc['input_json']
    raw_exp = tc['expected_json']
    args = json.loads(raw_in)
    expected = json.loads(raw_exp)
    
    # Take deep copies to detect in-place mutation side-effects
    args_copy = copy.deepcopy(args)
    
    t_start = time.perf_counter()
    mutated = False
    passed = False
    actual_val = None
    err_msg = None
    
    test_out = io.StringIO()
    sys.stdout = test_out
    try:
        if isinstance(args, list):
            actual_val = fn(*args)
        else:
            actual_val = fn(args)
            
        t_elapsed = (time.perf_counter() - t_start) * 1000.0
        
        # Check if caller arguments were modified
        if args != args_copy:
            mutated = True
            
        passed = canonical_compare(actual_val, expected)
        
        results.append({
            'index': idx,
            'passed': passed,
            'is_hidden': tc['is_hidden'],
            'edge_tag': tc['edge_tag'],
            'input_display': raw_in,
            'expected_display': json.dumps(expected),
            'actual_display': json.dumps(actual_val, default=str),
            'mutated': mutated,
            'stdout': test_out.getvalue()[:4096],
            'execution_ms': round(t_elapsed, 2)
        })
    except BaseException as ex:
        t_elapsed = (time.perf_counter() - t_start) * 1000.0
        tb = traceback.format_exc()
        results.append({
            'index': idx,
            'passed': False,
            'is_hidden': tc['is_hidden'],
            'edge_tag': tc['edge_tag'],
            'input_display': raw_in,
            'expected_display': json.dumps(expected),
            'actual_display': None,
            'error': f'{type(ex).__name__}: {str(ex)}',
            'traceback': tb,
            'mutated': False,
            'stdout': test_out.getvalue()[:4096],
            'execution_ms': round(t_elapsed, 2)
        })
    finally:
        sys.stdout = real_stdout

emit({'success': True, 'results': results})
");
            return sb.ToString();
        }

        private static string EscapePythonMultiline(string code)
        {
            return code.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "");
        }

        private static string EscapeStringForPythonLiteral(string text)
        {
            return text.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "").Replace("\n", "\\n");
        }

        private static AssessmentRunReport ParseRunnerOutput(
            AssessmentQuestion question,
            string stdout,
            string stderr,
            long elapsedMs)
        {
            var report = new AssessmentRunReport
            {
                TotalExecutionMs = elapsedMs
            };

            if (string.IsNullOrWhiteSpace(stdout))
            {
                report.Success = false;
                report.RawError = !string.IsNullOrWhiteSpace(stderr) ? stderr : "Process exited without output.";
                return report;
            }

            string jsonText = stdout;
            const string markerStart = "__MOMENTUM_OUTPUT_START__";
            const string markerEnd = "__MOMENTUM_OUTPUT_END__";
            int sIdx = stdout.IndexOf(markerStart, StringComparison.Ordinal);
            int eIdx = stdout.IndexOf(markerEnd, StringComparison.Ordinal);
            if (sIdx >= 0 && eIdx > sIdx)
            {
                jsonText = stdout.Substring(sIdx + markerStart.Length, eIdx - (sIdx + markerStart.Length)).Trim();
            }
            else
            {
                // Fallback: look for the last line starting with { and ending with }
                var candidate = stdout.Split('\n')
                    .Select(l => l.Trim())
                    .LastOrDefault(l => l.StartsWith("{") && l.EndsWith("}"));
                if (!string.IsNullOrEmpty(candidate)) jsonText = candidate;
            }

            try
            {
                using var doc = JsonDocument.Parse(jsonText);
                var root = doc.RootElement;

                if (root.TryGetProperty("syntax_error", out var isSyntax) && isSyntax.GetBoolean())
                {
                    report.HasSyntaxError = true;
                    report.Success = false;
                    var line = root.GetProperty("line").GetInt32();
                    var msg = root.GetProperty("msg").GetString();
                    var text = root.TryGetProperty("text", out var tProp) ? tProp.GetString() : "";
                    report.RawError = $"SyntaxError on Line {line}: {msg}\n{text}";

                    report.Diagnostic = new FailureDiagnostic
                    {
                        Category = FailureTaxonomy.SyntaxError,
                        CategoryDisplayName = "Syntax / Parse Error",
                        ObservedFact = $"Python parser encountered a SyntaxError on Line {line}: {msg}",
                        ConceptInsight = "Python source code must adhere to strict indentation and grammatical syntax rules before byte compilation can begin.",
                        ActionableRecommendation = $"Inspect line {line} for missing colons (:), unclosed parentheses, quotes, or mismatched indentation."
                    };
                    return report;
                }

                if (root.TryGetProperty("runtime_init_error", out var isInitErr) && isInitErr.GetBoolean())
                {
                    report.Success = false;
                    var msg = root.GetProperty("msg").GetString();
                    var tb = root.GetProperty("traceback").GetString();
                    report.RawError = $"Initialization Error: {msg}\n{tb}";
                    report.Diagnostic = new FailureDiagnostic
                    {
                        Category = FailureTaxonomy.UnhandledException,
                        CategoryDisplayName = "Global Initialization Error",
                        ObservedFact = $"Code crashed during module import/evaluation: {msg}",
                        ConceptInsight = "Top-level code outside of function definitions executes immediately upon module loading.",
                        ActionableRecommendation = "Avoid executing statements with side-effects or unresolved global variables outside your target function."
                    };
                    return report;
                }

                if (root.TryGetProperty("missing_function", out var isMissing) && isMissing.GetBoolean())
                {
                    report.Success = false;
                    var msg = root.GetProperty("msg").GetString() ?? "Function is missing or not callable.";
                    report.RawError = msg;
                    report.Diagnostic = new FailureDiagnostic
                    {
                        Category = FailureTaxonomy.OutputFormatting,
                        CategoryDisplayName = "Missing Entry Function",
                        ObservedFact = msg,
                        ConceptInsight = $"The assessment test runner expects a callable function named '{question.FunctionName}'.",
                        ActionableRecommendation = $"Ensure your code defines `def {question.FunctionName}(...)` with the requested name and parameters."
                    };
                    return report;
                }

                if (root.TryGetProperty("results", out var resultsElem) && resultsElem.ValueKind == JsonValueKind.Array)
                {
                    bool allPassed = true;
                    foreach (var item in resultsElem.EnumerateArray())
                    {
                        bool isHidden = item.GetProperty("is_hidden").GetBoolean();
                        bool passed = item.GetProperty("passed").GetBoolean();
                        string rawIn = item.GetProperty("input_display").GetString() ?? "";
                        string rawExp = item.GetProperty("expected_display").GetString() ?? "";
                        string rawAct = item.TryGetProperty("actual_display", out var ad) && ad.ValueKind != JsonValueKind.Null ? ad.GetString() ?? "" : "";

                        var res = new AssessmentTestResult
                        {
                            Index = item.GetProperty("index").GetInt32(),
                            Passed = passed,
                            IsEvaluated = true,
                            IsHidden = isHidden,
                            EdgeCaseTag = item.TryGetProperty("edge_tag", out var et) && !string.IsNullOrWhiteSpace(et.GetString()) ? et.GetString() : null,
                            RawInputDisplay = rawIn,
                            InputDisplay = isHidden ? "[Hidden test case parameters]" : rawIn,
                            ExpectedDisplay = isHidden ? "[Hidden canonical output]" : rawExp,
                            ActualDisplay = isHidden ? (passed ? "[Hidden match]" : "[Hidden output diverged]") : rawAct,
                            ErrorMessage = item.TryGetProperty("error", out var err) ? err.GetString() : null,
                            Stdout = item.TryGetProperty("stdout", out var st) && !string.IsNullOrWhiteSpace(st.GetString()) ? st.GetString() : null,
                            ExecutionTimeMs = item.TryGetProperty("execution_ms", out var em) ? (long)em.GetDouble() : 0,
                            Mutated = item.TryGetProperty("mutated", out var mut) && mut.GetBoolean()
                        };

                        if (!res.Passed) allPassed = false;
                        report.TestResults.Add(res);
                    }

                    report.Success = allPassed;
                    return report;
                }
            }
            catch (Exception ex)
            {
                report.Success = false;
                report.RawError = $"Failed to parse test execution output: {ex.Message}\nRaw stdout: {stdout}\nRaw stderr: {stderr}";
            }

            return report;
        }
    }
}
