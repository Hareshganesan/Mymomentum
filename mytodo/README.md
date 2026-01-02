# ✨ MyTodo - Personal Productivity Coach

A beautiful Windows desktop To-Do and Goal Tracking app built with WPF .NET 8. This app is designed to feel like a personal productivity coach, not just a task manager.

![Dark Mode](https://img.shields.io/badge/Theme-Dark%20Mode-1a1a2e)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512bd4)
![WPF](https://img.shields.io/badge/Framework-WPF-0078d4)

## ✨ Features

### 🎨 Beautiful UI
- Modern fluent design with glass effects and soft shadows
- Dark mode by default (with theme toggle)
- Large typography and clean spacing
- Rounded cards with hover effects
- Smooth animations throughout

### 📝 Task Management
- Add tasks with optional due dates
- Mark tasks as complete with satisfying animations
- Today view for focused daily productivity
- Color-coded status indicators:
  - 🟢 Green → Completed / doing well
  - 🟡 Yellow → Due today / falling behind
  - 🔴 Red → Overdue / urgent

### 🎯 Goals Tracking
- Simple goals section (e.g., "Finish Java", "Workout")
- Progress tracking with visual percentage bars
- Quick +10% increment buttons
- Color-coded progress feedback

### 💪 Motivation Features
- Dynamic motivational messages:
  - "You're ahead today! 🚀"
  - "Just one task left. Finish strong! 💪"
  - "All done! You crushed it today! 🎉"
- 🎊 Confetti celebration when completing all tasks
- 🔥 Streak counter to track consecutive productive days
- Encouraging empty state message

### 💾 Data Persistence
- Automatic JSON file save/load
- Data stored in `%LOCALAPPDATA%\MyTodo\data.json`
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
├── App.xaml                    # Application entry & resources
├── App.xaml.cs
├── MyTodo.csproj               # Project configuration
├── Models/
│   ├── TaskItem.cs             # Task data model
│   ├── Goal.cs                 # Goal data model
│   └── AppData.cs              # Container for JSON serialization
├── ViewModels/
│   └── MainViewModel.cs        # Main app logic (MVVM-lite)
├── Views/
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
