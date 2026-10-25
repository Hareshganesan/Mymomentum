# Momentum: Architectural Design & System Specification

## 1. Architectural Philosophy

Momentum is engineered as an offline-first, high-performance desktop preparation environment for software engineering placement candidates. Rather than functioning as a thin wrapper around cloud APIs, Momentum operates with local-first computation, local persistence, and client-side execution diagnostics.

### Core Architectural Principles
1. **Low-Latency Native Desktop Experience**: Built on .NET 8 WPF with custom DirectX hardware-accelerated rendering and modern styling.
2. **Defensive Subprocess Quarantining**: User-submitted code is isolated from the main CLR runtime inside dedicated OS worker processes.
3. **Evidence-Based Diagnostics**: Diagnostic feedback is strictly categorized based on empirical execution traces, AST compilation states, and output variances.
4. **Deterministic Local Storage**: Application state is persisted locally using atomic temporary file writes and rolling `.bak` backups with zero cloud tracking.

---

## 2. High-Level System Architecture

```
┌────────────────────────────────────────────────────────────────────────┐
│                        Momentum WPF Client (MVVM)                      │
│                                                                        │
│   ┌────────────────────────┐         ┌──────────────────────────────┐  │
│   │   AssessmentLabView    │◄───────►│   AssessmentLabViewModel     │  │
│   │   - Split-pane layout  │ (MVVM)  │   - Auto-saving user code    │  │
│   │   - Monospace editor   │         │   - Real-time telemetry      │  │
│   │   - Forensics card     │         │   - Async process dispatch   │  │
│   └────────────────────────┘         └──────────────┬───────────────┘  │
│                                                     │                  │
│                                                     ▼                  │
│   ┌─────────────────────────────────────────────────────────────────┐  │
│   │               Failure Forensics & Evaluation Core               │  │
│   │                                                                 │  │
│   │   ┌────────────────────────┐        ┌───────────────────────┐   │  │
│   │   │ PythonExecutionHarness │        │ FailureForensicsEngine│   │  │
│   │   │ - Out-of-proc runner   │───────►│ - Diagnostic triage   │   │  │
│   │   │ - Bounded execution    │        │ - Evidence taxonomy   │   │  │
│   │   │ - Environment scrubber │        │ - Actionable guidance │   │  │
│   │   └────────────────────────┘        └───────────────────────┘   │  │
│   └─────────────────────────────────────────────────────────────────┘  │
│                                     │                                  │
│                                     ▼                                  │
│   ┌─────────────────────────────────────────────────────────────────┐  │
│   │               Local-First Persistence (JSON + DPAPI)            │  │
│   │   - Atomic temp + rolling .bak file writes                      │  │
│   │   - Zero cloud telemetry; complete offline operation            │  │
│   └─────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────┬──────────────────────────────────────┘
                                  │ Subprocess invocation (isolated stdio)
                                  ▼
┌────────────────────────────────────────────────────────────────────────┐
│                     Out-of-Process Python Runner                       │
│   - Python 3.8+ system discovery                                       │
│   - Strict memory & environment scrubbing                              │
│   - In-place mutation detection via deep-copy snapshots                │
│   - Canonical JSON comparison with float epsilon (1e-6)                │
│   - Enforced 3000ms wall-clock timeout with process tree termination   │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Component Hierarchy

### A. Presentation Layer (`src/Momentum/Views/` & `ViewModels/`)
- **AssessmentLabView**: Master-detail workspace providing a catalog list, structured problem descriptions, code editor canvas, test execution status, and the Failure Forensics diagnostic card.
- **AssessmentLabViewModel**: State coordinator managing active question selection, filter queries, async test execution dispatch, user code auto-save scheduling, and attempt history recording.
- **MainWindow / ShellViewModel**: Root window container hosting the navigation rail, window chrome controls, theme toggle, and page routing.

### B. Execution & Grading Engine (`src/Momentum/Services/`)
- **PythonExecutionHarness**:
  - Dynamically discovers system `python.exe` across PATH and standard installation paths.
  - Generates a quarantined Python test harness script.
  - Launches execution as an isolated OS subprocess (`ProcessStartInfo`).
  - Buffers standard output and error streams with strict 64 KB memory bounds.
  - Enforces a 3,000ms wall-clock timeout and handles process cancellation via `process.Kill(entireProcessTree: true)`.
  - Captures test results, memory mutations, and runtime exceptions into structured reports.
- **FailureForensicsEngine**:
  - Evaluates empirical failure traces against the diagnostic taxonomy.
  - Distinguishes observed facts from diagnostic hypotheses.
  - Preserves hidden test case confidentiality.
  - Prescribes targeted next steps for skill improvement.

### C. Data Persistence Layer (`src/Momentum/Models/` & `Services/StorageService.cs`)
- **AppState**: Root document model encompassing placement profile, DSA progress, core CS card mastery, job applications, and assessment lab history.
- **StorageService**: Atomic serialization engine writing to `%LOCALAPPDATA%\Momentum\momentum.json` using a temporary file move pattern (`.tmp` ➔ `.bak` ➔ `.json`).
