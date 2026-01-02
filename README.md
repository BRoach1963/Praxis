# Praxis

**A calm, private workspace for reflective professional practice.**

Developed by Prickly Cactus Software.

## Overview

Praxis is a desktop-first application (WPF, .NET 8) designed for therapists, counselors, and small mental health practices. It helps clinicians maintain continuity across sessions, track client goals and progress, and reflect thoughtfully—without the administrative overhead of EHR systems.

## Getting Started

### Prerequisites

- .NET 8 SDK
- Visual Studio 2022 or VS Code with C# extension

### Build & Run

```powershell
# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run --project src/Praxis/Praxis.csproj
```

## Project Structure

```
Praxis/
├── src/
│   └── Praxis/                 # Main WPF application
│       ├── Data/               # EF Core DbContext
│       ├── Models/             # Domain entities
│       ├── Services/           # Business logic services
│       ├── Themes/             # XAML styles and colors
│       ├── ViewModels/         # MVVM ViewModels
│       └── Views/              # XAML views (coming soon)
├── PRAXIS-PRODUCT-DEFINITION.md
└── README.md
```

## Architecture

- **MVVM Pattern** using CommunityToolkit.Mvvm
- **Dependency Injection** via Microsoft.Extensions.DependencyInjection
- **Local SQLite Storage** via Entity Framework Core
- **Desktop-first, keyboard-friendly** design

## Data Storage

All data is stored locally in SQLite at:
```
%LOCALAPPDATA%\Praxis\praxis.db
```

Your data stays on your machine. No cloud sync, no telemetry, no external dependencies.

## License

Proprietary - © 2026 Prickly Cactus Software
