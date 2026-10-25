using System;
using System.Collections.Generic;

namespace Momentum.Models
{
    public enum FailureTaxonomy
    {
        None,
        SyntaxError,
        TimeLimitExceeded,
        EmptyInputOrBoundary,
        InputMutationSideEffect,
        OffByOneOrSlice,
        TypeOrHashabilityError,
        OutputFormatting,
        ValueMismatch,
        UnhandledException
    }

    public class AssessmentLabState
    {
        public List<AssessmentAttempt> Attempts { get; set; } = new();
        public HashSet<string> SolvedQuestionIds { get; set; } = new();
        public Dictionary<string, string> SavedCode { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class AssessmentAttempt
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string QuestionId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Code { get; set; } = string.Empty;
        public bool AllPassed { get; set; }
        public int TestsPassed { get; set; }
        public int TotalTests { get; set; }
        public long ExecutionTimeMs { get; set; }
        public FailureTaxonomy FailureCategory { get; set; } = FailureTaxonomy.None;
        public string? DiagnosticSummary { get; set; }
    }

    public class AssessmentQuestion
    {
        private string _description = string.Empty;

        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public Difficulty Difficulty { get; set; } = Difficulty.Medium;
        public string Topic { get; set; } = string.Empty;
        public string EstimatedMinutes { get; set; } = "15-20 min";
        public string FunctionName { get; set; } = string.Empty;
        public List<string> ParameterNames { get; set; } = new();
        public string StarterCode { get; set; } = string.Empty;
        public string ReferenceSolution { get; set; } = string.Empty;
        public List<AssessmentTestCase> TestCases { get; set; } = new();
        public string Hint { get; set; } = string.Empty;
        public string ConceptExplanation { get; set; } = string.Empty;
        public bool IsSolved { get; set; }

        public string Description
        {
            get => _description;
            set
            {
                _description = value ?? string.Empty;
                ParseDescription(_description);
            }
        }

        public string Summary { get; set; } = string.Empty;
        public List<QuestionExample> Examples { get; set; } = new();
        public List<string> Constraints { get; set; } = new();

        private void ParseDescription(string desc)
        {
            if (string.IsNullOrWhiteSpace(desc))
            {
                Summary = string.Empty;
                Examples = new();
                Constraints = new();
                return;
            }

            var lines = desc.Replace("\r\n", "\n").Split('\n');
            string currentSection = "summary";
            var summaryLines = new List<string>();
            var constraintLines = new List<string>();
            var exampleLines = new List<string>();

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith("### Problem Statement", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "summary";
                    continue;
                }
                if (line.StartsWith("### Constraints", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "constraints";
                    continue;
                }
                if (line.StartsWith("### Examples", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "examples";
                    continue;
                }

                if (currentSection == "summary")
                {
                    summaryLines.Add(rawLine);
                }
                else if (currentSection == "constraints")
                {
                    if (line.StartsWith("-") || line.StartsWith("*"))
                    {
                        var clean = line.TrimStart('-', '*', ' ').Trim('`');
                        if (!string.IsNullOrWhiteSpace(clean))
                            constraintLines.Add(clean);
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        constraintLines.Add(line.Trim('`'));
                    }
                }
                else if (currentSection == "examples")
                {
                    exampleLines.Add(rawLine);
                }
            }

            // Clean markdown syntax from summary for clean display
            var cleanedSummary = string.Join("\n", summaryLines).Trim();
            // Remove markdown bold markers
            cleanedSummary = cleanedSummary.Replace("**", "");
            Summary = cleanedSummary;
            Constraints = constraintLines;

            // Parse examples
            var parsedExamples = new List<QuestionExample>();
            QuestionExample? curEx = null;
            int exNum = 1;

            foreach (var rawLine in exampleLines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.Contains("Example", StringComparison.OrdinalIgnoreCase) && (line.StartsWith("-") || line.StartsWith("*") || line.StartsWith("**")))
                {
                    if (curEx != null) parsedExamples.Add(curEx);
                    curEx = new QuestionExample { Number = exNum++ };

                    // Try single line format: - **Example 1:** `nums = [2, 7]` -> `[0, 1]` (explanation)
                    int arrowIdx = line.IndexOf("->", StringComparison.Ordinal);
                    if (arrowIdx > 0)
                    {
                        var left = line.Substring(0, arrowIdx);
                        var right = line.Substring(arrowIdx + 2);

                        // Extract input
                        int colonIdx = left.IndexOf(':');
                        if (colonIdx >= 0) left = left.Substring(colonIdx + 1);
                        curEx.Input = left.Trim('`', '*', ' ', '-');

                        // Extract output and explanation
                        int parenIdx = right.IndexOf('(');
                        if (parenIdx >= 0)
                        {
                            curEx.Output = right.Substring(0, parenIdx).Trim('`', '*', ' ');
                            curEx.Explanation = right.Substring(parenIdx).Trim('(', ')', ' ', '*');
                        }
                        else
                        {
                            curEx.Output = right.Trim('`', '*', ' ');
                        }
                    }
                    else
                    {
                        curEx.Input = line;
                    }
                }
                else if (curEx != null)
                {
                    if (line.StartsWith("Input:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("`Input:", StringComparison.OrdinalIgnoreCase))
                    {
                        curEx.Input = line.Replace("Input:", "").Trim('`', ' ');
                    }
                    else if (line.StartsWith("Output:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("`Output:", StringComparison.OrdinalIgnoreCase))
                    {
                        curEx.Output = line.Replace("Output:", "").Trim('`', ' ');
                    }
                    else if (line.StartsWith("Explanation:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("*Explanation:", StringComparison.OrdinalIgnoreCase))
                    {
                        curEx.Explanation = line.Replace("Explanation:", "").Trim('*', ' ');
                    }
                    else if (string.IsNullOrEmpty(curEx.Explanation))
                    {
                        curEx.Explanation = line.Trim('*', ' ');
                    }
                }
            }

            if (curEx != null) parsedExamples.Add(curEx);
            Examples = parsedExamples;
        }
    }

    public class QuestionExample
    {
        public int Number { get; set; }
        public string Input { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public string? Explanation { get; set; }
    }

    public class AssessmentTestCase
    {
        public int Index { get; set; }
        public string InputJson { get; set; } = "[]"; // Arguments list as JSON array
        public string ExpectedOutputJson { get; set; } = "null";
        public bool IsHidden { get; set; }
        public string? EdgeCaseTag { get; set; }
        public string? Description { get; set; }
    }

    public class AssessmentTestResult
    {
        public int Index { get; set; }
        public bool Passed { get; set; }
        public bool IsEvaluated { get; set; }
        public bool IsHidden { get; set; }
        public string? EdgeCaseTag { get; set; }
        public string InputDisplay { get; set; } = string.Empty;
        public string ExpectedDisplay { get; set; } = string.Empty;
        public string ActualDisplay { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public string? Stdout { get; set; }
        public long ExecutionTimeMs { get; set; }
        public bool Mutated { get; set; }
        public string RawInputDisplay { get; set; } = string.Empty;

        public bool IsSuccess => IsEvaluated && Passed;
        public bool IsFailure => IsEvaluated && !Passed;
        public bool IsPending => !IsEvaluated;
    }

    public class AssessmentRunReport
    {
        public bool Success { get; set; }
        public bool TimedOut { get; set; }
        public bool HasSyntaxError { get; set; }
        public string? RawError { get; set; }
        public List<AssessmentTestResult> TestResults { get; set; } = new();
        public FailureDiagnostic? Diagnostic { get; set; }
        public long TotalExecutionMs { get; set; }
    }

    public class FailureDiagnostic
    {
        public FailureTaxonomy Category { get; set; } = FailureTaxonomy.None;
        public string CategoryDisplayName { get; set; } = string.Empty;
        public string ObservedFact { get; set; } = string.Empty;
        public string ConceptInsight { get; set; } = string.Empty;
        public string ActionableRecommendation { get; set; } = string.Empty;
        public int? FailedTestIndex { get; set; }
        public bool IsHiddenTest { get; set; }
    }
}
