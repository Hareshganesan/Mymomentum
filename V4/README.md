# Momentum 4 — placement prep coach

Momentum looks at where you actually are (your LeetCode history plus everything you do in the app) and tells you what to do **today**.

| Page | What it does |
|---|---|
| **Today** | A daily plan built for you: reviews that are due, new problems from your weakest pattern (or from a company whose OA is coming up), and Core CS cards. Also shows pace vs. your placement date, readiness, and a consistency heatmap. |
| **Problems** | About 160 interview problems grouped into 18 patterns, ordered as a learning path. Each pattern says when to use it and the general approach. Each problem has a hint, your key-insight notes, mistakes, company tags, and its review schedule. You can add your own problems. |
| **Review** | Spaced repetition for problems and flashcards. Recall the approach, reveal, then grade it (Space, then 1–4). |
| **Core CS** | 71 short interview answers across OS, DBMS, SQL, CN, OOP and System Design, with review tracking. You can add your own cards. |
| **Mock interview** | Timed rounds with interviewer-style phases (Understand → Approach → Code → Test), hints, and a scored debrief. What you write in the debrief goes into that problem's mistake notes. |
| **Applications** | A board for each placement drive. Add a round date and Today starts prioritising that company's problems. |
| **HR & stories** | A "Tell me about yourself" builder, STAR stories tagged by theme, and a 20-question bank that shows which questions your stories cover. |
| **Insights** | Readiness score broken down into points (each part tells you what to do next), pattern mastery next to your LeetCode skill tags, weekly activity, and a 1-year heatmap. |

## LeetCode sync
Enter your username, with no password. Momentum syncs every time it opens: solved counts, streak, skill tags, the submission calendar, and your last 20 accepted problems (the sheet's matching problems get marked solved). To import older solves too, paste your `LEETCODE_SESSION` cookie in Settings (optional). It stays on your PC. Old solves are added to your review queue about 4 per day, so a big import doesn't pile up all at once.

## Run / build
```powershell
..\V4_exe\Momentum.exe                      # standalone, no .NET install needed
dotnet run                                  # from this folder (needs .NET 8 SDK)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ..\V4_exe
```

## Accounts
Momentum opens on a sign-in screen. Each account is local to this PC and keeps its own progress in `%LOCALAPPDATA%\Momentum\users\<username>\momentum.json`. Passwords are stored only as salted PBKDF2 hashes in `accounts.json`. Tick "Keep me signed in" to skip the login screen next time. Use the sign-out button in the sidebar to switch accounts. The first account created on a PC picks up any existing Momentum data: progress from earlier V4 builds, or the Momentum v3 import (LeetCode handle, solved problems, applications).

Set `MOMENTUM_DATA_DIR` to use a different data folder. Export/import backups from Settings.
