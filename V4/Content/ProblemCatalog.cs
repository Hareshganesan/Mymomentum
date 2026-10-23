using Momentum.Models;

namespace Momentum.Content
{
    public sealed record PatternDef(string Key, string Name, string Cue, string Recipe);

    public sealed class ProblemDef
    {
        public string Slug { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public Difficulty Difficulty { get; init; }
        public string Pattern { get; init; } = string.Empty;
        public string[] Companies { get; init; } = Array.Empty<string>();
        public string Hint { get; init; } = string.Empty;
        public string? CustomUrl { get; init; }
        public bool IsCustom { get; init; }
        public int Order { get; init; }

        public string Url => !string.IsNullOrWhiteSpace(CustomUrl) ? CustomUrl! : $"https://leetcode.com/problems/{Slug}/";

        public int EstimatedMinutes => Difficulty switch { Difficulty.Easy => 20, Difficulty.Medium => 35, _ => 50 };
    }

    /// <summary>
    /// The core problem sheet: ~160 high-frequency interview problems ordered as a learning roadmap.
    /// Patterns come first so the app can teach recognition ("when do I reach for this?"), not just volume.
    /// Company tags are commonly reported associations, not official data.
    /// </summary>
    public static class ProblemCatalog
    {
        private const Difficulty E = Difficulty.Easy;
        private const Difficulty M = Difficulty.Medium;
        private const Difficulty H = Difficulty.Hard;

        public static IReadOnlyList<PatternDef> Patterns { get; } = new List<PatternDef>
        {
            new("arrays", "Arrays & Hashing",
                "Need fast lookups, counting, grouping or de-duplication over a list.",
                "Trade space for time: a HashMap/HashSet turns an O(n²) search into O(n). Count with a frequency map, group by a canonical key."),
            new("two-pointers", "Two Pointers",
                "Sorted array or string, pairs/triplets that meet a target, or in-place partitioning.",
                "Start pointers at both ends (or slow/fast) and move the one that brings you closer to the goal. Sort first if order doesn't matter."),
            new("sliding-window", "Sliding Window",
                "Contiguous subarray/substring with a constraint: longest, shortest, at most K, contains all of…",
                "Expand right every step; while the window is invalid, shrink from the left. Track state in a counter/map. Answer updates when valid."),
            new("stack", "Stack & Monotonic Stack",
                "Matching brackets, 'next greater/smaller element', spans, histograms, or undo-style processing.",
                "Push indices; pop while the current element breaks the monotonic order — each pop resolves an answer for the popped index."),
            new("binary-search", "Binary Search",
                "Sorted/rotated input, or 'minimum X such that condition holds' (search on the answer space).",
                "Define a monotonic predicate, keep lo/hi invariant, mid = lo + (hi-lo)/2. For answer-space problems, binary search the value and test feasibility."),
            new("linked-list", "Linked List",
                "Pointer manipulation: reverse, merge, detect cycles, find middle, reorder in place.",
                "Use a dummy head to avoid edge cases; slow/fast pointers for middle and cycles; reverse with prev/curr/next."),
            new("trees", "Trees & BST",
                "Hierarchical data, path sums, depth, ancestors, level-by-level views, BST ordering.",
                "Decide what each recursive call returns to its parent (height, sum, validity). Level order → BFS with a queue. BST → in-order is sorted."),
            new("tries", "Tries",
                "Many prefix queries, autocomplete, word dictionaries, or searching many words on a board.",
                "Each node holds children[26] + isEnd. Insert/search walk character by character; combine with DFS for board searches."),
            new("heap", "Heap / Priority Queue",
                "Top K, K-th largest, merging K sorted things, scheduling, or a running median.",
                "Min-heap of size K keeps the K largest. Two heaps (max-left, min-right) keep a median. Push (priority, item) pairs."),
            new("backtracking", "Backtracking",
                "Generate all subsets, permutations, combinations, partitions or board placements.",
                "choose → explore → un-choose. Pass the current path and a start index; prune early; sort first to skip duplicates."),
            new("graphs", "Graphs",
                "Grids, islands, connectivity, dependencies, shortest path in an unweighted graph.",
                "Build an adjacency list, keep a visited set. BFS for shortest steps, DFS for flood fill, Kahn's algorithm for ordering, Union-Find for components."),
            new("adv-graphs", "Advanced Graphs",
                "Weighted shortest paths, minimum spanning tree, constrained paths.",
                "Dijkstra = BFS with a min-heap on distance. Prim/Kruskal for MST. Bellman-Ford when you're limited to K edges."),
            new("dp-1d", "DP · 1-D",
                "'Number of ways', 'min/max cost', choices at each index that depend on earlier answers.",
                "Define dp[i] in words first, write the recurrence from the last choice, set base cases, then iterate. Often reducible to two variables."),
            new("dp-2d", "DP · 2-D",
                "Two strings/sequences, grids, or knapsack-style 'pick items under a capacity'.",
                "dp[i][j] = answer for prefixes i and j (or row/col). Fill row by row; 0/1 knapsack iterates capacity backwards in 1-D."),
            new("greedy", "Greedy",
                "A locally best choice is provably safe: max subarray, reachability, scheduling.",
                "Find the invariant ('farthest reachable', 'current sum') and argue why taking the best now never hurts later."),
            new("intervals", "Intervals",
                "Ranges that overlap, merge, insert, or need a minimum number of removals/rooms.",
                "Sort by start (or end for removal problems). Compare each interval with the last merged one."),
            new("math", "Math & Matrix",
                "Matrix rotation/spiral traversal, digit manipulation, fast power, simulation.",
                "Work layer by layer for matrices, handle overflow explicitly, and use exponentiation by squaring for powers."),
            new("bits", "Bit Manipulation",
                "XOR tricks, counting set bits, missing/single numbers, adding without '+'.",
                "x ^ x = 0, x & (x-1) clears the lowest set bit, x & -x isolates it. Shift and mask to read bits."),
        };

        public static IReadOnlyList<ProblemDef> Problems { get; }

        static ProblemCatalog()
        {
            var list = new List<ProblemDef>();
            string current = "arrays";
            void P(string slug, string title, Difficulty d, string companies, string hint) => list.Add(new ProblemDef
            {
                Slug = slug,
                Title = title,
                Difficulty = d,
                Pattern = current,
                Companies = companies.Length == 0 ? Array.Empty<string>() : companies.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
                Hint = hint,
                Order = list.Count
            });

            current = "arrays";
            P("contains-duplicate", "Contains Duplicate", E, "Amazon, Apple", "Insert into a HashSet; if it's already there you found a duplicate.");
            P("valid-anagram", "Valid Anagram", E, "Amazon, Microsoft, Bloomberg", "Count letters of s up and t down in a 26-length array; all zeros means anagram.");
            P("two-sum", "Two Sum", E, "Amazon, Google, Microsoft, Adobe", "For each x, check whether target - x is already in a value→index map, then store x.");
            P("group-anagrams", "Group Anagrams", M, "Amazon, Microsoft, Uber", "Use the sorted string (or a 26-count tuple) as the map key.");
            P("top-k-frequent-elements", "Top K Frequent Elements", M, "Amazon, Meta, Oracle", "Count frequencies, then bucket sort by count (index = frequency) and read buckets from the top.");
            P("product-of-array-except-self", "Product of Array Except Self", M, "Amazon, Microsoft, Adobe", "Prefix products left→right into the output, then multiply by a running suffix product right→left.");
            P("valid-sudoku", "Valid Sudoku", M, "Amazon, Uber", "Track seen digits per row, column and box (box = (r/3)*3 + c/3) in sets.");
            P("longest-consecutive-sequence", "Longest Consecutive Sequence", M, "Google, Amazon", "Put all numbers in a set; only start counting from x when x-1 is not in the set.");
            P("subarray-sum-equals-k", "Subarray Sum Equals K", M, "Meta, Amazon, Microsoft", "Running prefix sum; add count[prefix - k] to the answer. Seed count[0] = 1.");
            P("majority-element", "Majority Element", E, "Amazon, Adobe", "Boyer–Moore voting: keep a candidate and a counter, increment on match, decrement otherwise.");

            current = "two-pointers";
            P("valid-palindrome", "Valid Palindrome", E, "Meta, Microsoft", "Two pointers from both ends, skip non-alphanumerics, compare lowercase.");
            P("move-zeroes", "Move Zeroes", E, "Meta, Amazon", "Write pointer for the next non-zero slot; swap each non-zero into it.");
            P("two-sum-ii-input-array-is-sorted", "Two Sum II - Input Array Is Sorted", M, "Amazon", "Left/right pointers; move left up if the sum is too small, right down if too big.");
            P("3sum", "3Sum", M, "Amazon, Meta, Microsoft, Adobe", "Sort, fix i, run two-sum-II on the rest; skip equal neighbours to avoid duplicate triplets.");
            P("container-with-most-water", "Container With Most Water", M, "Amazon, Google", "Start wide; always move the shorter wall inward because it limits the area.");
            P("sort-colors", "Sort Colors", M, "Microsoft, Amazon, Adobe", "Dutch national flag: low/mid/high pointers; swap 0s to low, 2s to high.");
            P("trapping-rain-water", "Trapping Rain Water", H, "Amazon, Google, Goldman Sachs", "Two pointers with leftMax/rightMax; the smaller side decides how much water sits at that index.");

            current = "sliding-window";
            P("best-time-to-buy-and-sell-stock", "Best Time to Buy and Sell Stock", E, "Amazon, Microsoft, Goldman Sachs", "Track the minimum price so far; best profit = max(price - minSoFar).");
            P("longest-substring-without-repeating-characters", "Longest Substring Without Repeating Characters", M, "Amazon, Microsoft, Adobe, Bloomberg", "Window with last-seen index map; jump left past the previous occurrence of the repeating char.");
            P("longest-repeating-character-replacement", "Longest Repeating Character Replacement", M, "Google, Uber", "Window is valid while (length - maxFreq) ≤ k; maxFreq never needs to decrease.");
            P("permutation-in-string", "Permutation in String", M, "Microsoft", "Fixed-size window of len(s1); compare 26-count arrays (or track a 'matches' counter).");
            P("fruit-into-baskets", "Fruit Into Baskets", M, "Google, Amazon", "Longest window with at most 2 distinct values — shrink while the map has 3 keys.");
            P("minimum-window-substring", "Minimum Window Substring", H, "Meta, Amazon, Google, Uber", "Expand until all required counts are met (have == need), then shrink while still valid, recording the best.");
            P("sliding-window-maximum", "Sliding Window Maximum", H, "Amazon, Google", "Monotonic decreasing deque of indices; drop indices that fall out of the window from the front.");

            current = "stack";
            P("valid-parentheses", "Valid Parentheses", E, "Amazon, Meta, Microsoft, Bloomberg", "Push opens; on a close, the stack top must be its matching open.");
            P("min-stack", "Min Stack", M, "Amazon, Bloomberg", "Store (value, minSoFar) pairs so getMin is the top's second field.");
            P("evaluate-reverse-polish-notation", "Evaluate Reverse Polish Notation", M, "Amazon, LinkedIn", "Push numbers; on an operator pop b then a and push a op b (careful with order for - and /).");
            P("next-greater-element-i", "Next Greater Element I", E, "Amazon", "Monotonic decreasing stack over nums2 builds a next-greater map; look up nums1.");
            P("daily-temperatures", "Daily Temperatures", M, "Amazon, Meta", "Stack of indices with decreasing temps; when a warmer day arrives, pop and fill the wait.");
            P("car-fleet", "Car Fleet", M, "Google", "Sort by position descending, compute arrival time; a car joins a fleet if it arrives no later than the one ahead.");
            P("largest-rectangle-in-histogram", "Largest Rectangle in Histogram", H, "Amazon, Google, Microsoft", "Increasing stack; when popping a bar, its width spans from the new stack top+1 to i-1.");

            current = "binary-search";
            P("binary-search", "Binary Search", E, "Microsoft", "lo ≤ hi loop, mid = lo + (hi - lo) / 2; move lo or hi past mid.");
            P("search-a-2d-matrix", "Search a 2D Matrix", M, "Amazon, Microsoft", "Treat the matrix as one sorted array of m*n: row = mid / n, col = mid % n.");
            P("find-first-and-last-position-of-element-in-sorted-array", "Find First and Last Position of Element", M, "Meta, Amazon", "Run lower-bound twice: first index ≥ target, and first index ≥ target+1, minus one.");
            P("koko-eating-bananas", "Koko Eating Bananas", M, "Meta, Amazon", "Binary search the speed k in [1, max]; feasible if Σ ceil(pile/k) ≤ h.");
            P("capacity-to-ship-packages-within-d-days", "Capacity To Ship Packages Within D Days", M, "Amazon, Flipkart", "Search capacity in [max weight, sum]; greedily count days needed for a given capacity.");
            P("find-minimum-in-rotated-sorted-array", "Find Minimum in Rotated Sorted Array", M, "Amazon, Microsoft", "Compare mid with hi: if nums[mid] > nums[hi] the minimum is right of mid, else at or left of mid.");
            P("search-in-rotated-sorted-array", "Search in Rotated Sorted Array", M, "Amazon, Microsoft, Meta, Adobe", "One half is always sorted; check whether the target lies inside that sorted half.");
            P("time-based-key-value-store", "Time Based Key-Value Store", M, "Google, Uber", "Per key keep a list of (timestamp, value) in insertion order; binary search the largest ≤ t.");
            P("median-of-two-sorted-arrays", "Median of Two Sorted Arrays", H, "Google, Amazon, Goldman Sachs", "Binary search a partition of the smaller array so left halves ≤ right halves.");

            current = "linked-list";
            P("reverse-linked-list", "Reverse Linked List", E, "Amazon, Microsoft, Apple", "prev = null; for each node: save next, point node to prev, advance prev and node.");
            P("merge-two-sorted-lists", "Merge Two Sorted Lists", E, "Amazon, Microsoft, Adobe", "Dummy head; attach the smaller node each step, then the remaining tail.");
            P("middle-of-the-linked-list", "Middle of the Linked List", E, "Amazon", "Slow moves 1, fast moves 2; when fast ends, slow is the middle.");
            P("linked-list-cycle", "Linked List Cycle", E, "Amazon, Microsoft", "Floyd's tortoise and hare — if they meet, there's a cycle.");
            P("reorder-list", "Reorder List", M, "Amazon, Meta", "Find middle, reverse the second half, then weave the two halves together.");
            P("remove-nth-node-from-end-of-list", "Remove Nth Node From End of List", M, "Meta, Amazon", "Advance a lead pointer n steps from a dummy, then move both until lead hits the end.");
            P("copy-list-with-random-pointer", "Copy List with Random Pointer", M, "Amazon, Meta, Microsoft", "Map old→new in one pass, wire next/random in a second (or interleave copies in place).");
            P("add-two-numbers", "Add Two Numbers", M, "Amazon, Microsoft, Adobe", "Walk both lists adding digits with carry into a dummy-headed result.");
            P("find-the-duplicate-number", "Find the Duplicate Number", M, "Amazon, Microsoft", "Treat values as next pointers; Floyd's cycle detection finds the entry = duplicate.");
            P("lru-cache", "LRU Cache", M, "Amazon, Microsoft, Meta, Google, Flipkart", "HashMap key→node plus a doubly linked list; move to front on access, evict from the tail.");
            P("merge-k-sorted-lists", "Merge k Sorted Lists", H, "Amazon, Meta, Google", "Min-heap of list heads, or pairwise divide-and-conquer merging.");
            P("reverse-nodes-in-k-group", "Reverse Nodes in k-Group", H, "Amazon, Microsoft", "Check k nodes exist, reverse that block, connect to the recursively/iteratively processed rest.");

            current = "trees";
            P("invert-binary-tree", "Invert Binary Tree", E, "Google, Amazon", "Swap left and right children, recurse on both.");
            P("maximum-depth-of-binary-tree", "Maximum Depth of Binary Tree", E, "Amazon, LinkedIn", "depth(node) = 1 + max(depth(left), depth(right)).");
            P("diameter-of-binary-tree", "Diameter of Binary Tree", E, "Meta, Amazon", "Return height; at each node update best with leftHeight + rightHeight.");
            P("balanced-binary-tree", "Balanced Binary Tree", E, "Amazon", "Return height or -1 if unbalanced; propagate -1 upward.");
            P("same-tree", "Same Tree", E, "Amazon", "Both null → true; one null or values differ → false; recurse on children.");
            P("subtree-of-another-tree", "Subtree of Another Tree", E, "Amazon, Meta", "At every node of root, check sameTree(node, subRoot).");
            P("lowest-common-ancestor-of-a-binary-search-tree", "Lowest Common Ancestor of a BST", M, "Amazon, Microsoft", "If both values are smaller go left, both larger go right, otherwise this node is the LCA.");
            P("lowest-common-ancestor-of-a-binary-tree", "Lowest Common Ancestor of a Binary Tree", M, "Meta, Amazon, Microsoft", "Return the node if it matches p/q; if both subtrees return non-null, current node is the LCA.");
            P("binary-tree-level-order-traversal", "Binary Tree Level Order Traversal", M, "Amazon, Microsoft, Meta", "BFS; process queue.size() nodes per level.");
            P("binary-tree-zigzag-level-order-traversal", "Binary Tree Zigzag Level Order Traversal", M, "Amazon, Microsoft, Flipkart", "Level-order BFS; reverse every other level (or insert at front).");
            P("binary-tree-right-side-view", "Binary Tree Right Side View", M, "Meta, Amazon", "BFS and take the last node of each level (or DFS right-first, first node seen at each depth).");
            P("count-good-nodes-in-binary-tree", "Count Good Nodes in Binary Tree", M, "Microsoft", "DFS carrying the max value on the path; a node is good if it's ≥ that max.");
            P("validate-binary-search-tree", "Validate Binary Search Tree", M, "Amazon, Meta, Microsoft, Bloomberg", "Pass (low, high) bounds down; every node must lie strictly inside its bounds.");
            P("kth-smallest-element-in-a-bst", "Kth Smallest Element in a BST", M, "Amazon, Uber", "In-order traversal visits values in sorted order; stop at the k-th.");
            P("construct-binary-tree-from-preorder-and-inorder-traversal", "Construct Binary Tree from Preorder and Inorder", M, "Amazon, Microsoft", "Preorder's first value is the root; its inorder index splits left/right subtrees. Use a value→index map.");
            P("binary-tree-maximum-path-sum", "Binary Tree Maximum Path Sum", H, "Meta, Amazon, Google", "Return the best downward path (clamp negatives to 0); update the global best with left + node + right.");
            P("serialize-and-deserialize-binary-tree", "Serialize and Deserialize Binary Tree", H, "Amazon, Meta, Microsoft, Uber", "Preorder with a null marker; deserialize by consuming tokens recursively.");

            current = "tries";
            P("implement-trie-prefix-tree", "Implement Trie (Prefix Tree)", M, "Amazon, Google, Microsoft", "Node = children map/array + isEnd flag; walk characters to insert, search, or check a prefix.");
            P("design-add-and-search-words-data-structure", "Design Add and Search Words Data Structure", M, "Meta, Amazon", "Trie plus DFS: on '.', try every child.");
            P("word-search-ii", "Word Search II", H, "Amazon, Microsoft, Uber", "Build a trie of the words and DFS the board along trie edges; prune found words from the trie.");

            current = "heap";
            P("kth-largest-element-in-a-stream", "Kth Largest Element in a Stream", E, "Amazon", "Keep a min-heap of size k; its top is the k-th largest.");
            P("last-stone-weight", "Last Stone Weight", E, "Amazon", "Max-heap: pop the two heaviest, push back the difference if non-zero.");
            P("k-closest-points-to-origin", "K Closest Points to Origin", M, "Meta, Amazon", "Max-heap of size k on squared distance (or quickselect).");
            P("kth-largest-element-in-an-array", "Kth Largest Element in an Array", M, "Meta, Amazon, Microsoft", "Min-heap of size k, or quickselect for average O(n).");
            P("task-scheduler", "Task Scheduler", M, "Meta, Amazon", "Answer = max(len(tasks), (maxFreq - 1) * (n + 1) + countOfMaxFreq).");
            P("design-twitter", "Design Twitter", M, "Amazon, Twitter", "Per-user tweet lists with timestamps; merge the followees' recent tweets with a heap.");
            P("find-median-from-data-stream", "Find Median from Data Stream", H, "Amazon, Google, Microsoft", "Max-heap for the lower half, min-heap for the upper half; keep sizes balanced.");

            current = "backtracking";
            P("subsets", "Subsets", M, "Meta, Amazon", "At each index choose include/exclude, or loop from start index adding to the path.");
            P("combination-sum", "Combination Sum", M, "Amazon, Airbnb", "Recurse with the same index (unlimited reuse); stop when remaining < 0.");
            P("permutations", "Permutations", M, "Microsoft, Amazon", "Swap-based recursion or a used[] array; record when the path is full.");
            P("subsets-ii", "Subsets II", M, "Meta", "Sort, and skip nums[i] == nums[i-1] when i > start.");
            P("combination-sum-ii", "Combination Sum II", M, "Meta, Amazon", "Sort; each number used once (i + 1); skip duplicates at the same depth.");
            P("letter-combinations-of-a-phone-number", "Letter Combinations of a Phone Number", M, "Amazon, Microsoft, Uber", "Map digits to letters; DFS over digit positions appending one letter each.");
            P("word-search", "Word Search", M, "Amazon, Microsoft, Bloomberg", "DFS from every cell, mark visited in place, restore on the way back.");
            P("palindrome-partitioning", "Palindrome Partitioning", M, "Amazon, Google", "At each start, try every end that forms a palindrome and recurse on the rest.");
            P("n-queens", "N-Queens", H, "Amazon, Microsoft", "Place row by row; track used columns and both diagonals (r+c, r-c) in sets.");

            current = "graphs";
            P("number-of-islands", "Number of Islands", M, "Amazon, Microsoft, Google, Bloomberg", "Scan the grid; on each '1' increment count and flood-fill (DFS/BFS) to sink the island.");
            P("max-area-of-island", "Max Area of Island", M, "Amazon, Google", "Flood fill returning the size of each island; track the max.");
            P("clone-graph", "Clone Graph", M, "Meta, Amazon, Google", "DFS/BFS with an old→clone map; create on first visit, then wire neighbours.");
            P("rotting-oranges", "Rotting Oranges", M, "Amazon, Microsoft", "Multi-source BFS from all rotten oranges; count minutes by levels.");
            P("01-matrix", "01 Matrix", M, "Amazon, Google", "Multi-source BFS starting from every 0 gives the distance to the nearest 0.");
            P("pacific-atlantic-water-flow", "Pacific Atlantic Water Flow", M, "Google, Amazon", "Reverse the flow: DFS uphill from each ocean's border; answer is the intersection.");
            P("surrounded-regions", "Surrounded Regions", M, "Google, Amazon", "Mark border-connected 'O's as safe first, then flip the rest.");
            P("course-schedule", "Course Schedule", M, "Amazon, Microsoft, Meta", "Detect a cycle: Kahn's BFS with in-degrees (processed count == n) or DFS with 3 colours.");
            P("course-schedule-ii", "Course Schedule II", M, "Amazon, Meta", "Kahn's algorithm; the order nodes leave the queue is a valid topological order.");
            P("is-graph-bipartite", "Is Graph Bipartite?", M, "Meta, Amazon", "Two-colour with BFS/DFS; a neighbour with the same colour means not bipartite.");
            P("number-of-provinces", "Number of Provinces", M, "Amazon, Goldman Sachs", "Count connected components with DFS or Union-Find.");
            P("redundant-connection", "Redundant Connection", M, "Google, Amazon", "Union-Find: the first edge whose endpoints already share a root is redundant.");
            P("word-ladder", "Word Ladder", H, "Amazon, Meta, Google", "BFS over words; neighbours via wildcard patterns like h*t → [hot, hit].");

            current = "adv-graphs";
            P("network-delay-time", "Network Delay Time", M, "Amazon, Google", "Dijkstra from k; answer is the max shortest distance (or -1 if unreachable).");
            P("path-with-minimum-effort", "Path With Minimum Effort", M, "Google", "Dijkstra where a path's cost is the max edge difference so far.");
            P("min-cost-to-connect-all-points", "Min Cost to Connect All Points", M, "Amazon", "Prim's MST with a min-heap on Manhattan distance (or Kruskal + Union-Find).");
            P("cheapest-flights-within-k-stops", "Cheapest Flights Within K Stops", M, "Amazon, Airbnb", "Bellman-Ford for k+1 rounds, copying the distance array each round.");
            P("swim-in-rising-water", "Swim in Rising Water", H, "Google", "Dijkstra/min-heap where the cost is the max elevation along the path.");
            P("reconstruct-itinerary", "Reconstruct Itinerary", H, "Google, Uber", "Hierholzer's algorithm with lexically sorted destinations; build the path in post-order.");

            current = "dp-1d";
            P("climbing-stairs", "Climbing Stairs", E, "Amazon, Adobe", "ways(n) = ways(n-1) + ways(n-2) — Fibonacci with two variables.");
            P("min-cost-climbing-stairs", "Min Cost Climbing Stairs", E, "Amazon", "dp[i] = cost[i] + min(dp[i-1], dp[i-2]); answer min of the last two.");
            P("house-robber", "House Robber", M, "Amazon, Microsoft, Google", "dp[i] = max(dp[i-1], dp[i-2] + nums[i]).");
            P("house-robber-ii", "House Robber II", M, "Amazon, Microsoft", "Circle: run House Robber on [0..n-2] and [1..n-1], take the max.");
            P("longest-palindromic-substring", "Longest Palindromic Substring", M, "Amazon, Microsoft, Adobe", "Expand around each centre (odd and even); keep the longest.");
            P("palindromic-substrings", "Palindromic Substrings", M, "Meta, Amazon", "Expand around 2n-1 centres and count each palindrome found.");
            P("decode-ways", "Decode Ways", M, "Meta, Amazon, Microsoft", "dp[i] += dp[i-1] if s[i] valid single digit; += dp[i-2] if s[i-1..i] in 10..26.");
            P("coin-change", "Coin Change", M, "Amazon, Microsoft, Goldman Sachs", "dp[a] = 1 + min(dp[a - coin]) over coins; initialise with infinity.");
            P("maximum-product-subarray", "Maximum Product Subarray", M, "Amazon, Microsoft", "Track both the max and min product ending here — a negative flips them.");
            P("word-break", "Word Break", M, "Amazon, Meta, Google", "dp[i] true if some j < i has dp[j] true and s[j..i) in the dictionary.");
            P("longest-increasing-subsequence", "Longest Increasing Subsequence", M, "Amazon, Microsoft, Google", "O(n²) dp, or patience sorting: binary search the tails array for O(n log n).");
            P("partition-equal-subset-sum", "Partition Equal Subset Sum", M, "Meta, Amazon", "Target = sum/2; 0/1 knapsack boolean dp iterating the target backwards.");

            current = "dp-2d";
            P("unique-paths", "Unique Paths", M, "Google, Amazon", "dp[r][c] = dp[r-1][c] + dp[r][c-1]; a single row array is enough.");
            P("longest-common-subsequence", "Longest Common Subsequence", M, "Amazon, Google", "Match → 1 + dp[i-1][j-1], else max(dp[i-1][j], dp[i][j-1]).");
            P("best-time-to-buy-and-sell-stock-with-cooldown", "Best Time to Buy and Sell Stock with Cooldown", M, "Amazon", "State machine: hold, sold, rest — transition each day.");
            P("coin-change-ii", "Coin Change II", M, "Amazon", "Unbounded knapsack counting: loop coins outer, amounts inner, dp[a] += dp[a - coin].");
            P("target-sum", "Target Sum", M, "Meta, Amazon", "Reduce to counting subsets with sum (total + target) / 2, or memoise (i, sum).");
            P("interleaving-string", "Interleaving String", M, "Amazon", "dp[i][j]: can s1[:i] and s2[:j] form s3[:i+j]?");
            P("edit-distance", "Edit Distance", M, "Amazon, Google, Microsoft", "Match → dp[i-1][j-1]; else 1 + min(insert, delete, replace).");
            P("longest-increasing-path-in-a-matrix", "Longest Increasing Path in a Matrix", H, "Google, Amazon", "DFS from every cell with memoisation; strictly increasing means no cycles.");
            P("distinct-subsequences", "Distinct Subsequences", H, "Amazon", "dp[i][j] = dp[i-1][j] (+ dp[i-1][j-1] if s[i-1] == t[j-1]).");
            P("burst-balloons", "Burst Balloons", H, "Google", "Interval DP: choose the LAST balloon to burst in (l, r).");
            P("regular-expression-matching", "Regular Expression Matching", H, "Meta, Google, Amazon", "dp[i][j]; on '*' either skip the pair (j-2) or consume one char if it matches.");

            current = "greedy";
            P("maximum-subarray", "Maximum Subarray", M, "Amazon, Microsoft, LinkedIn", "Kadane: current = max(x, current + x); track the best.");
            P("jump-game", "Jump Game", M, "Amazon, Microsoft", "Track the farthest reachable index; fail if i ever exceeds it.");
            P("jump-game-ii", "Jump Game II", M, "Amazon, Google", "BFS by ranges: each jump extends the current window to the farthest reach.");
            P("gas-station", "Gas Station", M, "Amazon, Google", "If total gas ≥ total cost a start exists; reset the start whenever the running tank goes negative.");
            P("hand-of-straights", "Hand of Straights", M, "Google", "Count map; always start a group from the smallest remaining card.");
            P("merge-triplets-to-form-target-triplet", "Merge Triplets to Form Target Triplet", M, "Google", "Ignore triplets exceeding the target anywhere; check the rest can supply each target value.");
            P("partition-labels", "Partition Labels", M, "Amazon", "Record each char's last index; cut when i reaches the max last index in the current part.");
            P("valid-parenthesis-string", "Valid Parenthesis String", M, "Amazon, Meta", "Track the range [minOpen, maxOpen] of possible open counts.");

            current = "intervals";
            P("merge-intervals", "Merge Intervals", M, "Amazon, Google, Meta, Microsoft", "Sort by start; merge into the last interval while they overlap.");
            P("insert-interval", "Insert Interval", M, "Google, Amazon", "Add intervals ending before the new one, merge overlaps, then add the rest.");
            P("non-overlapping-intervals", "Non-overlapping Intervals", M, "Amazon, Meta", "Sort by end; greedily keep intervals that start after the last kept end.");
            P("minimum-number-of-arrows-to-burst-balloons", "Minimum Number of Arrows to Burst Balloons", M, "Amazon", "Sort by end; shoot at the end of the first balloon, skip all it pierces.");
            P("minimum-interval-to-include-each-query", "Minimum Interval to Include Each Query", H, "Google", "Sort queries and intervals; push started intervals into a min-heap by size, pop expired ones.");

            current = "math";
            P("rotate-image", "Rotate Image", M, "Amazon, Microsoft, Apple", "Transpose, then reverse each row.");
            P("spiral-matrix", "Spiral Matrix", M, "Microsoft, Amazon, Google", "Shrink top/bottom/left/right boundaries after each side traversal.");
            P("set-matrix-zeroes", "Set Matrix Zeroes", M, "Microsoft, Amazon", "Use the first row/column as markers (with a flag for the first column).");
            P("happy-number", "Happy Number", E, "Amazon", "Repeated digit-square sums either reach 1 or cycle — detect with a set or Floyd.");
            P("plus-one", "Plus One", E, "Google", "Add from the last digit; handle the all-9s case by prepending 1.");
            P("powx-n", "Pow(x, n)", M, "Meta, Amazon, Microsoft", "Exponentiation by squaring; for negative n, invert x (watch int overflow).");
            P("multiply-strings", "Multiply Strings", M, "Meta, Amazon", "Digit i × digit j adds into position i + j + 1 of a result array.");
            P("detect-squares", "Detect Squares", M, "Google", "Count points; for each query pick a diagonal point and look up the other two corners.");

            current = "bits";
            P("single-number", "Single Number", E, "Amazon, Google", "XOR everything — pairs cancel out.");
            P("number-of-1-bits", "Number of 1 Bits", E, "Apple, Microsoft", "n &= n - 1 removes one set bit per iteration.");
            P("counting-bits", "Counting Bits", E, "Amazon", "bits[i] = bits[i >> 1] + (i & 1).");
            P("reverse-bits", "Reverse Bits", E, "Apple", "Shift result left and OR in the lowest bit of n, 32 times.");
            P("missing-number", "Missing Number", E, "Amazon, Microsoft", "XOR all indices and values, or n(n+1)/2 - sum.");
            P("sum-of-two-integers", "Sum of Two Integers", M, "Meta", "XOR gives the sum without carry, (a & b) << 1 gives the carry; repeat until no carry.");
            P("reverse-integer", "Reverse Integer", M, "Bloomberg, Apple", "Pop digits with % and /; check for overflow before pushing.");

            Problems = list;
        }

        public static PatternDef? GetPattern(string key) => Patterns.FirstOrDefault(p => p.Key == key);

        public static string PatternName(string key) => GetPattern(key)?.Name ?? key;

        public static int PatternIndex(string key)
        {
            for (int i = 0; i < Patterns.Count; i++) if (Patterns[i].Key == key) return i;
            return Patterns.Count;
        }

        /// <summary>LeetCode skill tags we can compare against the user's public profile, keyed by our pattern.</summary>
        public static readonly IReadOnlyDictionary<string, string[]> PatternToLeetCodeTags = new Dictionary<string, string[]>
        {
            ["arrays"] = new[] { "hash-table", "array" },
            ["two-pointers"] = new[] { "two-pointers" },
            ["sliding-window"] = new[] { "sliding-window" },
            ["stack"] = new[] { "stack", "monotonic-stack" },
            ["binary-search"] = new[] { "binary-search" },
            ["linked-list"] = new[] { "linked-list" },
            ["trees"] = new[] { "tree", "binary-tree", "binary-search-tree" },
            ["tries"] = new[] { "trie" },
            ["heap"] = new[] { "heap-priority-queue" },
            ["backtracking"] = new[] { "backtracking" },
            ["graphs"] = new[] { "graph", "depth-first-search", "breadth-first-search", "union-find" },
            ["adv-graphs"] = new[] { "shortest-path", "minimum-spanning-tree", "topological-sort" },
            ["dp-1d"] = new[] { "dynamic-programming" },
            ["dp-2d"] = new[] { "dynamic-programming" },
            ["greedy"] = new[] { "greedy" },
            ["intervals"] = new[] { "sorting" },
            ["math"] = new[] { "math", "matrix" },
            ["bits"] = new[] { "bit-manipulation" },
        };
    }
}
