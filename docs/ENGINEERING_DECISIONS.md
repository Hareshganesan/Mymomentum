# Momentum Architecture & Engineering Decision Records (ADR)

This document records the foundational engineering tradeoffs, architectural decisions, and technical boundaries established during the development of Momentum V6.

---

## ADR-001: Subprocess-Based Execution vs. In-Process Interpreters

### Context
Evaluating candidate-submitted Python code requires executing arbitrary, untrusted logic on the candidate's machine. Embedding an in-process interpreter (such as Python.NET, IronPython, or a direct C-API binding) introduces risks of unmanaged memory corruption, unhandled native crashes, and inability to cleanly cancel hung executions without terminating the parent UI application.

### Decision
Execute candidate code as an **out-of-process OS worker subprocess** via `System.Diagnostics.Process` with standard I/O redirection:
- **Crash Isolation**: A segmentation fault, recursion crash, or out-of-memory fault inside Python leaves the parent WPF process intact.
- **Wall-Clock Termination**: A strict timer (default 3,000ms) enforces termination via `process.Kill(entireProcessTree: true)`.
- **Buffer Bounding**: Standard output and error readers cap capture buffers at 64 KB per channel to prevent memory starvation from infinite print loops.

### Trade-offs & Security Boundaries
- **Process Separation is NOT Kernel Sandboxing**: The worker process runs under the interactive user's Windows access token and has the same NTFS filesystem permissions.
- **Prerequisite Dependency**: Requires a compatible Python interpreter (Python 3.8+) detected on the host system.

---

## ADR-002: Python-First Deployment Model vs. Multi-Language Toolchain Bundling

### Context
Placement candidates frequently ask whether coding environments should support C++ and Java alongside Python. Commercial cloud platforms (LeetCode, HackerRank) support 40+ languages by executing submissions inside multi-gigabyte Docker container images on remote Linux server clusters.

### Decision
Prioritize **Python 3.8+ exclusively** for the initial V6 Assessment Lab release:
- **Alignment with Placement Reality**: In technical interview assessments, Python is the overwhelmingly preferred language due to its concise syntax and rich standard library.
- **Local-First Toolchain Footprint**: Windows does not ship with a native C++ compiler (requiring multi-gigabyte Visual Studio C++ workloads or MinGW/Clang distributions) or a Java Development Kit (requiring `javac.exe` to compile source to `.class` bytecode).
- **Execution Pipeline Simplicity**: Python executes directly via the interpreter without requiring a multi-stage compilation pipeline.

### Future Multi-Language Roadmap (V7)
When expanding to compiled languages, the harness abstraction must be designed as a **two-phase lifecycle contract** (`CompileAsync` ➔ `ExecuteAsync`) to distinctly report compilation failures, syntax diagnostics, and build timeouts prior to test-case execution.

---

## ADR-003: Empirical Failure Forensics vs. Binary Test Verdicts

### Context
Standard competitive programming and assessment platforms return opaque binary verdicts (`Wrong Answer`, `Runtime Error`). This fails to help candidates diagnose the underlying computer science invariant that caused the failure.

### Decision
Implement the **Failure Forensics Engine**, categorizing failures into a structured taxonomy:
- **Three-Part Evidence Model**:
  1. *Observed Empirical Fact*: Concrete failure trace observed on public tests (e.g. `returned [] for input [[3, 3], 6]`).
  2. *Underlying CS Invariant*: Architectural explanation of the failure mode (e.g. dictionary comprehension key collisions on duplicate elements).
  3. *Targeted Actionable Guidance*: Conceptual advice directing the candidate toward the correct algorithmic pattern without spoiling the code.
- **Confidentiality Rule**: Hidden test inputs and expected outputs are **strictly withheld** from candidate-facing UI and diagnostic messages, preserving placement assessment realism.

---

## ADR-004: Agentic Era AI Fluency: Supervision & Invariant Verification

### Context
With the proliferation of AI coding assistants (Copilot, Cursor, Devin), technical hiring platforms (e.g. HackerRank Chakra & Plan Mode) are shifting from evaluating rote syntax generation to assessing **AI Fluency and Code Supervision**: the ability to direct AI agents, audit synthetic code drafts, catch non-obvious latent bugs, and verify software invariants.

### Decision
Introduce an **AI Fluency & Code Audit Mode** directly inside the Assessment Lab:
- **Simulated AI Drafts with Seeded Invariants**: Challenges provide plausible, syntactically clean AI assistant drafts containing authentic latent flaws (e.g., dict comprehension key overwrites on duplicates, set equality frequency blindness, non-advancing binary ranges, in-place caller argument mutation).
- **AI Assumptions vs. Reality**: Exposes the simulated AI prompt and rationale alongside empirical failure forensics to train candidates in critical supervision.
- **AI Fluency Telemetry**: Dedicated tracking of audited questions verified.

---

## ADR Summary Matrix

| Architectural Area | V6 Implemented Position | Next Milestone (V7) |
|---|---|---|
| **Execution Engine** | External OS Subprocess with Process Tree Termination | Preserve and harden existing runner |
| **Runtime Availability** | System Python 3.8+ required on PATH | Evaluate bundled Windows embeddable Python distribution |
| **Security Model** | Process separation and buffer bounding (Explicitly not a sandbox) | Document security boundary transparently |
| **Language Support** | Python 3.x only | Design two-phase compilation/execution contract when needed |
| **Harness Architecture** | Concrete `PythonExecutionHarness` | Introduce `IExecutionHarness` abstraction when a second language justifies it |
