using System;
using System.Collections.Generic;

namespace Momentum.Content
{
    public enum NodeHighlight
    {
        None,
        Active,     // Current pointer / target being evaluated (amber highlight)
        Comparing,  // Secondary element in comparison (blue highlight)
        Matched,    // Success match found / target reached (emerald green)
        Visited,    // Already processed prefix element (graphite tint)
        Discarded   // Pruned or eliminated branch (dimmed)
    }

    public sealed class VisualDataNode
    {
        public string Value { get; init; } = string.Empty;
        public string IndexLabel { get; init; } = string.Empty; // e.g. "[0]", "[1]"
        public string PointerTag { get; init; } = string.Empty; // e.g. "i", "L", "R", "mid"
        public NodeHighlight Highlight { get; init; } = NodeHighlight.None;
        public int HeightPercent { get; init; } = 60;           // 20-100 for bar charts
    }

    public sealed class VariableWatch
    {
        public string Name { get; init; } = string.Empty;       // e.g. "i", "seen", "diff"
        public string Value { get; init; } = string.Empty;      // e.g. "2", "{1, 2}", "7"
        public bool IsChanged { get; init; }                    // Glow highlight if modified in this step
    }

    public sealed class VisualStep
    {
        public int StepIndex { get; init; }
        public string Title { get; init; } = string.Empty;             // e.g. "Query Hash Set for Complement"
        public int LineNumber { get; init; }                           // e.g. 4
        public string CodeLine { get; init; } = string.Empty;          // e.g. "if complement in seen:"
        public string ActionSummary { get; init; } = string.Empty;     // Plain English one-liner
        public string DeepExplanation { get; init; } = string.Empty;   // Deep mental model & algorithmic mechanics
        public string InvariantNote { get; init; } = string.Empty;     // Correctness invariant guarantee
        public List<VisualDataNode> Nodes { get; init; } = new();      // Visual elements in the data structure
        public List<VariableWatch> Variables { get; init; } = new();   // Scope variable table
        public List<string> AuxiliaryMemory { get; init; } = new();    // Auxiliary sets, maps, stacks
    }

    public sealed class CodeLineExplanation
    {
        public int LineNumber { get; init; }
        public string CodeLine { get; init; } = string.Empty;
        public string WhatItDoes { get; init; } = string.Empty;
        public string VariableImpact { get; init; } = string.Empty;
        public string WhyThisLine { get; init; } = string.Empty;
    }

    /// <summary>
    /// Comprehensive training and coaching module for a DSA problem.
    /// Original, clean-room educational content providing Brute Force vs. Optimal pedagogical breakdowns,
    /// interactive line-by-line step execution with variable inspectors and visual state graphs,
    /// and Socratic masterclasses with clarifying questions, pitfalls, and interviewer follow-ups.
    /// </summary>
    public sealed class ProblemTraining
    {
        public string Slug { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Pattern { get; init; } = string.Empty;
        public string PatternName { get; init; } = string.Empty;

        // 1. Problem Deconstruction & Mental Framing
        public string GoalSummary { get; init; } = string.Empty;
        public string ConstraintsNote { get; init; } = string.Empty;

        // 2. Brute Force Approach (The Intuitive Starting Point)
        public string BruteForceTitle { get; init; } = string.Empty;
        public string BruteForceConcept { get; init; } = string.Empty;
        public string BruteForceAlgorithm { get; init; } = string.Empty;
        public string BruteForceCodeCpp { get; init; } = string.Empty;
        public string BruteForceCodePython { get; init; } = string.Empty;
        public string BruteForceCodeJava { get; init; } = string.Empty;

        public string BruteForceCode
        {
            get => !string.IsNullOrEmpty(BruteForceCodeCpp) ? BruteForceCodeCpp : _bruteForceCode;
            init => _bruteForceCode = value;
        }
        private string _bruteForceCode = string.Empty;

        public string BruteForceTime { get; init; } = string.Empty;
        public string BruteForceSpace { get; init; } = string.Empty;
        public string BruteForceBottleneck { get; init; } = string.Empty; // Why it fails / TLE

        // 3. Optimal Approach (The Interview-Winning Target)
        public string OptimalTitle { get; init; } = string.Empty;
        public string AhaMoment { get; init; } = string.Empty; // The breakthrough intuition
        public string OptimalIntuition { get; init; } = string.Empty;
        public string OptimalAlgorithm { get; init; } = string.Empty;
        public string OptimalCodeCpp { get; init; } = string.Empty;
        public string OptimalCodePython { get; init; } = string.Empty;
        public string OptimalCodeJava { get; init; } = string.Empty;
        public string OptimalTime { get; init; } = string.Empty;
        public string OptimalTimeProof { get; init; } = string.Empty;
        public string OptimalSpace { get; init; } = string.Empty;
        public string OptimalSpaceProof { get; init; } = string.Empty;

        // 4. Interactive "Teach Me" Walkthrough
        public string ExampleInput { get; init; } = string.Empty;
        public string ExampleOutput { get; init; } = string.Empty;
        public string[] SimulationSteps { get; init; } = Array.Empty<string>();
        public string[] ClarifyingQuestions { get; init; } = Array.Empty<string>();
        public string[] CommonPitfalls { get; init; } = Array.Empty<string>();
        public string[] FollowUpQuestions { get; init; } = Array.Empty<string>();

        // 5. Interactive Algorithm Visualizer & State Stepper
        public List<VisualStep> VisualSteps { get; init; } = new();

        // 6. Line-by-Line Code & Variable Deconstruction
        public List<CodeLineExplanation> LineExplanations { get; init; } = new();
    }
}
