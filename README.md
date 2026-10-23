# Momentum

A Windows desktop app for placement and interview preparation. Momentum looks at where you are (your LeetCode history plus what you do in the app) and tells you what to work on today. Everything is stored locally on your PC.

## Features

| Page | What it does |
|---|---|
| Today | A daily plan: reviews that are due, new problems from your weakest pattern or from a company whose test is coming up, and Core CS cards. Shows pace against your placement date, readiness, and a consistency heatmap. |
| Problems | About 160 interview problems in 18 patterns, ordered as a learning path. Each problem has a hint, your notes, mistakes, company tags and a review schedule. You can add your own. |
| Review | Spaced repetition for problems and flashcards. Recall the approach, reveal it, then grade yourself. |
| Core CS | Short interview answers across OS, DBMS, SQL, Computer Networks, OOP and System Design, with review tracking. |
| Mock interview | Timed rounds with interviewer-style phases (understand, approach, code, test), hints and a scored debrief. |
| Applications | Tracks every application and campus drive as a journey from applied to offer. It can update itself from your inbox (see below). |
| HR and stories | A "Tell me about yourself" builder, STAR stories tagged by theme, and a question bank that shows which questions your stories cover. |
| Insights | A readiness score broken into parts, pattern mastery, weekly activity and a one-year heatmap. |

## LeetCode sync

Enter your LeetCode username in Settings. No password is needed. Momentum syncs when it opens: solved counts, streak, skill tags, the submission calendar and recent accepted problems. To import older solves as well, you can optionally paste your `LEETCODE_SESSION` cookie in Settings. It never leaves your PC.

## Inbox job tracking

Momentum can read job-related emails and move your applications forward on its own (applied, online test, interview, offer, rejected). Junk from job portals, coding sites and newsletters is filtered out, and placement cell emails are turned into campus drive entries.

Two ways to connect:

- Google sign-in (read-only Gmail access). You create your own OAuth client in Google Cloud Console and paste its Client ID and secret into the app. Momentum only requests the `gmail.readonly` scope.
- IMAP with an app password, for other providers or if you prefer not to use OAuth.

## Privacy

- All data stays on your computer in `%LOCALAPPDATA%\Momentum\users\<username>\`.
- Account passwords are stored only as salted PBKDF2 hashes.
- The Google client secret, Gmail token, mail app password and LeetCode cookie are encrypted with Windows DPAPI, so they can only be read by your Windows user on your PC.
- Nothing is sent anywhere except to LeetCode and to your own mail provider.

## Download and run

Download `Momentum.exe` from the Releases page and run it. It is a single self-contained file and does not need .NET installed.

### Windows SmartScreen warning

The first time you run it, Windows may show "Windows protected your PC" or "Unknown publisher". This happens because the app is not code-signed, which is common for free open-source tools. It does not mean the file is harmful.

To run it:

1. Click **More info**.
2. Click **Run anyway**.

Alternatively, right-click `Momentum.exe`, open **Properties**, tick **Unblock**, and click OK.

To check that your download has not been modified, compare its SHA-256 hash with the one in the release notes:

```powershell
certutil -hashfile Momentum.exe SHA256
```

## Build from source

Requires the .NET 8 SDK.

```powershell
cd V4
dotnet run
```

To build the standalone executable:

```powershell
cd V4
dotnet publish Momentum.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o ..\V4_exe
```

Set the `MOMENTUM_DATA_DIR` environment variable to use a different data folder (useful for testing).

## Tech stack

- C# and WPF on .NET 8, MVVM
- Local JSON storage
- MailKit for IMAP, Gmail REST API for Google sign-in
- Geist font (SIL Open Font License)

## Repository layout

- `V4` - current version (source)
- `V4_exe` - current standalone build
- `V2`, `V3`, `V3_exe`, `dist` - earlier versions, kept for reference
