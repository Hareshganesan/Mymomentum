using System;
using System.Collections.Generic;
using MyTodo.Models;

namespace MyTodo.Services
{
    /// <summary>
    /// Provides curated, high-yield placement preparation seed data
    /// covering DSA patterns (Striver/NeetCode style), Core CS fundamentals,
    /// STAR behavioral templates, and placement drive pipelines.
    /// </summary>
    public static class SeedDataService
    {
        public static List<DsaProblem> GetCuratedDsaSheet()
        {
            return new List<DsaProblem>
            {
                // ==================== ARRAYS & HASHING ====================
                new DsaProblem
                {
                    Title = "Two Sum",
                    Difficulty = ProblemDifficulty.Easy,
                    Topic = DsaTopic.ArraysAndHashing,
                    Pattern = "Hash Map Lookup",
                    LeetCodeUrl = "https://leetcode.com/problems/two-sum/",
                    TimeComplexity = "O(n)",
                    SpaceComplexity = "O(n)",
                    CompanyTags = "Google, Amazon, Microsoft, Meta",
                    IntuitionNotes = "Store seen numbers with their indices in a hashmap. For each element x, check if (target - x) is in the map. Single pass O(n).",
                    CodeSnippet = "unordered_map<int, int> mp;\nfor(int i = 0; i < nums.size(); i++) {\n    int rem = target - nums[i];\n    if(mp.count(rem)) return {mp[rem], i};\n    mp[nums[i]] = i;\n}",
                    ConfidenceLevel = 4
                },
                new DsaProblem
                {
                    Title = "Group Anagrams",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.ArraysAndHashing,
                    Pattern = "Frequency Map / Canonical Key",
                    LeetCodeUrl = "https://leetcode.com/problems/group-anagrams/",
                    TimeComplexity = "O(n * k log k)",
                    SpaceComplexity = "O(n * k)",
                    CompanyTags = "Amazon, Apple, Uber",
                    IntuitionNotes = "Sort each string to use as hashmap key, or count 26 character frequencies as a tuple key to map matching anagrams together.",
                    ConfidenceLevel = 3
                },
                new DsaProblem
                {
                    Title = "Longest Consecutive Sequence",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.ArraysAndHashing,
                    Pattern = "Hash Set Boundary Check",
                    LeetCodeUrl = "https://leetcode.com/problems/longest-consecutive-sequence/",
                    TimeComplexity = "O(n)",
                    SpaceComplexity = "O(n)",
                    CompanyTags = "Google, Amazon, Spotify",
                    IntuitionNotes = "Put all numbers into a HashSet. Only start counting a sequence if (num - 1) is NOT in the set (i.e. num is the start of a streak). Ensures O(n) total checks.",
                    ConfidenceLevel = 3
                },
                new DsaProblem
                {
                    Title = "Subarray Sum Equals K",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.ArraysAndHashing,
                    Pattern = "Prefix Sum + Hash Map",
                    LeetCodeUrl = "https://leetcode.com/problems/subarray-sum-equals-k/",
                    TimeComplexity = "O(n)",
                    SpaceComplexity = "O(n)",
                    CompanyTags = "Meta, Amazon, Microsoft",
                    IntuitionNotes = "Maintain running prefix sum. If (prefixSum - k) occurred before in map, add its frequency to count. Initialize map[0] = 1.",
                    ConfidenceLevel = 3
                },

                // ==================== TWO POINTERS & SLIDING WINDOW ====================
                new DsaProblem
                {
                    Title = "3Sum",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.TwoPointers,
                    Pattern = "Sort + Two Pointers",
                    LeetCodeUrl = "https://leetcode.com/problems/3sum/",
                    TimeComplexity = "O(n^2)",
                    SpaceComplexity = "O(1) extra",
                    CompanyTags = "Amazon, Microsoft, Meta",
                    IntuitionNotes = "Sort the array. Fix first element i, then use two pointers (left = i + 1, right = n - 1). Skip duplicates for i, left, and right to avoid repeated triplets.",
                    ConfidenceLevel = 3
                },
                new DsaProblem
                {
                    Title = "Container With Most Water",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.TwoPointers,
                    Pattern = "Greedy Two Pointers",
                    LeetCodeUrl = "https://leetcode.com/problems/container-with-most-water/",
                    TimeComplexity = "O(n)",
                    SpaceComplexity = "O(1)",
                    CompanyTags = "Amazon, Google, Adobe",
                    IntuitionNotes = "Start pointers at both ends. Area = min(h[l], h[r]) * (r - l). Always move the pointer pointing to the shorter line because keeping it can never yield a larger area.",
                    ConfidenceLevel = 4
                },
                new DsaProblem
                {
                    Title = "Trapping Rain Water",
                    Difficulty = ProblemDifficulty.Hard,
                    Topic = DsaTopic.TwoPointers,
                    Pattern = "Two Pointers with Left/Right Max",
                    LeetCodeUrl = "https://leetcode.com/problems/trapping-rain-water/",
                    TimeComplexity = "O(n)",
                    SpaceComplexity = "O(1)",
                    CompanyTags = "Google, Amazon, Goldman Sachs",
                    IntuitionNotes = "Maintain leftMax and rightMax. Water trapped at position is min(leftMax, rightMax) - height[i]. Move the pointer with smaller boundary inward.",
                    ConfidenceLevel = 3
                },
                new DsaProblem
                {
                    Title = "Longest Substring Without Repeating Characters",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.SlidingWindow,
                    Pattern = "Dynamic Sliding Window",
                    LeetCodeUrl = "https://leetcode.com/problems/longest-substring-without-repeating-characters/",
                    TimeComplexity = "O(n)",
                    SpaceComplexity = "O(min(m, n))",
                    CompanyTags = "Amazon, Microsoft, Bloomberg",
                    IntuitionNotes = "Expand window right pointer. If char is already seen in window, jump left pointer to max(left, lastSeenIndex[char] + 1). Track max window length.",
                    ConfidenceLevel = 4
                },
                new DsaProblem
                {
                    Title = "Minimum Window Substring",
                    Difficulty = ProblemDifficulty.Hard,
                    Topic = DsaTopic.SlidingWindow,
                    Pattern = "Sliding Window + Frequency Match",
                    LeetCodeUrl = "https://leetcode.com/problems/minimum-window-substring/",
                    TimeComplexity = "O(m + n)",
                    SpaceComplexity = "O(1) / O(256)",
                    CompanyTags = "Meta, Google, Uber, Lyft",
                    IntuitionNotes = "Count required chars. Expand right until all chars are satisfied ('matched == need'). Then contract left to find minimum valid window while preserving match count.",
                    ConfidenceLevel = 2
                },

                // ==================== STACK & QUEUE ====================
                new DsaProblem
                {
                    Title = "Valid Parentheses",
                    Difficulty = ProblemDifficulty.Easy,
                    Topic = DsaTopic.StackAndQueue,
                    Pattern = "LIFO Stack Matching",
                    LeetCodeUrl = "https://leetcode.com/problems/valid-parentheses/",
                    TimeComplexity = "O(n)",
                    SpaceComplexity = "O(n)",
                    CompanyTags = "Amazon, Microsoft, Meta",
                    IntuitionNotes = "Push opening brackets to stack. When seeing closing bracket, check if top matches. Return stack.empty() at end.",
                    ConfidenceLevel = 5
                },
                new DsaProblem
                {
                    Title = "Daily Temperatures",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.StackAndQueue,
                    Pattern = "Monotonic Decreasing Stack",
                    LeetCodeUrl = "https://leetcode.com/problems/daily-temperatures/",
                    TimeComplexity = "O(n)",
                    SpaceComplexity = "O(n)",
                    CompanyTags = "Amazon, Google, Meta",
                    IntuitionNotes = "Maintain stack of indices with decreasing temperatures. When a warmer day arrives, pop cooler days and compute distance (currIdx - poppedIdx).",
                    ConfidenceLevel = 3
                },
                new DsaProblem
                {
                    Title = "Largest Rectangle in Histogram",
                    Difficulty = ProblemDifficulty.Hard,
                    Topic = DsaTopic.StackAndQueue,
                    Pattern = "Monotonic Increasing Stack",
                    LeetCodeUrl = "https://leetcode.com/problems/largest-rectangle-in-histogram/",
                    TimeComplexity = "O(n)",
                    SpaceComplexity = "O(n)",
                    CompanyTags = "Amazon, Google, Microsoft",
                    IntuitionNotes = "Stack stores indices of increasing heights. When smaller height found, pop and calculate area using popped bar as smallest height between boundaries.",
                    ConfidenceLevel = 2
                },

                // ==================== BINARY SEARCH ====================
                new DsaProblem
                {
                    Title = "Search in Rotated Sorted Array",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.BinarySearch,
                    Pattern = "Modified Binary Search",
                    LeetCodeUrl = "https://leetcode.com/problems/search-in-rotated-sorted-array/",
                    TimeComplexity = "O(log n)",
                    SpaceComplexity = "O(1)",
                    CompanyTags = "Meta, Amazon, Microsoft",
                    IntuitionNotes = "At least one half is always sorted! Check if left half is sorted (nums[low] <= nums[mid]). If so, check if target falls in it; otherwise search right half.",
                    ConfidenceLevel = 3
                },
                new DsaProblem
                {
                    Title = "Koko Eating Bananas",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.BinarySearch,
                    Pattern = "Binary Search on Answer Space",
                    LeetCodeUrl = "https://leetcode.com/problems/koko-eating-bananas/",
                    TimeComplexity = "O(n log(maxP))",
                    SpaceComplexity = "O(1)",
                    CompanyTags = "Google, Airbnb, DoorDash",
                    IntuitionNotes = "Binary search speed k in range [1, max(piles)]. For each speed, calculate hours needed. If hours <= h, try smaller speed; else increase speed.",
                    ConfidenceLevel = 3
                },

                // ==================== LINKED LIST ====================
                new DsaProblem
                {
                    Title = "Reverse Linked List",
                    Difficulty = ProblemDifficulty.Easy,
                    Topic = DsaTopic.LinkedList,
                    Pattern = "Pointer Manipulation",
                    LeetCodeUrl = "https://leetcode.com/problems/reverse-linked-list/",
                    TimeComplexity = "O(n)",
                    SpaceComplexity = "O(1)",
                    CompanyTags = "Amazon, Microsoft, Apple",
                    IntuitionNotes = "Maintain prev = null, curr = head. Save next = curr->next, point curr->next = prev, advance prev = curr, curr = next.",
                    ConfidenceLevel = 5
                },
                new DsaProblem
                {
                    Title = "Linked List Cycle II (Find Start of Cycle)",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.LinkedList,
                    Pattern = "Floyd's Tortoise and Hare",
                    LeetCodeUrl = "https://leetcode.com/problems/linked-list-cycle-ii/",
                    TimeComplexity = "O(n)",
                    SpaceComplexity = "O(1)",
                    CompanyTags = "Amazon, Microsoft, Cisco",
                    IntuitionNotes = "Slow moves 1 step, Fast moves 2. When they meet, reset slow to head. Move both 1 step at a time; where they meet again is the cycle start!",
                    ConfidenceLevel = 4
                },
                new DsaProblem
                {
                    Title = "LRU Cache",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.LinkedList,
                    Pattern = "Doubly Linked List + Hash Map",
                    LeetCodeUrl = "https://leetcode.com/problems/lru-cache/",
                    TimeComplexity = "O(1) get & put",
                    SpaceComplexity = "O(capacity)",
                    CompanyTags = "Amazon, Google, Microsoft, Meta, Bloomberg",
                    IntuitionNotes = "Doubly linked list stores key-value nodes ordered by recency. Hash map stores key -> Node* pointer. On access/update, move node to head. On overflow, evict tail.",
                    ConfidenceLevel = 3
                },

                // ==================== TREES & BST ====================
                new DsaProblem
                {
                    Title = "Invert / Flip Binary Tree",
                    Difficulty = ProblemDifficulty.Easy,
                    Topic = DsaTopic.TreesAndBST,
                    Pattern = "Tree DFS / Recursion",
                    LeetCodeUrl = "https://leetcode.com/problems/invert-binary-tree/",
                    TimeComplexity = "O(n)",
                    SpaceComplexity = "O(h)",
                    CompanyTags = "Google, Amazon, Meta",
                    IntuitionNotes = "Recursively swap root->left and root->right, then call invert on both children.",
                    ConfidenceLevel = 5
                },
                new DsaProblem
                {
                    Title = "Lowest Common Ancestor in Binary Tree",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.TreesAndBST,
                    Pattern = "Bottom-up Postorder DFS",
                    LeetCodeUrl = "https://leetcode.com/problems/lowest-common-ancestor-of-a-binary-tree/",
                    TimeComplexity = "O(n)",
                    SpaceComplexity = "O(h)",
                    CompanyTags = "Amazon, Meta, Microsoft",
                    IntuitionNotes = "If root is p or q or null, return root. Recurse left and right. If both return non-null, root is the LCA. If one returns non-null, pass it up.",
                    ConfidenceLevel = 4
                },
                new DsaProblem
                {
                    Title = "Binary Tree Maximum Path Sum",
                    Difficulty = ProblemDifficulty.Hard,
                    Topic = DsaTopic.TreesAndBST,
                    Pattern = "Tree DFS with Global Max",
                    LeetCodeUrl = "https://leetcode.com/problems/binary-tree-maximum-path-sum/",
                    TimeComplexity = "O(n)",
                    SpaceComplexity = "O(h)",
                    CompanyTags = "Meta, Amazon, Google",
                    IntuitionNotes = "At each node, compute max gain from left and right (ignore negatives using max(0, gain)). Update global max = max(global, node->val + left + right). Return node->val + max(left, right).",
                    ConfidenceLevel = 2
                },

                // ==================== GRAPHS ====================
                new DsaProblem
                {
                    Title = "Number of Islands",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.Graphs,
                    Pattern = "Matrix BFS / DFS Flood Fill",
                    LeetCodeUrl = "https://leetcode.com/problems/number-of-islands/",
                    TimeComplexity = "O(m * n)",
                    SpaceComplexity = "O(m * n)",
                    CompanyTags = "Amazon, Google, Microsoft, Bloomberg",
                    IntuitionNotes = "Traverse grid. When seeing '1', increment island count and trigger DFS/BFS to sink all connected land ('0' or visited).",
                    ConfidenceLevel = 4
                },
                new DsaProblem
                {
                    Title = "Course Schedule (Detect Cycle in DAG)",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.Graphs,
                    Pattern = "Topological Sort (Kahn's Algorithm / BFS)",
                    LeetCodeUrl = "https://leetcode.com/problems/course-schedule/",
                    TimeComplexity = "O(V + E)",
                    SpaceComplexity = "O(V + E)",
                    CompanyTags = "Amazon, Google, Meta, Twitter",
                    IntuitionNotes = "Build in-degree array. Push nodes with in-degree 0 into queue. Pop, decrement neighbors' in-degree. If popped count == V, no cycle exists!",
                    ConfidenceLevel = 3
                },
                new DsaProblem
                {
                    Title = "Network Delay Time (Dijkstra's Algorithm)",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.Graphs,
                    Pattern = "Shortest Path / Min-Heap Dijkstra",
                    LeetCodeUrl = "https://leetcode.com/problems/network-delay-time/",
                    TimeComplexity = "O(E log V)",
                    SpaceComplexity = "O(V + E)",
                    CompanyTags = "Amazon, Google",
                    IntuitionNotes = "Min-heap storing (distance, node). Greedily pick shortest distance, relax neighbors. Return max distance to any node if all reachable.",
                    ConfidenceLevel = 3
                },

                // ==================== DYNAMIC PROGRAMMING ====================
                new DsaProblem
                {
                    Title = "Climbing Stairs",
                    Difficulty = ProblemDifficulty.Easy,
                    Topic = DsaTopic.DynamicProgramming,
                    Pattern = "1D DP / Fibonacci",
                    LeetCodeUrl = "https://leetcode.com/problems/climbing-stairs/",
                    TimeComplexity = "O(n)",
                    SpaceComplexity = "O(1)",
                    CompanyTags = "Amazon, Adobe, Apple",
                    IntuitionNotes = "dp[i] = dp[i-1] + dp[i-2]. Can optimize space to 2 variables (prev1, prev2).",
                    ConfidenceLevel = 5
                },
                new DsaProblem
                {
                    Title = "Coin Change (Minimum Coins)",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.DynamicProgramming,
                    Pattern = "Unbounded Knapsack / Bottom-Up DP",
                    LeetCodeUrl = "https://leetcode.com/problems/coin-change/",
                    TimeComplexity = "O(amount * n)",
                    SpaceComplexity = "O(amount)",
                    CompanyTags = "Amazon, Microsoft, Goldman Sachs",
                    IntuitionNotes = "dp[i] = min coins to make amount i. dp[i] = min(dp[i], 1 + dp[i - coin]) for each coin <= i. Base case dp[0] = 0, rest INF.",
                    ConfidenceLevel = 3
                },
                new DsaProblem
                {
                    Title = "Longest Increasing Subsequence (LIS)",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.DynamicProgramming,
                    Pattern = "DP O(n^2) or Patience Sort O(n log n)",
                    LeetCodeUrl = "https://leetcode.com/problems/longest-increasing-subsequence/",
                    TimeComplexity = "O(n log n)",
                    SpaceComplexity = "O(n)",
                    CompanyTags = "Google, Amazon, Microsoft",
                    IntuitionNotes = "Patience sort: maintain array tails where tails[i] is smallest tail of all increasing subsequences of length i+1. Use binary search (lower_bound) to replace or append.",
                    ConfidenceLevel = 3
                },
                new DsaProblem
                {
                    Title = "Edit Distance (Levenshtein Distance)",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.DynamicProgramming,
                    Pattern = "2D Grid DP String Matching",
                    LeetCodeUrl = "https://leetcode.com/problems/edit-distance/",
                    TimeComplexity = "O(m * n)",
                    SpaceComplexity = "O(m * n)",
                    CompanyTags = "Google, Amazon, Microsoft",
                    IntuitionNotes = "dp[i][j] is min edits for word1[0..i] and word2[0..j]. If chars match, dp[i-1][j-1]. Else 1 + min(insert: dp[i][j-1], delete: dp[i-1][j], replace: dp[i-1][j-1]).",
                    ConfidenceLevel = 2
                },

                // ==================== HEAP & PRIORITY QUEUE ====================
                new DsaProblem
                {
                    Title = "Kth Largest Element in an Array",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.HeapsAndPriorityQueue,
                    Pattern = "Min-Heap of Size K / Quickselect",
                    LeetCodeUrl = "https://leetcode.com/problems/kth-largest-element-in-an-array/",
                    TimeComplexity = "O(n log k)",
                    SpaceComplexity = "O(k)",
                    CompanyTags = "Meta, Amazon, Google",
                    IntuitionNotes = "Keep min-heap of size k. For each num, push. If heap size > k, pop top. Top element at the end is kth largest.",
                    ConfidenceLevel = 4
                },
                new DsaProblem
                {
                    Title = "Find Median from Data Stream",
                    Difficulty = ProblemDifficulty.Hard,
                    Topic = DsaTopic.HeapsAndPriorityQueue,
                    Pattern = "Two Heaps (Max-Heap + Min-Heap)",
                    LeetCodeUrl = "https://leetcode.com/problems/find-median-from-data-stream/",
                    TimeComplexity = "O(log n) add, O(1) find",
                    SpaceComplexity = "O(n)",
                    CompanyTags = "Amazon, Google, Apple",
                    IntuitionNotes = "Left max-heap stores smaller half. Right min-heap stores larger half. Keep sizes balanced (diff <= 1). Median is either top of larger heap or average of both tops.",
                    ConfidenceLevel = 3
                },

                // ==================== GREEDY & INTERVALS ====================
                new DsaProblem
                {
                    Title = "Merge Intervals",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.GreedyAndIntervals,
                    Pattern = "Sorting Intervals by Start Time",
                    LeetCodeUrl = "https://leetcode.com/problems/merge-intervals/",
                    TimeComplexity = "O(n log n)",
                    SpaceComplexity = "O(n)",
                    CompanyTags = "Amazon, Google, Meta, Microsoft",
                    IntuitionNotes = "Sort intervals by start. Compare curr start with prev end. If curr[0] <= prev[1], merge: prev[1] = max(prev[1], curr[1]). Else push curr.",
                    ConfidenceLevel = 4
                },
                new DsaProblem
                {
                    Title = "Non-overlapping Intervals",
                    Difficulty = ProblemDifficulty.Medium,
                    Topic = DsaTopic.GreedyAndIntervals,
                    Pattern = "Greedy Interval Scheduling",
                    LeetCodeUrl = "https://leetcode.com/problems/non-overlapping-intervals/",
                    TimeComplexity = "O(n log n)",
                    SpaceComplexity = "O(1)",
                    CompanyTags = "Meta, Amazon, Microsoft",
                    IntuitionNotes = "Sort by end time. Always keep interval that ends earliest (leaves maximum room for future intervals). If next interval starts before current end, increment removal count.",
                    ConfidenceLevel = 3
                }
            };
        }

        public static List<CoreCsTopic> GetCuratedCoreCsQuestions()
        {
            return new List<CoreCsTopic>
            {
                // ==================== OPERATING SYSTEMS ====================
                new CoreCsTopic
                {
                    Subject = CsSubject.OperatingSystems,
                    Question = "What are the 4 Necessary Conditions for Deadlock? How to prevent them?",
                    ShortAnswer = "Mutual Exclusion, Hold & Wait, No Preemption, Circular Wait (Coffman Conditions).",
                    DetailedExplanation = "1. Mutual Exclusion: At least one resource must be non-shareable.\n2. Hold and Wait: A process holds resources while waiting for more.\n3. No Preemption: Resources cannot be forcibly taken.\n4. Circular Wait: A closed chain of processes each waiting for a resource held by the next.\n\nPrevention: Break any 1 condition! E.g. enforce strict resource ordering to break circular wait, or require requesting all resources upfront to break hold & wait.",
                    HighYieldKeywords = "Coffman Conditions, Circular Wait, Banker's Algorithm, Resource Ordering",
                    Frequency = "Very High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.OperatingSystems,
                    Question = "Difference between Process and Thread? What is a Context Switch?",
                    ShortAnswer = "Process has separate memory space; Threads share address space of parent process.",
                    DetailedExplanation = "• Process: Independent program in execution with its own virtual address space, file descriptors, stack, and heap. Heavyweight context switch.\n• Thread: Smallest unit of execution inside a process. Shares code, data, and OS resources with peer threads, but has its own Program Counter, registers, and stack. Lightweight switch.\n• Context Switch: Saving the state (PCB/TCB) of the current CPU process and loading the state of the scheduled one.",
                    HighYieldKeywords = "PCB, TCB, Virtual Address Space, Stack vs Heap, Context Switching Overhead",
                    Frequency = "Very High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.OperatingSystems,
                    Question = "Explain Virtual Memory, Paging, and Page Fault.",
                    ShortAnswer = "Virtual Memory gives processes illusion of contiguous memory. Paging maps virtual pages to physical frames via Page Table.",
                    DetailedExplanation = "• Virtual Memory: Decouples logical program memory from physical RAM, enabling running programs larger than physical memory.\n• Paging: Memory is divided into fixed-size blocks (Pages in virtual memory, Frames in RAM). The MMU uses the Page Table (accelerated by TLB) to translate virtual to physical addresses.\n• Page Fault: Hardware interrupt triggered when a requested page is not currently in RAM. The OS loads it from disk swap space and updates the page table.",
                    HighYieldKeywords = "MMU, TLB (Translation Lookaside Buffer), Page Table, Demand Paging, Thrashing",
                    Frequency = "Very High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.OperatingSystems,
                    Question = "Mutex vs Semaphore vs Spinlock — Key differences?",
                    ShortAnswer = "Mutex is a locking mechanism with ownership (1 thread). Semaphore is a signaling mechanism (counter N).",
                    DetailedExplanation = "• Mutex (Mutual Exclusion): Binary lock. Only the thread that locked it can unlock it. Sleeps waiting threads.\n• Counting Semaphore: Integer counter indicating available units of a resource. Wait/P() decrements, Signal/V() increments. Any thread can signal it.\n• Spinlock: Busy-waits (loops) on CPU without sleeping. Useful for very short critical sections where context switch cost exceeds spin time.",
                    HighYieldKeywords = "Critical Section, Producer-Consumer, Ownership, Busy-waiting vs Sleep",
                    Frequency = "Very High"
                },

                // ==================== DBMS & SQL ====================
                new CoreCsTopic
                {
                    Subject = CsSubject.DBMS,
                    Question = "Explain ACID Properties with real banking transaction examples.",
                    ShortAnswer = "Atomicity (all or nothing), Consistency (valid state), Isolation (concurrency control), Durability (persisted).",
                    DetailedExplanation = "• Atomicity: If transferring $100 from A to B, both debit and credit succeed, or neither does (Write-Ahead Logging / Undo log).\n• Consistency: Database transitions from one valid state to another, respecting all constraints (e.g. balance >= 0).\n• Isolation: Concurrent transactions execute without interfering. Implemented via lock protocols (2PL) or MVCC.\n• Durability: Once committed, changes survive crashes/power failures (Redo log / WAL flushed to disk).",
                    HighYieldKeywords = "Write-Ahead Logging (WAL), MVCC, Two-Phase Locking (2PL), Dirty Read, Phantom Read",
                    Frequency = "Very High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.DBMS,
                    Question = "Clustered vs Non-Clustered Index — How do B+ Trees work?",
                    ShortAnswer = "Clustered index defines physical row order (1 per table). Non-clustered index is a secondary lookup pointer.",
                    DetailedExplanation = "• Clustered Index: Table data rows are stored in the leaf nodes of the B+ Tree itself, sorted by the clustered key (typically Primary Key). Only ONE per table.\n• Non-Clustered Index: Separate B+ Tree structure where leaf nodes contain index keys and row locators/pointers to physical rows.\n• Why B+ Tree: Keeps data sorted with O(log n) searches, high fan-out (low disk I/O depth), and leaf node linked list enables lightning-fast range queries.",
                    HighYieldKeywords = "B+ Tree, Range Scans, Primary Key, Covering Index, Index Lookup Overhead",
                    Frequency = "Very High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.DBMS,
                    Question = "Database Normalization (1NF, 2NF, 3NF, BCNF) in simple terms.",
                    ShortAnswer = "Eliminates redundancy and update anomalies by decomposing tables based on functional dependencies.",
                    DetailedExplanation = "• 1NF: Atomic values only (no lists or repeating columns), unique row identifier.\n• 2NF: In 1NF + No partial dependencies (all non-key attributes fully dependent on entire composite primary key).\n• 3NF: In 2NF + No transitive dependencies (non-key attribute depends on another non-key attribute).\n• BCNF: Stricter 3NF where for every functional dependency X -> Y, X must be a super key.",
                    HighYieldKeywords = "Functional Dependency, Insertion/Deletion Anomaly, Candidate Key, Denormalization",
                    Frequency = "High"
                },

                // ==================== COMPUTER NETWORKS ====================
                new CoreCsTopic
                {
                    Subject = CsSubject.ComputerNetworks,
                    Question = "What happens when you type https://google.com in your browser?",
                    ShortAnswer = "DNS resolution -> TCP 3-way handshake -> TLS Handshake -> HTTP GET -> Server Response -> DOM Rendering.",
                    DetailedExplanation = "1. URL Parsing & HSTS check.\n2. DNS Lookup: Browser cache -> OS cache -> Router -> ISP DNS -> Root / TLD / Authoritative nameservers.\n3. TCP Connection: 3-way handshake (SYN, SYN-ACK, ACK) with destination IP:443.\n4. TLS Handshake: Certificate validation, asymmetric key exchange (RSA/ECDH), symmetric session key established.\n5. HTTP Request: Browser sends 'GET / HTTP/2'.\n6. Server processes request & returns HTTP response (status code 200, HTML).\n7. Browser renders DOM tree, fetches assets (CSS, JS, images), executes scripts.",
                    HighYieldKeywords = "DNS, TCP SYN/ACK, TLS 1.3, Cipher Suite, HTTP/2 Multiplexing, DOM Tree",
                    Frequency = "Very High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.ComputerNetworks,
                    Question = "TCP vs UDP — Why is TCP reliable and how does the 3-Way Handshake work?",
                    ShortAnswer = "TCP is connection-oriented, reliable, and ordered. UDP is connectionless, fast, and lightweight.",
                    DetailedExplanation = "• TCP: Guarantees delivery via sequence numbers, checksums, acknowledgments (ACK), retransmissions, flow control (Sliding Window), and congestion control.\n• UDP: Fire-and-forget datagrams with minimal overhead. Ideal for live streaming, DNS queries, VoIP, and gaming.\n• TCP 3-Way Handshake:\n  1. Client sends SYN (seq = x)\n  2. Server responds SYN-ACK (seq = y, ack = x + 1)\n  3. Client responds ACK (ack = y + 1)",
                    HighYieldKeywords = "SYN, ACK, FIN, Sliding Window, Congestion Control (Tahoe/Reno), Packet Loss",
                    Frequency = "Very High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.ComputerNetworks,
                    Question = "HTTP 1.1 vs HTTP/2 vs HTTP/3 (QUIC) — Key Evolution?",
                    ShortAnswer = "HTTP/1.1 introduced keep-alive. HTTP/2 introduced binary framing & multiplexing. HTTP/3 uses UDP-based QUIC.",
                    DetailedExplanation = "• HTTP/1.1: Text-based. Suffers from Head-of-Line (HoL) blocking at application level (one request per TCP connection at a time).\n• HTTP/2: Binary protocol. Single TCP connection with multiplexed streams. Server push and HPACK header compression. Still suffers from HoL blocking if a TCP packet drops.\n• HTTP/3: Built over QUIC (UDP). Solves TCP HoL blocking completely, 0-RTT connection resumption, robust connection migration across IP/Wi-Fi switches.",
                    HighYieldKeywords = "Multiplexing, Head-of-Line Blocking, QUIC, Binary Framing, HPACK, 0-RTT",
                    Frequency = "High"
                },

                // ==================== OOP & DESIGN PATTERNS ====================
                new CoreCsTopic
                {
                    Subject = CsSubject.OOPAndDesignPatterns,
                    Question = "Explain the 5 SOLID Principles with concise examples.",
                    ShortAnswer = "Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion.",
                    DetailedExplanation = "• S (Single Responsibility): A class should have only one reason to change.\n• O (Open/Closed): Open for extension, closed for modification (use polymorphism/interfaces).\n• L (Liskov Substitution): Subtypes must be substitutable for their base types without breaking correctness (e.g. Square shouldn't inherit Rectangle if altering width changes height).\n• I (Interface Segregation): Many small, client-specific interfaces are better than one bloated general-purpose interface.\n• D (Dependency Inversion): Depend upon abstractions, not concretions (High-level business logic should not depend directly on low-level database classes; use Dependency Injection).",
                    HighYieldKeywords = "SOLID, Polymorphism, Decoupling, Dependency Injection, Inversion of Control (IoC)",
                    Frequency = "Very High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.OOPAndDesignPatterns,
                    Question = "Top Design Patterns: Singleton, Factory, and Observer.",
                    ShortAnswer = "Singleton ensures 1 instance. Factory abstracts object creation. Observer enables 1-to-many publish-subscribe.",
                    DetailedExplanation = "• Singleton: Ensures a class has only one instance and provides global access. Must use double-checked locking with thread safety.\n• Factory Method: Defines an interface for creating objects, letting subclasses decide which class to instantiate.\n• Observer: Defines a 1-to-many dependency so when one object changes state, all registered dependents are notified automatically (event-driven architecture, UI listeners, notification systems).",
                    HighYieldKeywords = "Creational vs Behavioral, Thread-Safe Singleton, Loose Coupling, Event Driven",
                    Frequency = "Very High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.OperatingSystems,
                    Question = "What is Thrashing in OS and how does the Working Set Model resolve it?",
                    ShortAnswer = "Thrashing occurs when the CPU spends more time swapping pages than executing instructions.",
                    DetailedExplanation = "• Cause: When total virtual memory working set of active processes exceeds physical RAM capacity, page faults skyrocket.\n• The OS scheduler misinterprets CPU underutilization as a need for higher multiprogramming, admitting more processes and worsening the storm.\n• Solution: Working Set Model (Peter Denning) — OS monitors the pages referenced by a process in the last Δ time units. If physical RAM cannot accommodate a process's entire working set, that process is temporarily suspended from the ready queue.",
                    HighYieldKeywords = "Thrashing, Page Fault Frequency (PFF), Working Set Model, Degree of Multiprogramming",
                    Frequency = "High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.OperatingSystems,
                    Question = "Explain CPU Scheduling: Preemptive vs Non-Preemptive, Round Robin & Starvation.",
                    ShortAnswer = "Preemptive allows interrupting running processes (Round Robin, SRTF); Non-preemptive runs until completion/I/O (FCFS, SJF).",
                    DetailedExplanation = "• FCFS: Simple FIFO queue, but suffers from Convoy Effect (short tasks trapped behind long I/O).\n• SJF / SRTF: Mathematically optimal average wait time, but causes Starvation of long bursts without Aging.\n• Round Robin: Time quantum allocated cyclically. Small quantum = high context switch overhead; Large quantum = degrades to FCFS.\n• Multilevel Feedback Queue (MLFQ): Processes move between priority queues based on CPU-burst history, prioritizing interactive tasks automatically.",
                    HighYieldKeywords = "Time Quantum, Context Switch Overhead, Starvation, Aging, Convoy Effect, MLFQ",
                    Frequency = "High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.DBMS,
                    Question = "What are the 4 Transaction Isolation Levels and their Concurrency Read Phenomena?",
                    ShortAnswer = "Read Uncommitted, Read Committed, Repeatable Read, and Serializable (preventing Dirty, Non-repeatable, and Phantom reads).",
                    DetailedExplanation = "1. Read Uncommitted: Allows Dirty Reads (reading uncommitted changes of another transaction).\n2. Read Committed: Prevents Dirty Reads, but allows Non-Repeatable Reads (row re-read changes value).\n3. Repeatable Read (MySQL default): Prevents Dirty & Non-Repeatable reads using MVCC snapshots, but historically allowed Phantom Reads (new rows inserted matching range).\n4. Serializable: Strict 2PL or serializable snapshot isolation. Zero anomalies, highest lock contention.",
                    HighYieldKeywords = "Dirty Read, Non-Repeatable Read, Phantom Read, MVCC, Snapshot Isolation",
                    Frequency = "Very High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.DBMS,
                    Question = "Explain the CAP Theorem and PACELC Extension in Distributed Databases.",
                    ShortAnswer = "In the presence of network Partition (P), choose Consistency (C) or Availability (A). PACELC adds Else Latency (L) vs Consistency (C).",
                    DetailedExplanation = "• CAP Theorem: A distributed data store can guarantee at most two: Consistency (every read gets the latest write), Availability (every non-failing node returns response), Partition Tolerance (tolerates dropped/delayed network messages). Since network splits are unavoidable, systems choose CP (MongoDB, HBase) or AP (Cassandra, DynamoDB).\n• PACELC: If Partition (P), choose Availability (A) or Consistency (C); Else (E), choose Latency (L) or Consistency (C).",
                    HighYieldKeywords = "CAP Theorem, Split-Brain, Eventual Consistency, Quorum, PACELC",
                    Frequency = "High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.ComputerNetworks,
                    Question = "Explain the OSI 7-Layer Model vs TCP/IP Architecture with Protocols.",
                    ShortAnswer = "OSI: Physical, Data Link, Network, Transport, Session, Presentation, Application (Please Do Not Throw Sausage Pizza Away).",
                    DetailedExplanation = "• Layer 7 Application: HTTP, DNS, SMTP, FTP, SSH.\n• Layer 6 Presentation: TLS/SSL encryption, JSON/Protobuf formatting, compression.\n• Layer 5 Session: RPC session management, NetBIOS.\n• Layer 4 Transport: End-to-end transport, port addressing, flow/congestion control (TCP, UDP).\n• Layer 3 Network: Logical IP addressing, routing across subnets (IP, ICMP, BGP, OSPF).\n• Layer 2 Data Link: Physical MAC addressing, Ethernet frames, switches, ARP.\n• Layer 1 Physical: Bits over physical medium (cables, radio waves, optical fiber).",
                    HighYieldKeywords = "Encapsulation, De-encapsulation, PDU, MAC Address, IP Address, Port Number",
                    Frequency = "Very High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.ComputerNetworks,
                    Question = "WebSockets vs Server-Sent Events (SSE) vs Long Polling — When to use which?",
                    ShortAnswer = "WebSockets for full-duplex bi-directional communication. SSE for server-to-client unidirectional streams. Long polling as legacy fallback.",
                    DetailedExplanation = "• WebSockets: Full-duplex bidirectional TCP stream initiated via HTTP Upgrade header. Ideal for multiplayer gaming, live collaborative whiteboards, high-frequency chat.\n• SSE (Server-Sent Events): Lightweight unidirectional stream from server to client over standard HTTP. Built-in reconnection and event IDs. Ideal for LLM token streaming, live stock feeds, notification tickers.\n• Long Polling: Client sends request; server holds open until data is ready. High HTTP header overhead.",
                    HighYieldKeywords = "Full Duplex, Bi-directional, HTTP Upgrade, EventSource, LLM Streaming",
                    Frequency = "High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.SystemDesignBasics,
                    Question = "How do Layer 4 vs Layer 7 Load Balancers differ? Explain Consistent Hashing.",
                    ShortAnswer = "L4 routes at Transport layer (IP/Port). L7 inspects Application data (HTTP headers, cookies, URL paths).",
                    DetailedExplanation = "• L4 Load Balancer: Fast, ultra-high throughput packet forwarding (TCP SYN/ACK). Does not decrypt TLS or read HTTP headers.\n• L7 Load Balancer: Smart routing based on URLs (e.g. /api -> backend, /static -> CDN), cookies for sticky sessions, TLS termination, and rate limiting (Nginx, Envoy, AWS ALB).\n• Consistent Hashing: Maps servers and cache keys to a hash ring (0 to 2^32-1). When a server node joins or leaves, only k/n keys are remapped instead of reshuffling all keys, preserving cache hits.",
                    HighYieldKeywords = "L4 vs L7, Nginx, Envoy, Virtual Nodes, Hash Ring, Cache Eviction",
                    Frequency = "Very High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.SystemDesignBasics,
                    Question = "Caching Strategies: Cache-Aside vs Write-Through vs Write-Back & Eviction Policies.",
                    ShortAnswer = "Cache-Aside reads cache first and falls back to DB. Write-Through writes DB and cache synchronously. Write-Back writes cache and flushes async.",
                    DetailedExplanation = "• Cache-Aside (Lazy Loading): Application reads cache. If miss, reads DB, writes to cache, and returns. Cache only holds requested data.\n• Write-Through: App writes to cache; cache writes synchronously to DB. Ensures high consistency, but adds write latency.\n• Write-Back (Write-Behind): App writes only to cache; cache batches async writes to DB. Ultra-low write latency, but risks data loss on cache node crash.\n• Eviction Policies: LRU (Least Recently Used via Doubly Linked List + HashMap), LFU (Least Frequently Used), FIFO, TTL (Time-To-Live).",
                    HighYieldKeywords = "Cache Miss, Cache Stampede, Thundering Herd, LRU Cache, Write Latency, Redis",
                    Frequency = "Very High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.SystemDesignBasics,
                    Question = "Message Queues: Apache Kafka vs RabbitMQ — Core Architecture Differences?",
                    ShortAnswer = "Kafka is a distributed append-only commit log (pull-based, partitioned). RabbitMQ is a smart broker / dumb consumer queue (push-based, AMQP).",
                    DetailedExplanation = "• Apache Kafka: Distributed, partitioned, append-only disk commit log. Consumers pull messages and track offsets. Messages persist after reading. High-throughput (millions msg/sec), event sourcing, stream processing.\n• RabbitMQ: Traditional broker with flexible exchange routing (direct, topic, fanout, headers). Pushes messages to consumers. Messages are acknowledged and deleted from queue. Ideal for complex microservice task routing.",
                    HighYieldKeywords = "Commit Log, Consumer Offsets, Partitions, AMQP, Acknowledgments, Event Sourcing",
                    Frequency = "Very High"
                },
                new CoreCsTopic
                {
                    Subject = CsSubject.SystemDesignBasics,
                    Question = "API Rate Limiting: Token Bucket vs Leaky Bucket vs Sliding Window Log.",
                    ShortAnswer = "Token bucket allows controlled traffic bursts; Leaky bucket enforces smooth constant outflow rate; Sliding window prevents edge-boundary spikes.",
                    DetailedExplanation = "• Token Bucket: Tokens refilled into bucket at constant rate r up to capacity b. Each request consumes 1 token. Allows bursts up to b, then throttles. Standard choice (AWS, Redis, Stripe).\n• Leaky Bucket: Requests enter FIFO buffer and leak out at fixed rate. Smooths out traffic, but drops requests if queue fills.\n• Fixed Window Counter: Resets count per minute (e.g. 100 req/min). Flaw: 100 requests at 0:59 + 100 at 1:01 = 200 requests in 2 seconds.\n• Sliding Window Log: Stores timestamps of requests in Redis Sorted Set (ZSET). Accurately trims expired timestamps.",
                    HighYieldKeywords = "Token Bucket, Leaky Bucket, Redis ZSET, 429 Too Many Requests, Burst Capacity",
                    Frequency = "High"
                }
            };
        }

        public static List<BehavioralQuestion> GetCuratedBehavioralQuestions()
        {
            return new List<BehavioralQuestion>
            {
                new BehavioralQuestion
                {
                    QuestionTitle = "Tell Me About Yourself (Elevator Pitch)",
                    Category = "Introduction & Impact",
                    Situation = "Final year Computer Science student passionate about distributed systems and full-stack software development.",
                    Task = "Provide a 90-second crisp narrative connecting academic coursework, major projects, internship experience, and career trajectory.",
                    Action = "Focus on 3 points: (1) Background & core programming languages (C++, Java, Python); (2) High-impact technical project built (e.g., real-time collaboration tool or performance-optimized app); (3) Why this specific company's engineering challenges excite me.",
                    Result = "Leaves the interviewer with a structured, memorable impression as a problem-solver who can ship reliable production code.",
                    KeyTakeaway = "Follow the Present -> Past -> Future formula. Never simply recite your resume."
                },
                new BehavioralQuestion
                {
                    QuestionTitle = "Describe a Challenging Technical Bug and How You Resolved It",
                    Category = "Problem Solving & Debugging",
                    Situation = "During a team web application project, the server experienced intermittent memory exhaustion and latency spikes under concurrent simulated users.",
                    Task = "Identify root cause without crashing the live staging deployment before evaluation deadline.",
                    Action = "Set up APM profiling and inspected memory heaps. Discovered unclosed database connection pools and an unindexed foreign-key query triggering full table scans. Implemented connection pooling with async/await and added composite B-Tree indexes.",
                    Result = "Query response time dropped from 1.4s to 45ms (96% reduction), and RAM usage stabilized under 5x simulated load.",
                    KeyTakeaway = "Show systematic debugging methodology: hypothesis, instrumentation, profiling, solution, and automated regression test."
                },
                new BehavioralQuestion
                {
                    QuestionTitle = "A Time You Disagreed with a Teammate or Faced Conflict",
                    Category = "Teamwork & Communication",
                    Situation = "In our capstone project, a teammate wanted to use a trending NoSQL database while I advocated for PostgreSQL.",
                    Task = "Reach alignment without delaying the sprint schedule or creating interpersonal friction.",
                    Action = "Avoided emotional argument. Created a decision matrix evaluating schema rigidity, ACID requirements for our payment module, and team familiarity. Set up a 1-hour prototype benchmark demonstrating PostgreSQL handled our transactional requirements seamlessly while supporting JSONB for unstructured fields.",
                    Result = "Teammate agreed with data-backed findings. We delivered the sprint 2 days ahead of schedule.",
                    KeyTakeaway = "Disagree and commit; rely on benchmarks and objective criteria rather than personal opinions."
                },
                new BehavioralQuestion
                {
                    QuestionTitle = "Tell Me About a Time You Failed or Made a Mistake",
                    Category = "Resilience & Ownership",
                    Situation = "Underestimated the complexity of real-time WebSocket state synchronization for a hackathon project.",
                    Task = "Deliver the MVP within a 36-hour hackathon window when the initial sync architecture failed under multi-client race conditions.",
                    Action = "Acknowledged the flaw immediately to teammates. Stripped unnecessary complex custom conflict resolution and pivoted to a well-tested Operational Transformation library. Worked through the night to rewrite the synchronization layer.",
                    Result = "Successfully demoed the working MVP to judges and secured 2nd place out of 40 teams.",
                    KeyTakeaway = "Demonstrate complete ownership, lack of defensive ego, quick pivot ability, and the lasting engineering lesson learned."
                }
            };
        }

        public static List<CompanyApplication> GetCuratedSampleApplications()
        {
            return new List<CompanyApplication>
            {
                new CompanyApplication
                {
                    CompanyName = "Google",
                    Role = "Software Engineer (University Graduate)",
                    PackageCtc = "38 LPA",
                    Stage = DriveStage.Applied,
                    UpcomingEventDate = DateTime.Today.AddDays(14),
                    UpcomingEventTitle = "Google Online Challenge (GOC)",
                    Location = "Bangalore / Hyderabad",
                    FocusTopics = "Advanced DP, Graphs, Binary Search on Answer Space, Segment Trees",
                    RoundNotes = "Focus on clean modular code, handling constraints, and time/space complexity justification."
                },
                new CompanyApplication
                {
                    CompanyName = "Amazon",
                    Role = "Software Development Engineer 1 (SDE-1)",
                    PackageCtc = "28.5 LPA",
                    Stage = DriveStage.OnlineAssessment,
                    UpcomingEventDate = DateTime.Today.AddDays(4),
                    UpcomingEventTitle = "Amazon OA (2 Coding Questions + Work Style Simulation)",
                    Location = "Bangalore / Chennai",
                    FocusTopics = "Trees, BFS/DFS, Priority Queue, Sliding Window, Leadership Principles",
                    RoundNotes = "Amazon tests Leadership Principles heavily in both OA and Tech rounds. Review Customer Obsession & Ownership."
                },
                new CompanyApplication
                {
                    CompanyName = "Microsoft",
                    Role = "Software Engineer (SDE-1)",
                    PackageCtc = "26 LPA",
                    Stage = DriveStage.TechnicalRound,
                    UpcomingEventDate = DateTime.Today.AddDays(7),
                    UpcomingEventTitle = "Technical Round 1: DSA & Core CS (OS/Threads)",
                    Location = "Hyderabad",
                    FocusTopics = "Linked Lists, Tree Traversals, OS Deadlocks & Concurrency, OOP Design",
                    RoundNotes = "Interviewer emphasizes clean code, OOP principles, and discussing edge cases before coding."
                },
                new CompanyApplication
                {
                    CompanyName = "Cisco",
                    Role = "Software Engineer - Networking & Systems",
                    PackageCtc = "19.5 LPA",
                    Stage = DriveStage.Applied,
                    UpcomingEventDate = DateTime.Today.AddDays(9),
                    UpcomingEventTitle = "Cisco Campus Aptitude & Coding OA",
                    Location = "Bangalore",
                    FocusTopics = "Computer Networks (TCP/IP, OSI), Operating Systems, C/C++ Pointers, Arrays/Strings",
                    RoundNotes = "Cisco asks direct questions on subnetting, routing, socket programming, and memory leaks."
                }
            };
        }

        public static List<TaskItem> GetCuratedDailyTasks()
        {
            return new List<TaskItem>
            {
                new TaskItem
                {
                    Title = "Solve 2 DP/Graph problems on LeetCode (Focus: Topological Sort & Knapsack)",
                    EnergyTag = EnergyTag.Deep,
                    DueDate = DateTime.Today,
                    IsDailyHighlight = true
                },
                new TaskItem
                {
                    Title = "Revise Core CS Flashcards: OS Deadlocks & Paging vs Segmentation",
                    EnergyTag = EnergyTag.Quick,
                    DueDate = DateTime.Today
                },
                new TaskItem
                {
                    Title = "Practice STAR Method response for 'Challenging Bug' out loud (3 mins)",
                    EnergyTag = EnergyTag.Quick,
                    DueDate = DateTime.Today
                },
                new TaskItem
                {
                    Title = "Review Amazon OA syllabus & practice 1 timed Mock session (45 mins)",
                    EnergyTag = EnergyTag.Deep,
                    DueDate = DateTime.Today.AddDays(1)
                }
            };
        }
    }
}
