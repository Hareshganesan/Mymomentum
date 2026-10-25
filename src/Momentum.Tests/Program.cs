using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Momentum.Content;
using Momentum.Models;
using Momentum.Services;

namespace Momentum.Tests
{
    class Program
    {
        private static int _passedCount = 0;
        private static int _failedCount = 0;

        static async Task<int> Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("===============================================================");
            Console.WriteLine("   MOMENTUM V6: ASSESSMENT LAB & FAILURE FORENSICS TEST SUITE  ");
            Console.WriteLine("===============================================================");
            Console.ResetColor();

            var harness = new PythonExecutionHarness();

            // Test 1: Python Discovery
            RunTest("Python 3.x Environment Discovery", () =>
            {
                var py = PythonExecutionHarness.FindPythonExecutable();
                AssertTrue(!string.IsNullOrEmpty(py), $"Python executable detected at: {py}");
            });

            // Test 2: AppState V6 Deserialization & Backward Compatibility
            RunTest("AppState V6 Backward Compatibility Normalization", () =>
            {
                var legacyV5Json = @"{
                    ""version"": 5,
                    ""profile"": { ""name"": ""Candidate"" },
                    ""problems"": { ""two-sum"": { ""status"": ""Solved"" } }
                }";

                var state = JsonSerializer.Deserialize<AppState>(legacyV5Json, StorageService.JsonOptions);
                AssertTrue(state != null, "State deserialized successfully");
                AssertTrue(state!.Lab != null, "Lab state initialized");
                AssertTrue(state.Lab?.Attempts != null, "Attempts list initialized");
                AssertTrue(state.Lab?.SolvedQuestionIds != null, "SolvedQuestionIds set initialized");
                AssertTrue(state.Lab?.SavedCode != null, "SavedCode dictionary initialized");
            });

            // Test 3: Catalog Integrity
            RunTest("Assessment Catalog Integrity (8-10 Questions, Test Cases, Reference Solutions)", () =>
            {
                var questions = AssessmentCatalog.Questions;
                AssertTrue(questions.Count >= 8, $"Catalog contains {questions.Count} questions (expected >= 8)");
                foreach (var q in questions)
                {
                    AssertTrue(!string.IsNullOrWhiteSpace(q.Id), $"Question {q.Id} has non-empty ID");
                    AssertTrue(!string.IsNullOrWhiteSpace(q.FunctionName), $"Question {q.Id} has FunctionName");
                    AssertTrue(!string.IsNullOrWhiteSpace(q.StarterCode), $"Question {q.Id} has StarterCode");
                    AssertTrue(!string.IsNullOrWhiteSpace(q.ReferenceSolution), $"Question {q.Id} has ReferenceSolution");
                    AssertTrue(q.TestCases.Count >= 3, $"Question {q.Id} has >= 3 test cases");
                    AssertTrue(q.TestCases.Any(t => t.IsHidden), $"Question {q.Id} contains hidden test cases");
                }
            });

            // Test 4: Reference Solution Verification (All catalog questions pass 100%)
            await RunTestAsync("Reference Solutions Pass 100% of Test Cases", async () =>
            {
                foreach (var q in AssessmentCatalog.Questions)
                {
                    var report = await harness.ExecuteAssessmentAsync(q, q.ReferenceSolution);
                    AssertTrue(report.Success, $"Question '{q.Title}' reference solution must pass all tests. Raw error: {report.RawError}");
                    AssertTrue(report.TestResults.All(r => r.Passed), $"All test cases for '{q.Title}' passed");
                    var diag = FailureForensicsEngine.Diagnose(q, q.ReferenceSolution, report);
                    AssertTrue(diag.Category == FailureTaxonomy.None, $"Diagnostic for '{q.Title}' is None");
                }
            });

            // Test 5: Forensics - Syntax Error Classification
            await RunTestAsync("Forensics: Syntax Error Detection & Classification", async () =>
            {
                var q = AssessmentCatalog.Questions[0]; // Two sum
                var brokenSyntaxCode = @"def two_sum(nums, target):
    if len(nums) > 0
        return []
";
                var report = await harness.ExecuteAssessmentAsync(q, brokenSyntaxCode);
                AssertTrue(!report.Success, "Broken code must fail execution");
                AssertTrue(report.HasSyntaxError, "Must flag HasSyntaxError");
                var diag = FailureForensicsEngine.Diagnose(q, brokenSyntaxCode, report);
                AssertTrue(diag.Category == FailureTaxonomy.SyntaxError, $"Category must be SyntaxError, got {diag.Category}");
                AssertTrue(!string.IsNullOrWhiteSpace(diag.ActionableRecommendation), "Must provide actionable recommendation");
            });

            // Test 6: Forensics - Infinite Loop Timeout Classification
            await RunTestAsync("Forensics: Infinite Loop Timeout Handling", async () =>
            {
                var q = AssessmentCatalog.Questions[0];
                var infiniteLoopCode = @"def two_sum(nums, target):
    x = 0
    while True:
        x += 1
    return []
";
                var report = await harness.ExecuteAssessmentAsync(q, infiniteLoopCode, timeoutMs: 1500);
                AssertTrue(!report.Success, "Infinite loop must fail");
                AssertTrue(report.TimedOut, "Must report TimedOut = true");
                var diag = FailureForensicsEngine.Diagnose(q, infiniteLoopCode, report);
                AssertTrue(diag.Category == FailureTaxonomy.TimeLimitExceeded, $"Category must be TimeLimitExceeded, got {diag.Category}");
            });

            // Test 7: Forensics - Boundary / Empty Collection Crash Classification
            await RunTestAsync("Forensics: Empty Collection Boundary Crash Classification", async () =>
            {
                var q = AssessmentCatalog.Questions[0];
                var boundaryCrashCode = @"def two_sum(nums, target):
    if target == 9:
        return [0, 1]
    if target == 6 and nums == [3, 2, 4]:
        return [1, 2]
    if target == 6 and nums == [3, 3]:
        return [0, 1]
    # Unsafe access on empty or boundary input without check
    val = nums[0]
    return []
";
                var report = await harness.ExecuteAssessmentAsync(q, boundaryCrashCode);
                AssertTrue(!report.Success, "Unsafe index access must fail on empty list");
                var diag = FailureForensicsEngine.Diagnose(q, boundaryCrashCode, report);
                AssertTrue(diag.Category == FailureTaxonomy.EmptyInputOrBoundary, $"Category must be EmptyInputOrBoundary, got {diag.Category}");
            });

            // Test 8: Forensics - Missing Return Value (NoneType)
            await RunTestAsync("Forensics: Missing Return Value Classification", async () =>
            {
                var q = AssessmentCatalog.Questions[0];
                var missingReturnCode = @"def two_sum(nums, target):
    # Does some computation but forgets return
    ans = [0, 1]
";
                var report = await harness.ExecuteAssessmentAsync(q, missingReturnCode);
                AssertTrue(!report.Success, "Missing return must fail");
                var diag = FailureForensicsEngine.Diagnose(q, missingReturnCode, report);
                AssertTrue(diag.Category == FailureTaxonomy.OutputFormatting, $"Category must be OutputFormatting, got {diag.Category}");
            });

            // Test 9: Forensics - Value Mismatch & Hidden Test Obfuscation
            await RunTestAsync("Forensics: Value Mismatch & Hidden Test Protection", async () =>
            {
                var q = AssessmentCatalog.Questions[0];
                // Fails on negative numbers or no solution
                var partialCode = @"def two_sum(nums, target):
    if nums == [2, 7, 11, 15]:
        return [0, 1]
    if nums == [3, 2, 4]:
        return [1, 2]
    if nums == [3, 3]:
        return [0, 1]
    return [99, 99]
";
                var report = await harness.ExecuteAssessmentAsync(q, partialCode);
                AssertTrue(!report.Success, "Hardcoded code must fail hidden tests");
                var diag = FailureForensicsEngine.Diagnose(q, partialCode, report);
                AssertTrue(diag.Category == FailureTaxonomy.ValueMismatch, $"Category must be ValueMismatch, got {diag.Category}");
                // Ensure hidden test input is not exposed in diagnostic or UI display
                if (diag.IsHiddenTest)
                {
                    AssertTrue(!diag.ObservedFact.Contains("[-1, -2, -3, -4, -5]"), "Hidden test raw input must NOT be exposed in diagnostic message");
                }
                var hiddenRes = report.TestResults.FirstOrDefault(r => r.IsHidden);
                if (hiddenRes != null)
                {
                    AssertTrue(!hiddenRes.InputDisplay.Contains("[-1, -2, -3, -4, -5]"), "Hidden test raw input must NOT be exposed in InputDisplay");
                    AssertTrue(!hiddenRes.ExpectedDisplay.Contains("[2, 4]"), "Hidden test expected output must NOT be exposed in ExpectedDisplay");
                }
            });

            // Test 10: Forensics - Off-By-One Numerical Discrepancy
            await RunTestAsync("Forensics: Off-By-One Numerical Discrepancy Classification", async () =>
            {
                var q = AssessmentCatalog.Questions.First(x => x.Id == "find-in-rotated-array");
                // Returns 1 higher than true index
                var offByOneCode = @"def search_rotated(nums, target):
    # Off by one logic
    if target == 0:
        return 5 # Correct is 4
    return -1
";
                var report = await harness.ExecuteAssessmentAsync(q, offByOneCode);
                AssertTrue(!report.Success, "Off-by-one code must fail");
                var diag = FailureForensicsEngine.Diagnose(q, offByOneCode, report);
                AssertTrue(diag.Category == FailureTaxonomy.OffByOneOrSlice, $"Category must be OffByOneOrSlice, got {diag.Category}");
            });

            // Test 11: Forensics - Zero Division Error Classification
            await RunTestAsync("Forensics: Zero Division Error Classification", async () =>
            {
                var q = AssessmentCatalog.Questions[0];
                var divZeroCode = @"def two_sum(nums, target):
    zero = len(nums) - len(nums)
    return [1 // zero, 0]
";
                var report = await harness.ExecuteAssessmentAsync(q, divZeroCode);
                AssertTrue(!report.Success, "Division by zero must fail");
                var diag = FailureForensicsEngine.Diagnose(q, divZeroCode, report);
                AssertTrue(diag.Category == FailureTaxonomy.EmptyInputOrBoundary, $"Category must be EmptyInputOrBoundary, got {diag.Category}");
                AssertTrue(diag.CategoryDisplayName.Contains("Zero Division"), $"DisplayName must mention Zero Division, got {diag.CategoryDisplayName}");
            });

            // Test 12: Forensics - Input Mutation Side-Effect Detection
            await RunTestAsync("Forensics: Input Mutation Side-Effect Detection", async () =>
            {
                var q = AssessmentCatalog.Questions.First(x => x.Id == "merge-intervals");
                // In-place mutating intervals without returning correct merged structure
                var mutatingCode = @"def merge_intervals(intervals):
    intervals.sort(key=lambda x: x[0])
    intervals.pop() # In-place mutation of caller argument
    return [[1, 6], [8, 10], [15, 18]]
";
                var report = await harness.ExecuteAssessmentAsync(q, mutatingCode);
                var diag = FailureForensicsEngine.Diagnose(q, mutatingCode, report);
                AssertTrue(diag.Category == FailureTaxonomy.InputMutationSideEffect, $"Category must be InputMutationSideEffect, got {diag.Category}");
                AssertTrue(diag.CategoryDisplayName.Contains("Mutation"), $"DisplayName must mention Mutation, got {diag.CategoryDisplayName}");
            });

            // Test 13: Harness - Output Buffer Bounding under Print Flood
            await RunTestAsync("Harness: Output Buffer Bounding under Heavy Print Flood", async () =>
            {
                var q = AssessmentCatalog.Questions[0];
                // Floods stdout with thousands of lines
                var floodCode = @"def two_sum(nums, target):
    for i in range(5000):
        print(f'spamming output line {i} abcdefghijklmnopqrstuvwxyz')
    # Correct solution follows
    seen = {}
    for i, num in enumerate(nums):
        comp = target - num
        if comp in seen:
            return [seen[comp], i]
        seen[num] = i
    return []
";
                var report = await harness.ExecuteAssessmentAsync(q, floodCode);
                AssertTrue(report.Success, "Valid solution with heavy output flood should execute within bounded buffer");
            });

            // Test 14: Harness - Process Tree Cancellation Termination
            await RunTestAsync("Harness: Process Tree Cancellation Termination", async () =>
            {
                var q = AssessmentCatalog.Questions[0];
                var loopCode = @"def two_sum(nums, target):
    import time
    time.sleep(10)
    return [0, 1]
";
                using var cts = new System.Threading.CancellationTokenSource();
                cts.CancelAfter(400); // cancel after 400ms
                var report = await harness.ExecuteAssessmentAsync(q, loopCode, cancellationToken: cts.Token, timeoutMs: 10000);
                AssertTrue(!report.Success, "Cancelled execution must fail");
                AssertTrue(report.RawError != null && report.RawError.Contains("cancelled"), $"Expected cancellation message, got: {report.RawError}");
            });

            // Test 15: Persistence - AppState Round-Trip with Lab State
            RunTest("Persistence: AppState Round-Trip with Lab State", () =>
            {
                var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MomentumTestState_" + Guid.NewGuid().ToString("N"));
                System.IO.Directory.CreateDirectory(tempDir);
                var testFile = System.IO.Path.Combine(tempDir, "state.json");
                try
                {
                    var state = new AppState();
                    state.Lab.SolvedQuestionIds.Add("two-sum");
                    state.Lab.SavedCode["two-sum"] = "def two_sum(nums, target): return [0, 1]";
                    state.Lab.Attempts.Add(new AssessmentAttempt
                    {
                        QuestionId = "two-sum",
                        AllPassed = true,
                        TestsPassed = 5,
                        TotalTests = 5
                    });

                    var json = JsonSerializer.Serialize(state, StorageService.JsonOptions);
                    System.IO.File.WriteAllText(testFile, json);

                    var loaded = JsonSerializer.Deserialize<AppState>(System.IO.File.ReadAllText(testFile), StorageService.JsonOptions);
                    AssertTrue(loaded != null, "Loaded state must not be null");
                    AssertTrue(loaded!.Lab.SolvedQuestionIds.Contains("two-sum"), "Solved question ID persisted");
                    AssertTrue(loaded.Lab.SavedCode["two-sum"].Contains("def two_sum"), "Saved code persisted");
                    AssertTrue(loaded.Lab.Attempts.Count == 1, "Attempt record persisted");
                    AssertTrue(loaded.Lab.Attempts[0].AllPassed, "Attempt record data preserved");
                }
                finally
                {
                    if (System.IO.Directory.Exists(tempDir))
                        System.IO.Directory.Delete(tempDir, true);
                }
            });

            // Test 16: AI Fluency - Draft Catalog & Traps Integrity
            RunTest("AI Fluency: Draft Integrity & Prompt Simulation Catalog", () =>
            {
                var questions = AssessmentCatalog.Questions;
                foreach (var q in questions)
                {
                    AssertTrue(q.HasAiDraft, $"Question '{q.Title}' has an AI draft");
                    AssertTrue(!string.IsNullOrWhiteSpace(q.AiDraftCode), $"Question '{q.Title}' has non-empty AiDraftCode");
                    AssertTrue(!string.IsNullOrWhiteSpace(q.AiStatedAssumption), $"Question '{q.Title}' has stated assumption");
                    AssertTrue(!string.IsNullOrWhiteSpace(q.AiFlawExplanation), $"Question '{q.Title}' explains latent flaw");
                    AssertTrue(!string.IsNullOrWhiteSpace(q.AiPromptSimulation), $"Question '{q.Title}' includes prompt simulation");
                }
            });

            // Test 17: AI Fluency - Raw AI Drafts Contain Real Latent Edge Flaws
            await RunTestAsync("AI Fluency: Raw AI Drafts Fail Latent Invariant Checks", async () =>
            {
                int flawCount = 0;
                foreach (var q in AssessmentCatalog.Questions)
                {
                    var report = await harness.ExecuteAssessmentAsync(q, q.AiDraftCode, timeoutMs: 2500);
                    // Raw AI draft should fail at least one test case due to subtle hallucination/trap
                    if (!report.Success)
                    {
                        flawCount++;
                        var diag = FailureForensicsEngine.Diagnose(q, q.AiDraftCode, report);
                        AssertTrue(diag.Category != FailureTaxonomy.None, $"Question '{q.Title}' latent flaw diagnosed as {diag.CategoryDisplayName}");
                    }
                }
                AssertTrue(flawCount == AssessmentCatalog.Questions.Count, 
                    $"All {AssessmentCatalog.Questions.Count} AI drafts must fail at least one edge case initially. Caught: {flawCount}/{AssessmentCatalog.Questions.Count}");
            });

            // Test 18: Persistence - AI Fluency Lab State Round-Trip
            RunTest("Persistence: AI Fluency Lab State Serialization Round-Trip", () =>
            {
                var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MomentumAiState_" + Guid.NewGuid().ToString("N"));
                System.IO.Directory.CreateDirectory(tempDir);
                var testFile = System.IO.Path.Combine(tempDir, "state.json");
                try
                {
                    var state = new AppState();
                    state.Lab.AuditedQuestionIds.Add("two-sum");
                    state.Lab.SavedAuditCode["two-sum"] = "# Audited fix\ndef two_sum(nums, target): return []";
                    state.Lab.Attempts.Add(new AssessmentAttempt
                    {
                        QuestionId = "two-sum",
                        IsAiAuditAttempt = true,
                        AllPassed = true,
                        TestsPassed = 5,
                        TotalTests = 5
                    });

                    var json = JsonSerializer.Serialize(state, StorageService.JsonOptions);
                    System.IO.File.WriteAllText(testFile, json);

                    var loaded = JsonSerializer.Deserialize<AppState>(System.IO.File.ReadAllText(testFile), StorageService.JsonOptions);
                    AssertTrue(loaded != null, "Loaded state must not be null");
                    AssertTrue(loaded!.Lab.AuditedQuestionIds.Contains("two-sum"), "AuditedQuestionIds persisted");
                    AssertTrue(loaded.Lab.SavedAuditCode["two-sum"].Contains("Audited fix"), "SavedAuditCode persisted");
                    AssertTrue(loaded.Lab.Attempts.Count == 1 && loaded.Lab.Attempts[0].IsAiAuditAttempt, "IsAiAuditAttempt preserved");
                }
                finally
                {
                    if (System.IO.Directory.Exists(tempDir))
                        System.IO.Directory.Delete(tempDir, true);
                }
            });

            // Summary
            Console.WriteLine();
            Console.WriteLine("===============================================================");
            if (_failedCount == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"   ALL {_passedCount} TESTS PASSED SUCCESSFULLY! (0 Failures)");
                Console.ResetColor();
                return 0;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"   TEST SUITE FAILED: {_failedCount} Failed, {_passedCount} Passed");
                Console.ResetColor();
                return 1;
            }
        }

        private static void RunTest(string name, Action test)
        {
            try
            {
                test();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("  [PASS] ");
                Console.ResetColor();
                Console.WriteLine(name);
                _passedCount++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  [FAIL] ");
                Console.ResetColor();
                Console.WriteLine($"{name}: {ex.Message}");
                _failedCount++;
            }
        }

        private static async Task RunTestAsync(string name, Func<Task> test)
        {
            try
            {
                await test();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("  [PASS] ");
                Console.ResetColor();
                Console.WriteLine(name);
                _passedCount++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  [FAIL] ");
                Console.ResetColor();
                Console.WriteLine($"{name}: {ex.Message}");
                _failedCount++;
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }
    }
}
