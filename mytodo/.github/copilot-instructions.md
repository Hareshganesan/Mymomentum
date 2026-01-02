# MyTodo - Windows Desktop To-Do & Goal Tracking App

## Project Overview
A WPF .NET 8 Windows desktop To-Do and Goal Tracking app with modern fluent UI design.

## Tech Stack
- **Platform**: Windows
- **Framework**: WPF (.NET 8)
- **Storage**: In-memory + JSON file persistence
- **Pattern**: MVVM-lite

## Key Features
- Modern, minimal UI with glass/fluent effects and soft shadows
- Dark mode by default with optional light toggle
- Smooth animations for adding/completing tasks
- Visual progress indicators (circle/bar)
- Color feedback (green=good, yellow=warning, red=urgent)
- Dynamic motivational text and completion celebrations
- Streak counter
- Today view for daily tasks
- Simple goals section with progress tracking

## Build Instructions
```bash
dotnet restore
dotnet build
dotnet run
```

## Project Structure
- `Models/` - TaskItem, Goal, and AppData classes
- `ViewModels/` - MainViewModel with app logic and RelayCommand
- `Views/` - MainWindow XAML UI with code-behind
- `Services/` - JsonStorageService for persistence
- `Converters/` - XAML value converters
- `Resources/` - Themes, Styles, and Animations XAML

## Progress
- [x] Create copilot-instructions.md
- [x] Scaffold WPF .NET 8 project
- [x] Create app models and view models
- [x] Build main UI with XAML
- [x] Implement JSON save/load
- [x] Add animations and effects
- [x] Create README.md
