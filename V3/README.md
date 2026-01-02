# ✨ MyTodo Premium - Personal Productivity Coach

A beautiful Windows desktop To-Do and Goal Tracking app built with WPF .NET 8. Designed to feel like a personal productivity coach with advanced features like Focus Mode, Daily Highlights, Achievement System, and Multi-Page Navigation.

![Dark Mode](https://img.shields.io/badge/Theme-Dark%20Mode-1a1a2e)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512bd4)
![WPF](https://img.shields.io/badge/Framework-WPF-0078d4)

## ✨ Core Features

### 🎨 Beautiful UI
- Modern fluent design with glass effects and soft shadows
- Animated particle background with floating gold elements
- Dark mode by default (with theme toggle)
- Full-screen mode support
- Multi-page navigation (Home, Achievements, History, Settings)
- Large typography and clean spacing
- Rounded cards with hover effects
- Smooth animations throughout

### 📝 Smart Task Management
- Add tasks with optional due dates
- **Energy Tags** 🧠 Deep Work / ⚡ Quick Win / 😴 Low Energy
- Mark tasks complete with satisfying animations
- Today view for focused daily productivity
- Color-coded status indicators:
  - 🟢 Green → Completed / doing well
  - 🟡 Yellow → Due today / falling behind
  - 🔴 Red → Overdue / urgent

### 🎯 Focus Mode
- **Pomodoro Timer** - 25-minute focused work sessions
- Distraction-free overlay mode
- Start, pause, and reset timer controls
- Complete focused tasks with enhanced celebration

### ⭐ Daily Highlight
- Automatically selects your **#1 priority** task each day
- Manual selection available
- Special celebration when completed
- Visual "HIGHLIGHT" badge on tasks

### 🔥 Streak Protection
- Track consecutive productive days
- **2-day grace period** if you forget
- Pause/Resume streak feature
- Best streak tracking

### 🌅 Daily Check-ins
- **Morning Check-in** - Set intentions for the day
- **Evening Check-in** - Reflect on your progress
- Beautiful overlay panels with gold gradients
- Reflections saved with timestamps

### 🏆 Achievement System
12 unlockable badges:
- 🎯 **First Task** - Complete your first task
- ⚡ **Productive** - Complete 10 tasks
- 💪 **Committed** - Complete 50 tasks
- 🏅 **Elite** - Complete 100 tasks
- 🔥 **Week Warrior** - 7-day streak
- 🚀 **Month Master** - 30-day streak
- 💯 **Perfect Day** - 100% completion
- ✨ **Perfect Week** - 10 perfect days
- 🌅 **Early Bird** - Morning check-in
- 🌙 **Night Owl** - Evening check-in
- 🎖️ **Goal Crusher** - Complete a goal
- 🧘 **Focus Master** - Complete 25 tasks

### 📖 History & Reflections
- View all completed tasks with timestamps
- Browse past daily reflections
- Track perfect days and streaks
- Productivity statistics

### 🎯 Goals Tracking
- Simple goals section (e.g., "Finish Java", "Workout")
- Progress tracking with visual percentage bars
- Quick +10% increment buttons
- Color-coded progress feedback

### 💪 Motivation Features
- Dynamic motivational messages
- 🎊 Confetti celebration when completing all tasks
- 🔥 Streak counter tracking
- Encouraging empty state messages
- Progress indicators and mini circular charts

### 💾 Data Persistence
- Automatic JSON file save/load
- Data stored in `%LOCALAPPDATA%\MyTodo\data.json`
- Includes tasks, goals, streaks, reflections, and highlights
- No database required

## 🚀 Getting Started

### Prerequisites
- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build & Run

```powershell
# Clone or navigate to the project folder
cd mytodo

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the app
dotnet run
```

### Or Build Release Version

```powershell
dotnet publish -c Release -r win-x64 --self-contained
```

## 📁 Project Structure

```
MyTodo/
├── App.xaml                       # Application entry & resources
├── App.xaml.cs
├── MyTodo.csproj                  # Project configuration
├── Models/
│   ├── TaskItem.cs                # Task data model with Energy Tags
│   ├── Goal.cs                    # Goal data model
│   ├── DailyReflection.cs         # Morning/Evening reflections
│   └── AppData.cs                 # Container for JSON serialization
├── ViewModels/
│   └── MainViewModel.cs           # Main app logic (MVVM-lite)
│                                  # Navigation, Focus Mode, Highlights, etc.
├── Views/
│   ├── MainWindow.xaml            # Multi-page UI layout
│   └── MainWindow.xaml.cs         # Window code-behind + animations
├── Services/
│   └── JsonStorageService.cs      # JSON file persistence
├── Converters/
│   └── Converters.cs              # Value converters for XAML bindings
├── Controls/
│   ├── CircularProgress.cs        # Custom circular progress control
│   ├── StatCard.cs                # Statistics display card
│   └── WeeklyChart.cs             # 7-day completion chart
├── Resources/
│   ├── Themes.xaml                # Color palette & effects
│   ├── Styles.xaml                # UI component styles
│   └── Animations.xaml            # Animation definitions
└── README.md
```

## 🎯 Usage Tips

### Daily Workflow
1. **Morning**: Open the app → Complete morning check-in → Review Daily Highlight
2. **During Day**: Work on tasks → Use Focus Mode for deep work → Check streak status
3. **Evening**: Complete tasks → Evening reflection → View progress

### Keyboard Shortcuts
- Enter in task input → Add task
- Click task → Toggle completion
- Focus Mode → Immersive Pomodoro session

### Energy Tags Guide
- 🧠 **Deep Work**: Complex tasks requiring focus (coding, writing, planning)
- ⚡ **Quick Win**: Fast tasks you can knock out quickly (emails, calls)
- 😴 **Low Energy**: Easy tasks for when you're tired (organizing, reading)

## 🛠️ Technical Details

### Architecture
- **Pattern**: MVVM-lite (Model-View-ViewModel)
- **UI Framework**: WPF with XAML
- **Platform**: .NET 8 (Windows only)
- **Storage**: JSON file-based (no database)
- **Custom Controls**: CircularProgress, StatCard, WeeklyChart

### Key Components
- **MainViewModel** (1400+ lines): All business logic, commands, and state management
- **MainWindow.xaml** (1300+ lines): Multi-page layout with 4 pages + overlays
- **AppData Model**: Serializes tasks, goals, streaks, reflections, daily highlights
- **BadgeInfo System**: Dynamic achievement tracking with 12 unlockable badges

### Performance
- Minimal memory footprint
- Instant startup
- Smooth 60fps animations
- Background particle system with DispatcherTimer

## 📝 License

MIT License - Feel free to use and modify!

## 🙏 Credits

Built with ❤️ using WPF .NET 8
Icons: Unicode Emojis
Theme: Black & Gold Premium Design

---

**MyTodo Premium** - Your personal productivity coach 🚀
│   ├── MainWindow.xaml         # Main UI layout
│   └── MainWindow.xaml.cs      # Window code-behind
├── Services/
│   └── JsonStorageService.cs   # JSON file persistence
├── Converters/
│   └── Converters.cs           # Value converters for XAML
├── Resources/
│   ├── Themes.xaml             # Color palette & effects
│   ├── Styles.xaml             # UI component styles
│   └── Animations.xaml         # Animation definitions
└── README.md
```

## 🎮 Usage

1. **Add a Task**: Type in the input field and press Enter or click "Add"
2. **Set Due Date**: Use the date picker below the input (optional)
3. **Complete a Task**: Click the circle checkbox on the left
4. **Delete a Task**: Click the trash icon on the right
5. **Add a Goal**: Type in the goals section and click "Add Goal"
6. **Update Goal Progress**: Click "+10%" to increment progress
7. **Toggle Theme**: Click the sun/moon icon in the title bar

## 🎨 Design Philosophy

> "Prioritize feel over features. Everything should push the user to complete tasks."

- **Minimal UI**: Fewer buttons, fewer options, maximum focus
- **Visual Feedback**: Every action has satisfying visual response
- **Motivation First**: Encouraging messages and celebrations
- **Beautiful Defaults**: Dark mode with carefully chosen colors

## 🛠️ Tech Stack

- **Platform**: Windows
- **Framework**: WPF (.NET 8)
- **Pattern**: MVVM-lite (simple ViewModels with INotifyPropertyChanged)
- **Storage**: In-memory + JSON file persistence
- **No External Dependencies**: Pure WPF, no NuGet packages required

## 📄 License

MIT License - Feel free to use and modify!

---

Made with 💜 for productivity enthusiasts
