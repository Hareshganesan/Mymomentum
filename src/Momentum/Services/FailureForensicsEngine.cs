using System;
using System.Linq;
using System.Text.Json;
using Momentum.Models;

namespace Momentum.Services
{
    public static class FailureForensicsEngine
    {
        public static FailureDiagnostic Diagnose(AssessmentQuestion question, string userCode, AssessmentRunReport report)
        {
            if (report.Success)
            {
                return new FailureDiagnostic
                {
                    Category = FailureTaxonomy.None,
                    CategoryDisplayName = "All Tests Passed",
                    ObservedFact = $"All {report.TestResults.Count} test cases (including hidden tests) executed and matched canonical expectations.",
                    ConceptInsight = "Your implementation satisfies required problem constraints, boundary conditions, and return signatures.",
                    ActionableRecommendation = "Review reference solution for optimal time and space complexity comparison."
                };
            }

            // 1. Timeout / TLE
            if (report.TimedOut)
            {
                return new FailureDiagnostic
                {
                    Category = FailureTaxonomy.TimeLimitExceeded,
                    CategoryDisplayName = "Time Limit Exceeded",
                    ObservedFact = $"Execution exceeded maximum time budget ({report.TotalExecutionMs}ms).",
                    ConceptInsight = "Algorithms with infinite loops (`while` without advancement), unbounded recursion, or quadratic O(N^2) complexity on large inputs trigger timeouts.",
                    ActionableRecommendation = "Check your while-loop increment/decrement conditions and verify base cases in recursive branches."
                };
            }

            // 2. Syntax / Initialization error already captured
            if (report.Diagnostic != null && report.Diagnostic.Category != FailureTaxonomy.None)
            {
                return report.Diagnostic;
            }

            // 3. Find first failed test result
            var firstFail = report.TestResults.FirstOrDefault(t => !t.Passed);
            if (firstFail == null)
            {
                return new FailureDiagnostic
                {
                    Category = FailureTaxonomy.UnhandledException,
                    CategoryDisplayName = "Unknown Failure",
                    ObservedFact = report.RawError ?? "Execution produced an error with no test results.",
                    ConceptInsight = "An unhandled condition occurred during execution.",
                    ActionableRecommendation = "Check function signature and imports."
                };
            }

            // 4. Runtime error analysis
            if (!string.IsNullOrEmpty(firstFail.ErrorMessage))
            {
                var err = firstFail.ErrorMessage;

                // Index error (distinguish empty input from out-of-bounds)
                if (err.Contains("IndexError", StringComparison.OrdinalIgnoreCase))
                {
                    bool isEmpty = (firstFail.EdgeCaseTag != null && firstFail.EdgeCaseTag.Contains("Empty", StringComparison.OrdinalIgnoreCase))
                                   || firstFail.RawInputDisplay.Contains("[]")
                                   || firstFail.RawInputDisplay.Contains("\"\"")
                                   || firstFail.InputDisplay.Contains("[]");
                    return new FailureDiagnostic
                    {
                        Category = FailureTaxonomy.EmptyInputOrBoundary,
                        CategoryDisplayName = isEmpty ? "Empty Sequence Index Fault" : "Sequence Index Out of Range",
                        ObservedFact = firstFail.IsHidden
                            ? $"Hidden test #{firstFail.Index + 1} raised {err}."
                            : $"Test #{firstFail.Index + 1} raised {err} on input: {firstFail.InputDisplay}.",
                        ConceptInsight = isEmpty
                            ? "Accessing sequence indices without checking for empty collections causes an immediate IndexError."
                            : "Attempted to access an index outside the valid range [0, len-1]. This frequently occurs when loop conditions use `<=` instead of `<`, or pointers advance past bounds.",
                        ActionableRecommendation = isEmpty
                            ? "Add an early guard clause `if not arr: return ...` at the top of your function to handle empty collections safely."
                            : "Inspect loop termination conditions (`< len(arr)` vs `<= len(arr)`) and boundary pointer updates.",
                        FailedTestIndex = firstFail.Index,
                        IsHiddenTest = firstFail.IsHidden
                    };
                }

                // Key error
                if (err.Contains("KeyError", StringComparison.OrdinalIgnoreCase))
                {
                    return new FailureDiagnostic
                    {
                        Category = FailureTaxonomy.EmptyInputOrBoundary,
                        CategoryDisplayName = "Dictionary Key Not Found",
                        ObservedFact = firstFail.IsHidden
                            ? $"Hidden test #{firstFail.Index + 1} raised {err}."
                            : $"Test #{firstFail.Index + 1} raised {err} on input: {firstFail.InputDisplay}.",
                        ConceptInsight = "Accessing `dict[key]` directly raises KeyError when the key does not exist in the dictionary.",
                        ActionableRecommendation = "Use `dict.get(key, default)` or verify key presence with `if key in dict:` before lookup.",
                        FailedTestIndex = firstFail.Index,
                        IsHiddenTest = firstFail.IsHidden
                    };
                }

                // Zero division error
                if (err.Contains("ZeroDivisionError", StringComparison.OrdinalIgnoreCase))
                {
                    return new FailureDiagnostic
                    {
                        Category = FailureTaxonomy.EmptyInputOrBoundary,
                        CategoryDisplayName = "Zero Division Fault",
                        ObservedFact = firstFail.IsHidden
                            ? $"Hidden test #{firstFail.Index + 1} raised {err}."
                            : $"Test #{firstFail.Index + 1} raised {err} on input: {firstFail.InputDisplay}.",
                        ConceptInsight = "An arithmetic division or modulo operation evaluated with a denominator of zero, often triggered by empty array lengths or identical pointer positions.",
                        ActionableRecommendation = "Add a guard clause checking that denominators are non-zero before performing division or modulo operations.",
                        FailedTestIndex = firstFail.Index,
                        IsHiddenTest = firstFail.IsHidden
                    };
                }

                // Type / Hashability error
                if (err.Contains("unhashable type", StringComparison.OrdinalIgnoreCase))
                {
                    return new FailureDiagnostic
                    {
                        Category = FailureTaxonomy.TypeOrHashabilityError,
                        CategoryDisplayName = "Unhashable Type Error",
                        ObservedFact = $"Encountered {err}.",
                        ConceptInsight = "In Python, mutable objects like `list` or `dict` cannot be hashed, meaning they cannot be used as dictionary keys or set elements.",
                        ActionableRecommendation = "Convert mutable collections to immutable equivalents (e.g. convert `list` to `tuple`) before inserting into a `set` or `dict`.",
                        FailedTestIndex = firstFail.Index,
                        IsHiddenTest = firstFail.IsHidden
                    };
                }

                if (err.Contains("TypeError", StringComparison.OrdinalIgnoreCase))
                {
                    return new FailureDiagnostic
                    {
                        Category = FailureTaxonomy.TypeOrHashabilityError,
                        CategoryDisplayName = "Type Mismatch Exception",
                        ObservedFact = firstFail.IsHidden
                            ? $"Encountered {err} on a hidden test."
                            : $"Encountered {err} with input {firstFail.InputDisplay}.",
                        ConceptInsight = "Python is strictly typed: operations combining incompatible types (such as adding `int` and `str`, or indexing a `NoneType`) raise TypeError at runtime.",
                        ActionableRecommendation = "Verify variable types and inspect operations where a function might return `None` or an unexpected type.",
                        FailedTestIndex = firstFail.Index,
                        IsHiddenTest = firstFail.IsHidden
                    };
                }

                if (err.Contains("RecursionError", StringComparison.OrdinalIgnoreCase))
                {
                    return new FailureDiagnostic
                    {
                        Category = FailureTaxonomy.TimeLimitExceeded,
                        CategoryDisplayName = "Maximum Recursion Depth Exceeded",
                        ObservedFact = "Function entered unbounded recursion.",
                        ConceptInsight = "Recursive calls consume stack frames until Python's recursion limit is exceeded.",
                        ActionableRecommendation = "Ensure recursive base cases are evaluated before making recursive calls, and confirm progress is made toward the base case.",
                        FailedTestIndex = firstFail.Index,
                        IsHiddenTest = firstFail.IsHidden
                    };
                }

                return new FailureDiagnostic
                {
                    Category = FailureTaxonomy.UnhandledException,
                    CategoryDisplayName = "Runtime Exception",
                    ObservedFact = firstFail.IsHidden
                        ? $"Hidden test #{firstFail.Index + 1} raised {err}."
                        : $"Test #{firstFail.Index + 1} raised {err}.",
                    ConceptInsight = "An unhandled exception occurred during evaluation.",
                    ActionableRecommendation = "Review the exception message and inspect relevant line numbers.",
                    FailedTestIndex = firstFail.Index,
                    IsHiddenTest = firstFail.IsHidden
                };
            }

            // 5. Check for unwanted input mutation side-effects
            if (firstFail.Mutated)
            {
                return new FailureDiagnostic
                {
                    Category = FailureTaxonomy.InputMutationSideEffect,
                    CategoryDisplayName = "Unintended Input Mutation Side-Effect",
                    ObservedFact = firstFail.IsHidden
                        ? $"Function modified caller input arguments in-place on hidden test #{firstFail.Index + 1}."
                        : $"Function modified caller input arguments in-place on test #{firstFail.Index + 1} (Input: {firstFail.InputDisplay}).",
                    ConceptInsight = "In Python, arguments such as lists and dictionaries are passed by reference. In-place operations (e.g. `.sort()`, `.pop()`, or index assignment) alter the caller's data structure, causing subsequent assertions or side-effect checks to fail.",
                    ActionableRecommendation = "Avoid in-place modifications to input parameters. Use non-destructive operations (such as `sorted(arr)` instead of `arr.sort()`) or create an explicit copy with `arr.copy()` or `list(arr)` before modifying.",
                    FailedTestIndex = firstFail.Index,
                    IsHiddenTest = firstFail.IsHidden
                };
            }

            // 5. Output comparison analysis
            var actual = firstFail.ActualDisplay;
            var expected = firstFail.ExpectedDisplay;

            // Null / None return
            if (string.Equals(actual, "null", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(actual))
            {
                return new FailureDiagnostic
                {
                    Category = FailureTaxonomy.OutputFormatting,
                    CategoryDisplayName = "Missing Return Value (None)",
                    ObservedFact = firstFail.IsHidden
                        ? $"Function returned None on hidden test #{firstFail.Index + 1} where a value was expected."
                        : $"Function returned None on test #{firstFail.Index + 1} (Input: {firstFail.InputDisplay}).",
                    ConceptInsight = "Functions in Python implicitly return `None` if execution reaches the end without an explicit `return` statement.",
                    ActionableRecommendation = "Ensure all code paths and loop exits return the computed result rather than terminating silently.",
                    FailedTestIndex = firstFail.Index,
                    IsHiddenTest = firstFail.IsHidden
                };
            }

            // Off-by-one or slice length differences:
            // 1. Integer return off by exactly 1 (e.g. binary search index)
            if (int.TryParse(actual, out var actualInt) && int.TryParse(expected, out var expectedInt) && Math.Abs(actualInt - expectedInt) == 1)
            {
                return new FailureDiagnostic
                {
                    Category = FailureTaxonomy.OffByOneOrSlice,
                    CategoryDisplayName = "Off-By-One Numerical Discrepancy",
                    ObservedFact = firstFail.IsHidden
                        ? $"Returned numerical value differs from expected by exactly 1 on a hidden test."
                        : $"Observed return value {actualInt} differs from expected {expectedInt} by 1 (Candidate Off-By-One).",
                    ConceptInsight = "A discrepancy of exactly 1 is characteristic of an off-by-one condition in loop bounds, range termination, or counter initialization, but may also stem from divergent branch calculations.",
                    ActionableRecommendation = "Review loop termination conditions (`<` vs `<=`), range initialization (`0` vs `1`), or boundary index updates.",
                    FailedTestIndex = firstFail.Index,
                    IsHiddenTest = firstFail.IsHidden
                };
            }

            // 2. Collection length off by exactly 1 with non-trivial elements
            try
            {
                using var actDoc = JsonDocument.Parse(actual);
                using var expDoc = JsonDocument.Parse(expected);
                if (actDoc.RootElement.ValueKind == JsonValueKind.Array && expDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    int actLen = actDoc.RootElement.GetArrayLength();
                    int expLen = expDoc.RootElement.GetArrayLength();
                    if (actLen > 0 && expLen > 0 && Math.Abs(actLen - expLen) == 1)
                    {
                        return new FailureDiagnostic
                        {
                            Category = FailureTaxonomy.OffByOneOrSlice,
                            CategoryDisplayName = "Off-By-One or Slice Boundary Discrepancy",
                            ObservedFact = firstFail.IsHidden
                                ? $"Result element count ({actLen}) differs from expected ({expLen}) by exactly 1 element on a hidden test."
                                : $"Observed list length ({actLen}) differs from expected ({expLen}) by exactly 1 element.",
                            ConceptInsight = "Off-by-one errors frequently stem from Python's half-open intervals `[start, end)` in `range()` or slices `arr[a:b]`, where the upper bound is exclusive.",
                            ActionableRecommendation = "Check whether your loops or slices should include or exclude the boundary index (`len(nums)` vs `len(nums) - 1`).",
                            FailedTestIndex = firstFail.Index,
                            IsHiddenTest = firstFail.IsHidden
                        };
                    }
                }
            }
            catch { }

            // Value mismatch
            return new FailureDiagnostic
            {
                Category = FailureTaxonomy.ValueMismatch,
                CategoryDisplayName = "Logical Output Divergence",
                ObservedFact = firstFail.IsHidden
                    ? $"Failed hidden test #{firstFail.Index + 1} (edge case tag: {firstFail.EdgeCaseTag ?? "Hidden Verification"}). Output did not match canonical solution."
                    : $"Test #{firstFail.Index + 1}: For input `{firstFail.InputDisplay}`, expected `{expected}` but received `{actual}`.",
                ConceptInsight = "The algorithm produced a valid return type but diverged from the specified requirements on this input case.",
                ActionableRecommendation = firstFail.IsHidden
                    ? "Consider edge cases like duplicates, single elements, negative inputs, or non-overlapping ranges."
                    : $"Trace your logic step-by-step using input `{firstFail.InputDisplay}` to find where the calculation deviates.",
                FailedTestIndex = firstFail.Index,
                IsHiddenTest = firstFail.IsHidden
            };
        }
    }
}
