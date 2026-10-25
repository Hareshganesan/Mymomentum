using System;
using System.Collections.Generic;
using Momentum.Models;

namespace Momentum.Content
{
    public static class AssessmentCatalog
    {
        public static readonly List<AssessmentQuestion> Questions = new()
        {
            new AssessmentQuestion
            {
                Id = "two-sum-target",
                Title = "Two Sum - Indices of Target Pair",
                Difficulty = Difficulty.Easy,
                Topic = "Arrays & Hash Maps",
                EstimatedMinutes = "15 min",
                FunctionName = "two_sum",
                ParameterNames = new() { "nums", "target" },
                Description = @"### Problem Statement
Given a list of integers `nums` and an integer `target`, return the **0-indexed indices** of the two numbers such that they add up to `target`.

You may assume that each input has **at most one valid answer**, and you may not use the same element twice. Return the indices in ascending order `[i, j]` where `i < j`. If no valid pair exists, return an empty list `[]`.

### Constraints
- `0 <= len(nums) <= 10^4`
- `-10^9 <= nums[i] <= 10^9`
- `-10^9 <= target <= 10^9`

### Examples
- **Example 1:** `nums = [2, 7, 11, 15]`, `target = 9` -> `[0, 1]` (since `2 + 7 == 9`)
- **Example 2:** `nums = [3, 2, 4]`, `target = 6` -> `[1, 2]`
- **Example 3:** `nums = [3, 3]`, `target = 6` -> `[0, 1]`
- **Example 4:** `nums = []`, `target = 5` -> `[]`",
                StarterCode = @"def two_sum(nums: list[int], target: int) -> list[int]:
    # Write your solution below.
    # Return [i, j] where i < j, or [] if no pair exists.
    seen = {}
    pass
",
                ReferenceSolution = @"def two_sum(nums: list[int], target: int) -> list[int]:
    seen = {}
    for i, num in enumerate(nums):
        diff = target - num
        if diff in seen:
            return [seen[diff], i]
        seen[num] = i
    return []
",
                Hint = "Use a hash table to store `{value: index}` of elements you have already inspected. For each number, check if `target - num` already exists in your table in O(1) time.",
                ConceptExplanation = "One-pass hash table reduces time complexity from O(n^2) brute force to O(n) time and O(n) space.",
                TestCases = new()
                {
                    new AssessmentTestCase
                    {
                        Index = 0,
                        InputJson = "[[2, 7, 11, 15], 9]",
                        ExpectedOutputJson = "[0, 1]",
                        IsHidden = false,
                        Description = "Standard pair at beginning"
                    },
                    new AssessmentTestCase
                    {
                        Index = 1,
                        InputJson = "[[3, 2, 4], 6]",
                        ExpectedOutputJson = "[1, 2]",
                        IsHidden = false,
                        Description = "Elements in middle and end"
                    },
                    new AssessmentTestCase
                    {
                        Index = 2,
                        InputJson = "[[3, 3], 6]",
                        ExpectedOutputJson = "[0, 1]",
                        IsHidden = false,
                        EdgeCaseTag = "Duplicates",
                        Description = "Duplicate values making target"
                    },
                    new AssessmentTestCase
                    {
                        Index = 3,
                        InputJson = "[[], 5]",
                        ExpectedOutputJson = "[]",
                        IsHidden = true,
                        EdgeCaseTag = "Empty Input",
                        Description = "Empty list of numbers"
                    },
                    new AssessmentTestCase
                    {
                        Index = 4,
                        InputJson = "[[-1, -2, -3, -4, -5], -8]",
                        ExpectedOutputJson = "[2, 4]",
                        IsHidden = true,
                        EdgeCaseTag = "Negative Numbers",
                        Description = "All negative values"
                    },
                    new AssessmentTestCase
                    {
                        Index = 5,
                        InputJson = "[[1, 5, 10], 20]",
                        ExpectedOutputJson = "[]",
                        IsHidden = true,
                        EdgeCaseTag = "No Solution",
                        Description = "No two numbers sum to target"
                    }
                }
            },

            new AssessmentQuestion
            {
                Id = "valid-anagram-frequency",
                Title = "Valid Anagram & Character Frequency",
                Difficulty = Difficulty.Easy,
                Topic = "Strings & Hash Maps",
                EstimatedMinutes = "15 min",
                FunctionName = "is_anagram",
                ParameterNames = new() { "s", "t" },
                Description = @"### Problem Statement
Given two strings `s` and `t`, return `True` if `t` is an **anagram** of `s`, and `False` otherwise.

An **Anagram** is a word or phrase formed by rearranging the letters of a different word or phrase, typically using all the original letters exactly once.

For this problem:
- Comparison should be **case-insensitive** (`'A'` is equal to `'a'`).
- Non-alphanumeric characters (spaces, punctuation) should be **ignored**.

### Constraints
- `0 <= len(s), len(t) <= 5 * 10^4`

### Examples
- **Example 1:** `s = ""Anagram"", t = ""Nag a ram""` -> `True`
- **Example 2:** `s = ""rat"", t = ""car""` -> `False`
- **Example 3:** `s = """", t = """"` -> `True`",
                StarterCode = @"def is_anagram(s: str, t: str) -> bool:
    # Return True if t is a valid case-insensitive anagram of s, ignoring non-alphanumeric chars.
    pass
",
                ReferenceSolution = @"def is_anagram(s: str, t: str) -> bool:
    counts = {}
    for ch in s:
        if ch.isalnum():
            c = ch.lower()
            counts[c] = counts.get(c, 0) + 1
    for ch in t:
        if ch.isalnum():
            c = ch.lower()
            if c not in counts:
                return False
            counts[c] -= 1
            if counts[c] < 0:
                return False
    return all(v == 0 for v in counts.values())
",
                Hint = "Filter each string with `ch.lower()` for `ch in text if ch.isalnum()`. Compare character frequency counts using a dictionary or Python's `collections.Counter`.",
                ConceptExplanation = "Comparing character frequencies with a dictionary ensures O(n) linear scan time while handling unicode/ascii gracefully.",
                TestCases = new()
                {
                    new AssessmentTestCase
                    {
                        Index = 0,
                        InputJson = "[\"Anagram\", \"Nag a ram\"]",
                        ExpectedOutputJson = "true",
                        IsHidden = false,
                        Description = "Mixed case and spaces"
                    },
                    new AssessmentTestCase
                    {
                        Index = 1,
                        InputJson = "[\"rat\", \"car\"]",
                        ExpectedOutputJson = "false",
                        IsHidden = false,
                        Description = "Different characters"
                    },
                    new AssessmentTestCase
                    {
                        Index = 2,
                        InputJson = "[\"\", \"\"]",
                        ExpectedOutputJson = "true",
                        IsHidden = false,
                        EdgeCaseTag = "Empty String",
                        Description = "Both empty strings"
                    },
                    new AssessmentTestCase
                    {
                        Index = 3,
                        InputJson = "[\"a! b? c.\", \"C B A\"]",
                        ExpectedOutputJson = "true",
                        IsHidden = true,
                        EdgeCaseTag = "Punctuation Filtering",
                        Description = "Punctuation and irregular spacing"
                    },
                    new AssessmentTestCase
                    {
                        Index = 4,
                        InputJson = "[\"aa\", \"a\"]",
                        ExpectedOutputJson = "false",
                        IsHidden = true,
                        EdgeCaseTag = "Frequency Mismatch",
                        Description = "Same characters but differing counts"
                    }
                }
            },

            new AssessmentQuestion
            {
                Id = "group-word-frequencies",
                Title = "Aggregate Word Frequencies & Ranking",
                Difficulty = Difficulty.Medium,
                Topic = "Practical Data Transformation",
                EstimatedMinutes = "20 min",
                FunctionName = "word_frequency_analysis",
                ParameterNames = new() { "text", "stop_words" },
                Description = @"### Problem Statement
In practical software systems and technical assessments, processing and transforming raw textual data is a frequent task.

Write a function `word_frequency_analysis(text: str, stop_words: list[str]) -> list[list]`:
1. Clean the text: treat any sequence of alphanumeric characters (`[a-zA-Z0-9]`) as words. All words must be converted to **lowercase**.
2. Filter out words that appear in `stop_words` (stop words comparison must also be case-insensitive).
3. Count the frequency of each remaining word.
4. Return the results as a list of pairs `[word, count]`, sorted:
   - **Primary key:** Frequency count **descending** (highest count first).
   - **Secondary key:** Alphabetical order **ascending** (A to Z) for ties.

### Constraints
- Empty text or all-stop-words text should return an empty list `[]`.
- Non-alphanumeric punctuation (commas, periods, quotes) must not be attached to words.

### Examples
- **Example 1:**
  `text = ""Data science is data-driven, science is fun!""`, `stop_words = [""is""]`
  -> `[[""data"", 2], [""science"", 2], [""driven"", 1], [""fun"", 1]]`",
                StarterCode = @"def word_frequency_analysis(text: str, stop_words: list[str]) -> list[list]:
    # Return list of [word, count] sorted by count desc, then word asc.
    pass
",
                ReferenceSolution = @"import re
from collections import Counter

def word_frequency_analysis(text: str, stop_words: list[str]) -> list[list]:
    if not text:
        return []
    stops = {w.lower() for w in stop_words}
    raw_words = re.findall(r'[a-zA-Z0-9]+', text.lower())
    filtered = [w for w in raw_words if w not in stops]
    if not filtered:
        return []
    counts = Counter(filtered)
    # Sort by -count, then word
    sorted_items = sorted(counts.items(), key=lambda item: (-item[1], item[0]))
    return [[word, count] for word, count in sorted_items]
",
                Hint = "Use `re.findall(r'[a-zA-Z0-9]+', text.lower())` to extract words cleanly. Use a set for `stop_words` for O(1) lookup. Sort with `key=lambda x: (-x[1], x[0])`.",
                ConceptExplanation = "Demonstrates composite key sorting (`(-count, word)`) and robust regex tokenization for data pipelines.",
                TestCases = new()
                {
                    new AssessmentTestCase
                    {
                        Index = 0,
                        InputJson = "[\"Data science is data-driven, science is fun!\", [\"is\"]]",
                        ExpectedOutputJson = "[[\"data\", 2], [\"science\", 2], [\"driven\", 1], [\"fun\", 1]]",
                        IsHidden = false,
                        Description = "Punctuation, ties, and hyphen splitting"
                    },
                    new AssessmentTestCase
                    {
                        Index = 1,
                        InputJson = "[\"\", [\"the\", \"a\"]]",
                        ExpectedOutputJson = "[]",
                        IsHidden = false,
                        EdgeCaseTag = "Empty Input",
                        Description = "Empty string input"
                    },
                    new AssessmentTestCase
                    {
                        Index = 2,
                        InputJson = "[\"The the THE tHe!\", [\"THE\"]]",
                        ExpectedOutputJson = "[]",
                        IsHidden = false,
                        EdgeCaseTag = "Case-insensitive Stop Words",
                        Description = "All words excluded by stop words"
                    },
                    new AssessmentTestCase
                    {
                        Index = 3,
                        InputJson = "[\"apple banana apple cherry banana apple date\", []]",
                        ExpectedOutputJson = "[[\"apple\", 3], [\"banana\", 2], [\"cherry\", 1], [\"date\", 1]]",
                        IsHidden = true,
                        Description = "Multiple frequency tiers with empty stop words"
                    },
                    new AssessmentTestCase
                    {
                        Index = 4,
                        InputJson = "[\"user_id: 100, order_id: 100, item_id: 200\", [\"id\"]]",
                        ExpectedOutputJson = "[[\"100\", 2], [\"200\", 1], [\"item\", 1], [\"order\", 1], [\"user\", 1]]",
                        IsHidden = true,
                        EdgeCaseTag = "Alphanumeric Numbers",
                        Description = "Numeric tokens and underscores"
                    }
                }
            },

            new AssessmentQuestion
            {
                Id = "merge-intervals",
                Title = "Merge Overlapping Time Intervals",
                Difficulty = Difficulty.Medium,
                Topic = "Arrays & Sorting",
                EstimatedMinutes = "20 min",
                FunctionName = "merge_intervals",
                ParameterNames = new() { "intervals" },
                Description = @"### Problem Statement
Given an array of `intervals` where `intervals[i] = [start_i, end_i]`, merge all overlapping intervals, and return an array of the non-overlapping intervals that cover all the intervals in the input.

The resulting intervals must be returned in **ascending order** by start time.

### Constraints
- `0 <= len(intervals) <= 10^4`
- `intervals[i].length == 2`
- `start_i <= end_i`

### Examples
- **Example 1:** `intervals = [[1, 3], [2, 6], [8, 10], [15, 18]]` -> `[[1, 6], [8, 10], [15, 18]]`
  *Explanation: Intervals [1, 3] and [2, 6] overlap, merging into [1, 6].*
- **Example 2:** `intervals = [[1, 4], [4, 5]]` -> `[[1, 5]]`
  *Explanation: Touching boundary points count as overlapping.*
- **Example 3:** `intervals = []` -> `[]`",
                StarterCode = @"def merge_intervals(intervals: list[list[int]]) -> list[list[int]]:
    # Merge overlapping intervals and return sorted result.
    pass
",
                ReferenceSolution = @"def merge_intervals(intervals: list[list[int]]) -> list[list[int]]:
    if not intervals:
        return []
    # Sort intervals by start time
    sorted_inv = sorted(intervals, key=lambda x: x[0])
    merged = [sorted_inv[0]]
    for current in sorted_inv[1:]:
        last = merged[-1]
        if current[0] <= last[1]:
            last[1] = max(last[1], current[1])
        else:
            merged.append(current)
    return merged
",
                Hint = "Sort intervals by their start time first. Then iterate through them: if the current interval starts before or at the end of the previous one, extend the previous interval's end to `max(last.end, current.end)`.",
                ConceptExplanation = "Sorting by the start boundary reduces a multi-dimensional comparison problem into a single linear sweep in O(n log n) time.",
                TestCases = new()
                {
                    new AssessmentTestCase
                    {
                        Index = 0,
                        InputJson = "[[[1, 3], [2, 6], [8, 10], [15, 18]]]",
                        ExpectedOutputJson = "[[1, 6], [8, 10], [15, 18]]",
                        IsHidden = false,
                        Description = "Standard overlapping merge"
                    },
                    new AssessmentTestCase
                    {
                        Index = 1,
                        InputJson = "[[[1, 4], [4, 5]]]",
                        ExpectedOutputJson = "[[1, 5]]",
                        IsHidden = false,
                        EdgeCaseTag = "Touching Boundary",
                        Description = "Touching boundaries at 4"
                    },
                    new AssessmentTestCase
                    {
                        Index = 2,
                        InputJson = "[[]]",
                        ExpectedOutputJson = "[]",
                        IsHidden = false,
                        EdgeCaseTag = "Empty Input",
                        Description = "Empty list of intervals"
                    },
                    new AssessmentTestCase
                    {
                        Index = 3,
                        InputJson = "[[[1, 10], [2, 3], [4, 8]]]",
                        ExpectedOutputJson = "[[1, 10]]",
                        IsHidden = true,
                        EdgeCaseTag = "Fully Enclosed",
                        Description = "Interval fully engulfing subsequent intervals"
                    },
                    new AssessmentTestCase
                    {
                        Index = 4,
                        InputJson = "[[[5, 8], [1, 2], [3, 4]]]",
                        ExpectedOutputJson = "[[1, 2], [3, 4], [5, 8]]",
                        IsHidden = true,
                        Description = "Unsorted input with no overlaps"
                    }
                }
            },

            new AssessmentQuestion
            {
                Id = "reverse-words",
                Title = "Reverse Words in a String",
                Difficulty = Difficulty.Medium,
                Topic = "Strings & Two Pointers",
                EstimatedMinutes = "15 min",
                FunctionName = "reverse_words",
                ParameterNames = new() { "s" },
                Description = @"### Problem Statement
Given an input string `s`, reverse the order of the **words**.

A **word** is defined as a sequence of non-space characters. The words in `s` will be separated by at least one space.

Return a string of the words in reverse order concatenated by a single space.
- `s` may contain leading or trailing spaces or multiple spaces between two words.
- The returned string should only have a **single space** separating the words.
- Do not include any extra spaces.

### Constraints
- `1 <= len(s) <= 10^4`
- `s` contains English letters (upper-case and lower-case), digits, and spaces `' '`.

### Examples
- **Example 1:** `s = ""the sky is blue""` -> `""blue is sky the""`
- **Example 2:** `s = ""  hello world  ""` -> `""world hello""`
- **Example 3:** `s = ""a good   example""` -> `""example good a""`",
                StarterCode = @"def reverse_words(s: str) -> str:
    # Reverse the order of words with single space separation and trimmed ends.
    pass
",
                ReferenceSolution = @"def reverse_words(s: str) -> str:
    words = s.strip().split()
    return "" "".join(reversed(words))
",
                Hint = "In Python, `s.split()` without arguments automatically handles consecutive whitespaces and trims leading/trailing spaces. Reverse the resulting list and join with a space.",
                ConceptExplanation = "String parsing and two-pointer reversal: handles irregular whitespace normalization in O(n) time.",
                TestCases = new()
                {
                    new AssessmentTestCase
                    {
                        Index = 0,
                        InputJson = "[\"the sky is blue\"]",
                        ExpectedOutputJson = "\"blue is sky the\"",
                        IsHidden = false,
                        Description = "Normal sentence"
                    },
                    new AssessmentTestCase
                    {
                        Index = 1,
                        InputJson = "[\"  hello world  \"]",
                        ExpectedOutputJson = "\"world hello\"",
                        IsHidden = false,
                        EdgeCaseTag = "Leading/Trailing Spaces",
                        Description = "Extra spaces at boundaries"
                    },
                    new AssessmentTestCase
                    {
                        Index = 2,
                        InputJson = "[\"a good   example\"]",
                        ExpectedOutputJson = "\"example good a\"",
                        IsHidden = false,
                        EdgeCaseTag = "Multiple Inner Spaces",
                        Description = "Multiple consecutive spaces between words"
                    },
                    new AssessmentTestCase
                    {
                        Index = 3,
                        InputJson = "[\"SingleWord\"]",
                        ExpectedOutputJson = "\"SingleWord\"",
                        IsHidden = true,
                        EdgeCaseTag = "Single Word",
                        Description = "Single word with no spaces"
                    },
                    new AssessmentTestCase
                    {
                        Index = 4,
                        InputJson = "[\"   \"]",
                        ExpectedOutputJson = "\"\"",
                        IsHidden = true,
                        EdgeCaseTag = "Whitespace Only",
                        Description = "String containing only spaces"
                    }
                }
            },

            new AssessmentQuestion
            {
                Id = "find-in-rotated-array",
                Title = "Binary Search in Rotated Sorted Array",
                Difficulty = Difficulty.Medium,
                Topic = "Searching & Boundary Conditions",
                EstimatedMinutes = "25 min",
                FunctionName = "search_rotated",
                ParameterNames = new() { "nums", "target" },
                Description = @"### Problem Statement
There is an integer array `nums` sorted in ascending order (with **distinct** values).

Prior to being passed to your function, `nums` is possibly rotated at an unknown pivot index `k` (`1 <= k < len(nums)`) such that the resulting array is `[nums[k], nums[k+1], ..., nums[n-1], nums[0], nums[1], ..., nums[k-1]]`.

Given the array `nums` after the possible rotation and an integer `target`, return the **index** of `target` if it is in `nums`, or `-1` if it is not in `nums`.

You must write an algorithm with **O(log n)** runtime complexity.

### Constraints
- `0 <= len(nums) <= 5000`
- All values of `nums` are unique.

### Examples
- **Example 1:** `nums = [4, 5, 6, 7, 0, 1, 2]`, `target = 0` -> `4`
- **Example 2:** `nums = [4, 5, 6, 7, 0, 1, 2]`, `target = 3` -> `-1`
- **Example 3:** `nums = [1]`, `target = 0` -> `-1`",
                StarterCode = @"def search_rotated(nums: list[int], target: int) -> int:
    # Perform modified binary search in O(log n) time.
    # Return index of target, or -1 if not present.
    pass
",
                ReferenceSolution = @"def search_rotated(nums: list[int], target: int) -> int:
    if not nums:
        return -1
    left, right = 0, len(nums) - 1
    while left <= right:
        mid = (left + right) // 2
        if nums[mid] == target:
            return mid
        # Left half is sorted
        if nums[left] <= nums[mid]:
            if nums[left] <= target < nums[mid]:
                right = mid - 1
            else:
                left = mid + 1
        # Right half is sorted
        else:
            if nums[mid] < target <= nums[right]:
                left = mid + 1
            else:
                right = mid - 1
    return -1
",
                Hint = "At least one half (left to mid, or mid to right) is always normally sorted! Check whether the target falls within the sorted half; if it does, discard the other half.",
                ConceptExplanation = "Modified binary search: by determining which subarray half is monotonic, we maintain the O(log n) invariant even around rotational discontinuities.",
                TestCases = new()
                {
                    new AssessmentTestCase
                    {
                        Index = 0,
                        InputJson = "[[4, 5, 6, 7, 0, 1, 2], 0]",
                        ExpectedOutputJson = "4",
                        IsHidden = false,
                        Description = "Target in second rotated section"
                    },
                    new AssessmentTestCase
                    {
                        Index = 1,
                        InputJson = "[[4, 5, 6, 7, 0, 1, 2], 3]",
                        ExpectedOutputJson = "-1",
                        IsHidden = false,
                        Description = "Target not present in array"
                    },
                    new AssessmentTestCase
                    {
                        Index = 2,
                        InputJson = "[[], 5]",
                        ExpectedOutputJson = "-1",
                        IsHidden = false,
                        EdgeCaseTag = "Empty Array",
                        Description = "Empty input array"
                    },
                    new AssessmentTestCase
                    {
                        Index = 3,
                        InputJson = "[[1], 1]",
                        ExpectedOutputJson = "0",
                        IsHidden = true,
                        EdgeCaseTag = "Single Element",
                        Description = "Single element matching target"
                    },
                    new AssessmentTestCase
                    {
                        Index = 4,
                        InputJson = "[[5, 1, 3], 5]",
                        ExpectedOutputJson = "0",
                        IsHidden = true,
                        EdgeCaseTag = "Target at Pivot",
                        Description = "Target is the first boundary element"
                    },
                    new AssessmentTestCase
                    {
                        Index = 5,
                        InputJson = "[[1, 2, 3, 4, 5], 4]",
                        ExpectedOutputJson = "3",
                        IsHidden = true,
                        Description = "Unrotated sorted array"
                    }
                }
            },

            new AssessmentQuestion
            {
                Id = "longest-unique-substring",
                Title = "Longest Substring Without Repeating Characters",
                Difficulty = Difficulty.Medium,
                Topic = "Sliding Window & Sets",
                EstimatedMinutes = "20 min",
                FunctionName = "length_of_longest_substring",
                ParameterNames = new() { "s" },
                Description = @"### Problem Statement
Given a string `s`, find the length of the **longest substring** without repeating characters.

A **substring** is a contiguous non-empty sequence of characters within a string.

### Constraints
- `0 <= len(s) <= 5 * 10^4`
- `s` consists of English letters, digits, symbols and spaces.

### Examples
- **Example 1:** `s = ""abcabcbb""` -> `3` (The answer is `""abc""`, with the length of 3)
- **Example 2:** `s = ""bbbbb""` -> `1` (The answer is `""b""`, with the length of 1)
- **Example 3:** `s = ""pwwkew""` -> `3` (The answer is `""wke""`, with length 3. Notice `""pwke""` is a subsequence, not a substring)
- **Example 4:** `s = """" -> `0`",
                StarterCode = @"def length_of_longest_substring(s: str) -> int:
    # Return length of longest contiguous substring with all unique chars.
    pass
",
                ReferenceSolution = @"def length_of_longest_substring(s: str) -> int:
    char_index = {}
    max_len = 0
    start = 0
    for end, ch in enumerate(s):
        if ch in char_index and char_index[ch] >= start:
            start = char_index[ch] + 1
        char_index[ch] = end
        max_len = max(max_len, end - start + 1)
    return max_len
",
                Hint = "Use the sliding window technique with two pointers (`start` and `end`). Keep a dictionary recording each character's latest seen index. When a duplicate is seen at or after `start`, jump `start` past that index.",
                ConceptExplanation = "Sliding window with hash lookup avoids nested loops and scans the string in a single linear O(n) pass.",
                TestCases = new()
                {
                    new AssessmentTestCase
                    {
                        Index = 0,
                        InputJson = "[\"abcabcbb\"]",
                        ExpectedOutputJson = "3",
                        IsHidden = false,
                        Description = "Repeating cycle"
                    },
                    new AssessmentTestCase
                    {
                        Index = 1,
                        InputJson = "[\"bbbbb\"]",
                        ExpectedOutputJson = "1",
                        IsHidden = false,
                        EdgeCaseTag = "All Identical",
                        Description = "All identical characters"
                    },
                    new AssessmentTestCase
                    {
                        Index = 2,
                        InputJson = "[\"pwwkew\"]",
                        ExpectedOutputJson = "3",
                        IsHidden = false,
                        Description = "Repeats inside string"
                    },
                    new AssessmentTestCase
                    {
                        Index = 3,
                        InputJson = "[\"\"]",
                        ExpectedOutputJson = "0",
                        IsHidden = true,
                        EdgeCaseTag = "Empty String",
                        Description = "Empty string input"
                    },
                    new AssessmentTestCase
                    {
                        Index = 4,
                        InputJson = "[\"tmmzuxt\"]",
                        ExpectedOutputJson = "5",
                        IsHidden = true,
                        EdgeCaseTag = "Window Jump Trap",
                        Description = "Requires jumping start correctly when repeating char was before current window"
                    }
                }
            },

            new AssessmentQuestion
            {
                Id = "deep-clean-dict",
                Title = "Deep Flatten & Clean Nested Dictionary",
                Difficulty = Difficulty.Medium,
                Topic = "Practical Data Transformation",
                EstimatedMinutes = "25 min",
                FunctionName = "flatten_and_clean",
                ParameterNames = new() { "data" },
                Description = @"### Problem Statement
In real engineering teams (e.g. telemetry pipelines, config parsers, or API response sanitizers), dealing with deeply nested JSON schemas is a daily requirement.

Implement a function `flatten_and_clean(data: dict) -> dict`:
1. Flatten arbitrary levels of nested dictionaries using dot notation:
   `{""user"": {""profile"": {""name"": ""Alice""}}}` becomes `{""user.profile.name"": ""Alice""}`.
2. Clean the data by omitting any key where the terminal value is `None` or an empty string `""""`.
3. If an input dictionary contains nested dictionaries that only contain empty or `None` values, that branch should not appear in the flattened output.
4. If `data` is empty or all its values are cleaned out, return an empty dictionary `{}`.

### Constraints
- Input keys are always non-empty strings.
- Values can be integers, floats, booleans, strings, `None`, or nested dictionaries.
- Lists are treated as leaf values (do not flatten lists).

### Examples
- **Example 1:**
  `data = {""a"": 1, ""b"": {""c"": ""hello"", ""d"": None}}`
  -> `{""a"": 1, ""b.c"": ""hello""}`
- **Example 2:**
  `data = {""user"": {""name"": """", ""meta"": {""status"": None}}}`
  -> `{}`",
                StarterCode = @"def flatten_and_clean(data: dict) -> dict:
    # Recursively flatten nested dictionaries into dot notation and strip None or empty string values.
    pass
",
                ReferenceSolution = @"def flatten_and_clean(data: dict) -> dict:
    result = {}
    def recurse(current, prefix):
        if not isinstance(current, dict):
            return
        for key, value in current.items():
            full_key = f""{prefix}.{key}"" if prefix else key
            if isinstance(value, dict):
                recurse(value, full_key)
            else:
                if value is not None and value != """":
                    result[full_key] = value
    recurse(data, """")
    return result
",
                Hint = "Use a recursive helper that carries a `prefix: str`. If a value is a dictionary, recurse with `f'{prefix}.{key}'`. Otherwise, if `value is not None and value != ''`, add it to the final dictionary.",
                ConceptExplanation = "Tree traversal and path aggregation: tests recursive state propagation and strict null/empty predicate filtering.",
                TestCases = new()
                {
                    new AssessmentTestCase
                    {
                        Index = 0,
                        InputJson = "[{\"a\": 1, \"b\": {\"c\": \"hello\", \"d\": null}}]",
                        ExpectedOutputJson = "{\"a\": 1, \"b.c\": \"hello\"}",
                        IsHidden = false,
                        Description = "Single nested level with null removal"
                    },
                    new AssessmentTestCase
                    {
                        Index = 1,
                        InputJson = "[{\"user\": {\"name\": \"\", \"meta\": {\"status\": null}}}]",
                        ExpectedOutputJson = "{}",
                        IsHidden = false,
                        EdgeCaseTag = "All Empty Branch",
                        Description = "Nested dictionary completely pruned"
                    },
                    new AssessmentTestCase
                    {
                        Index = 2,
                        InputJson = "[{}]",
                        ExpectedOutputJson = "{}",
                        IsHidden = false,
                        EdgeCaseTag = "Empty Dict",
                        Description = "Empty dictionary input"
                    },
                    new AssessmentTestCase
                    {
                        Index = 3,
                        InputJson = "[{\"flags\": {\"active\": false, \"count\": 0}}]",
                        ExpectedOutputJson = "{\"flags.active\": false, \"flags.count\": 0}",
                        IsHidden = true,
                        EdgeCaseTag = "Falsy Non-Nulls",
                        Description = "Ensure False and 0 are preserved (not treated as empty)"
                    },
                    new AssessmentTestCase
                    {
                        Index = 4,
                        InputJson = "[{\"x\": {\"y\": {\"z\": [1, 2, 3]}}}]",
                        ExpectedOutputJson = "{\"x.y.z\": [1, 2, 3]}",
                        IsHidden = true,
                        Description = "Lists preserved as leaf values"
                    }
                }
            },

            new AssessmentQuestion
            {
                Id = "fix-binary-range-search",
                Title = "Debug: Fix Range Binary Search",
                Difficulty = Difficulty.Medium,
                Topic = "Debugging Existing Code",
                EstimatedMinutes = "20 min",
                FunctionName = "find_first_and_last",
                ParameterNames = new() { "nums", "target" },
                Description = @"### Problem Statement (Debugging Exercise)
A junior engineer submitted the following function intended to find the **starting and ending position** of a given `target` value in a sorted integer array `nums`. If the target is not found in the array, it should return `[-1, -1]`.

The current code has subtle **off-by-one errors** and incorrect boundary update logic, causing it to fail on edge cases (such as when the target is at index 0 or at the very end of the array, or missing entirely).

Fix the implementation so it passes all test cases efficiently in **O(log n)** time.

### Constraints
- `0 <= len(nums) <= 10^5`
- `nums` is sorted in non-decreasing order.

### Examples
- **Example 1:** `nums = [5, 7, 7, 8, 8, 10]`, `target = 8` -> `[3, 4]`
- **Example 2:** `nums = [5, 7, 7, 8, 8, 10]`, `target = 6` -> `[-1, -1]`
- **Example 3:** `nums = []`, `target = 0` -> `[-1, -1]`",
                StarterCode = @"def find_first_and_last(nums: list[int], target: int) -> list[int]:
    # BUGGY CODE TO FIX:
    # Notice the boundary checks and loop termination errors below!
    def find_bound(is_first: bool) -> int:
        left, right = 0, len(nums)
        ans = -1
        while left < right: # Bug: condition and boundary updates miss edges
            mid = (left + right) // 2
            if nums[mid] == target:
                ans = mid
                if is_first:
                    right = mid # Bug: potential infinite loop or missing left boundary
                else:
                    left = mid + 1
            elif nums[mid] < target:
                left = mid + 1
            else:
                right = mid - 1
        return ans

    return [find_bound(True), find_bound(False)]
",
                ReferenceSolution = @"def find_first_and_last(nums: list[int], target: int) -> list[int]:
    if not nums:
        return [-1, -1]

    def find_bound(find_first: bool) -> int:
        left, right = 0, len(nums) - 1
        bound = -1
        while left <= right:
            mid = (left + right) // 2
            if nums[mid] == target:
                bound = mid
                if find_first:
                    right = mid - 1
                else:
                    left = mid + 1
            elif nums[mid] < target:
                left = mid + 1
            else:
                right = mid - 1
        return bound

    return [find_bound(True), find_bound(False)]
",
                Hint = "Use two clean binary searches with `left <= right`. When `nums[mid] == target`, record `ans = mid` and continue searching: move `right = mid - 1` to find the first occurrence, or `left = mid + 1` to find the last occurrence.",
                ConceptExplanation = "Standard binary search boundary stabilization: recording the candidate match and tightening bounds avoids off-by-one termination bugs.",
                TestCases = new()
                {
                    new AssessmentTestCase
                    {
                        Index = 0,
                        InputJson = "[[5, 7, 7, 8, 8, 10], 8]",
                        ExpectedOutputJson = "[3, 4]",
                        IsHidden = false,
                        Description = "Target in middle with multiple occurrences"
                    },
                    new AssessmentTestCase
                    {
                        Index = 1,
                        InputJson = "[[5, 7, 7, 8, 8, 10], 6]",
                        ExpectedOutputJson = "[-1, -1]",
                        IsHidden = false,
                        Description = "Target not present"
                    },
                    new AssessmentTestCase
                    {
                        Index = 2,
                        InputJson = "[[], 0]",
                        ExpectedOutputJson = "[-1, -1]",
                        IsHidden = false,
                        EdgeCaseTag = "Empty Array",
                        Description = "Empty input array"
                    },
                    new AssessmentTestCase
                    {
                        Index = 3,
                        InputJson = "[[8, 8, 8, 8], 8]",
                        ExpectedOutputJson = "[0, 3]",
                        IsHidden = true,
                        EdgeCaseTag = "All Target",
                        Description = "Array composed entirely of target elements"
                    },
                    new AssessmentTestCase
                    {
                        Index = 4,
                        InputJson = "[[1, 3, 5], 1]",
                        ExpectedOutputJson = "[0, 0]",
                        IsHidden = true,
                        EdgeCaseTag = "Left Edge",
                        Description = "Target at exact index 0"
                    },
                    new AssessmentTestCase
                    {
                        Index = 5,
                        InputJson = "[[1, 3, 5], 5]",
                        ExpectedOutputJson = "[2, 2]",
                        IsHidden = true,
                        EdgeCaseTag = "Right Edge",
                        Description = "Target at exact last index"
                    }
                }
            },

            new AssessmentQuestion
            {
                Id = "validate-balanced-brackets",
                Title = "Validate Bracket Nesting with Strict Pairing",
                Difficulty = Difficulty.Medium,
                Topic = "Stack & Edge Case Resilience",
                EstimatedMinutes = "15 min",
                FunctionName = "is_valid_brackets",
                ParameterNames = new() { "s" },
                Description = @"### Problem Statement
Given a string `s` containing just the characters `'('`, `')'`, `'{'`, `'}'`, `'['` and `']'`, determine if the input string is valid.

An input string is valid if:
1. Open brackets must be closed by the same type of brackets.
2. Open brackets must be closed in the correct order.
3. Every close bracket has a corresponding open bracket of the same type.
4. Characters other than brackets (if any) should be ignored.

### Constraints
- `0 <= len(s) <= 10^4`

### Examples
- **Example 1:** `s = ""()[]{}""` -> `True`
- **Example 2:** `s = ""(]""` -> `False`
- **Example 3:** `s = ""([)]""` -> `False`
- **Example 4:** `s = ""{[] (hello)}""` -> `True`
- **Example 5:** `s = """" -> `True`",
                StarterCode = @"def is_valid_brackets(s: str) -> bool:
    # Return True if all brackets are properly matched and closed, False otherwise.
    pass
",
                ReferenceSolution = @"def is_valid_brackets(s: str) -> bool:
    matching = {')': '(', '}': '{', ']': '['}
    openers = set(matching.values())
    stack = []
    for ch in s:
        if ch in openers:
            stack.append(ch)
        elif ch in matching:
            if not stack or stack[-1] != matching[ch]:
                return False
            stack.pop()
    return len(stack) == 0
",
                Hint = "Maintain a LIFO stack of open bracket characters. When a closing bracket is encountered, verify the stack is non-empty and the top element matches the closing bracket's pair.",
                ConceptExplanation = "Stack LIFO semantics naturally model nested grammar rules and balanced parenthesis validation.",
                TestCases = new()
                {
                    new AssessmentTestCase
                    {
                        Index = 0,
                        InputJson = "[\"()[]{}\"]",
                        ExpectedOutputJson = "true",
                        IsHidden = false,
                        Description = "Sequential pairs"
                    },
                    new AssessmentTestCase
                    {
                        Index = 1,
                        InputJson = "[\"(]\"]",
                        ExpectedOutputJson = "false",
                        IsHidden = false,
                        Description = "Mismatched bracket type"
                    },
                    new AssessmentTestCase
                    {
                        Index = 2,
                        InputJson = "[\"([)]\"]",
                        ExpectedOutputJson = "false",
                        IsHidden = false,
                        Description = "Interleaved brackets without proper nesting"
                    },
                    new AssessmentTestCase
                    {
                        Index = 3,
                        InputJson = "[\"\"]",
                        ExpectedOutputJson = "true",
                        IsHidden = false,
                        EdgeCaseTag = "Empty String",
                        Description = "Empty string has no violations"
                    },
                    new AssessmentTestCase
                    {
                        Index = 4,
                        InputJson = "[\")(\"]",
                        ExpectedOutputJson = "false",
                        IsHidden = true,
                        EdgeCaseTag = "Premature Close",
                        Description = "Closing bracket before open"
                    },
                    new AssessmentTestCase
                    {
                        Index = 5,
                        InputJson = "[\"((((\"]",
                        ExpectedOutputJson = "false",
                        IsHidden = true,
                        EdgeCaseTag = "Unclosed Openers",
                        Description = "All open brackets with no closers"
                    }
                }
            }
        };
    }
}
