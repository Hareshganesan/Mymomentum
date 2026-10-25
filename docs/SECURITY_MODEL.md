# Momentum Execution Security & Process Boundary Analysis

## 1. Threat Model & Overview

Executing user-supplied code on a candidate's local machine introduces potential stability, resource consumption, and security challenges. Momentum enforces defense-in-depth measures while documenting the exact boundaries of the Windows operating system model honestly.

---

## 2. Implemented Safeguards

### A. Subprocess Isolation
Untrusted Python code is **never** executed inside the main WPF application process or CLR runtime. Execution occurs in a spawned OS worker process via `ProcessStartInfo`. An unhandled fatal crash, memory corruption, or native fault inside the Python process cannot destabilize or crash the parent WPF application.

### B. Standard Output Redirection & Protocol Quarantining
Candidate `print()` statements are intercepted inside the Python runner using an in-memory `io.StringIO()` stream, capped at 4 KB per test case. Structured test grading data is emitted over an isolated communication stream bracketed by unique delimiter markers (`__MOMENTUM_OUTPUT_START__` and `__MOMENTUM_OUTPUT_END__`). This completely prevents candidates' stdout floods from corrupting or desynchronizing IPC data.

### C. Output Buffer Limits (Anti-DOS)
Standard output and standard error event handlers enforce strict 64 KB per-channel buffer limits. If user code attempts to flood output (`while True: print(...)`), the buffer truncates after 64 KB, preventing memory starvation or process hangs.

### D. Process Tree Wall-Clock Termination
Every execution is bounded by an explicit wall-clock timeout (default 3,000ms). If an execution exceeds this threshold or if the user cancels execution, `process.Kill(entireProcessTree: true)` terminates the runner and any child processes spawned by it.

### E. Ephemeral Working Directory Isolation
Each evaluation generates a temporary directory under `%TEMP%\MomentumAssessmentLab\<guid>`. The script is executed within this directory and the entire directory tree is deleted in a `finally` block upon completion.

---

## 3. Absent Protections & Honest Recruiter Disclosure

When presenting this project to senior engineering interviewers, architectural credibility requires clearly disclosing boundaries that are not present:

1. **No Kernel / Hyper-V Containerization**: Subprocesses run under the standard Windows Access Token of the interactive user. The process has the same NTFS file read/write permissions as the logged-in user account.
2. **Environment Variable Scrubbing is NOT Disk Virtualization**: Removing `%APPDATA%` and `%LOCALAPPDATA%` prevents simple lookup via `os.environ`, but untrusted code can still access files via absolute file paths or `os.path.expanduser("~")`.
3. **No Network Socket Firewalling**: If Python code imports `socket` or `urllib`, outbound connections are not blocked at the OS layer.
4. **No CPU Core Throttling**: A tight busy loop can utilize 100% of a single CPU core during the 3,000ms execution window before process tree termination occurs.
