# Failure Forensics: Diagnostic Triage Framework

## 1. Executive Overview

Traditional competitive programming and online assessment platforms produce binary verdicts (`Accepted` vs `Wrong Answer`). When an assessment submission fails, candidates are left guessing or looking up full spoilers, short-circuiting the debugging skill essential for real software engineering roles.

**Failure Forensics** is an evidence-based diagnostic framework implemented in Momentum. When a submission fails, the engine analyzes empirical execution traces, AST compilation states, and output variances to classify the error category, explain the root computer science invariant, and prescribe an actionable next step without revealing the answer.

---

## 2. Failure Taxonomy Matrix

| Failure Taxonomy | Empirical Trigger | Architectural / Conceptual Invariant | Actionable Recommendation |
|---|---|---|---|
| **SyntaxError** | Python parser fails `compile()` with line and offset | Indentation and grammatical grammar rules must be satisfied prior to bytecode execution | Inspect highlighted line for missing colons (`:`), unclosed quotes, or tab/space mixture |
| **TimeLimitExceeded** | Wall-clock execution exceeds 3,000ms threshold | Non-advancing while-loops, unbounded recursion, or quadratic $O(N^2)$ algorithmic time complexity | Verify while-loop advancement conditions and base-case termination |
| **EmptyInputOrBoundary** | `IndexError` on empty collection (`[]`, `""`) | Production data pipelines regularly receive empty batches; functions must guard against index access on empty structures | Add an early guard clause `if not arr: return ...` at the top of the function |
| **Sequence Index Out of Range** | `IndexError` on non-empty sequence | Loop or pointer boundary exceeded valid index range `[0, len-1]` | Inspect loop inequality (`< len(arr)` vs `<= len(arr)`) and boundary pointer updates |
| **Dictionary Key Not Found** | `KeyError` during mapping lookup | Direct indexing `dict[key]` raises KeyError if the key does not exist | Use `dict.get(key, default)` or verify presence with `if key in dict:` |
| **Zero Division Fault** | `ZeroDivisionError` during arithmetic | Denominator evaluated to zero, often from zero-length collections or matching pointers | Add a guard clause verifying denominators are non-zero before division |
| **InputMutationSideEffect** | Argument modified in-place (`args != args_copy`) | In Python, lists and dicts are passed by reference; mutating them in-place introduces caller side-effects | Use non-destructive operations (`sorted()` instead of `.sort()`) or copy via `arr.copy()` |
| **TypeOrHashability** | `TypeError: unhashable type: 'list'` or incompatible operations | Python collections require immutable items for hashing in sets and dictionary keys | Convert mutable collections to immutable equivalents (e.g. `tuple(arr)`) |
| **OutputFormatting** | Function returned `None` or invalid shape | Implicit `None` returned due to missing explicit `return` along a code path | Verify all execution branches return the computed object |
| **OffByOneOrSlice** | Numerical return off by 1 or list length offset by 1 | Candidate off-by-one discrepancy in loop bounds, range termination, or counter initialization | Verify range boundaries (`len(nums)` vs `len(nums) - 1`) and slice upper bounds |
| **ValueMismatch** | Algorithm produces correct types but diverges from canonical output | Algorithmic logic failure or untested edge case (e.g. duplicates, negatives, sorted order) | Trace logic step-by-step against sample input to isolate deviation |

---

## 3. Strict Hidden-Test Confidentiality

To preserve realistic assessment conditions, hidden test inputs and expected outputs are **never exposed** through the UI or diagnostic messages.

1. **Obfuscation at Harness Boundary**: When parsing runner outputs, results with `is_hidden == true` have their display strings replaced with placeholder tokens (`[Hidden test parameters]`, `[Hidden canonical output]`, `[Hidden match]` / `[Hidden output diverged]`).
2. **Diagnostic Privacy**: In `FailureForensicsEngine`, diagnostic messages for hidden tests reference test case numbers and edge-case tags (e.g. *"Hidden test #4 (edge case: Empty Input) raised IndexError on empty collection"*) without printing the raw test vector.
3. **Internal Raw Preservation**: The internal model stores `RawInputDisplay` strictly for automated diagnostic heuristics (e.g., verifying whether an input was empty), completely insulated from UI data binding.
