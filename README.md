# ETS Reminder
# ETS Reminder

A gamified Windows desktop app that helps teams track daily ETS (Employee Time Sheet) reports with coins, streaks, achievements, and a theme shop.

## Features

### Daily Reports
- Rich text editor for daily report entry
- Browse, search, and manage reports by month
- Quick-add buttons for Holiday, Sick Leave, Vacation, WFH
- Auto-save drafts so you never lose work

### Gamification
- **Coins** — Earn coins for filing reports on time, with streak bonuses
- **Streaks** — Consecutive weekday reports build your streak; leave days freeze it
- **Levels** — Progress from "Newcomer" (Lvl 1) to "ETS God" (Lvl 10)
- **Achievements** — 20 badges to unlock across Milestone, Streak, and Special categories
- **Weekly Summary** — Friday recap toast: reports filed, coins earned, streak status

### Theme Shop
Spend your coins on 10 color themes:

| Theme | Price |
|-------|-------|
| Classic Dark / Light | Free |
| Midnight Blue | 25 |
| Forest Green | 25 |
| Sunset Orange | 20 |
| Royal Purple | 30 |
| Cherry Red | 30 |
| Arctic | 35 |
| Cyberpunk | 40 |
| Golden | 50 |

### Achievements

| Category | Examples |
|----------|---------|
| **Milestone** | First Step (1 report), Centurion (100), Report Machine (500) |
| **Streak** | On Fire (5 days), Unstoppable (10), Unbreakable (100) |
| **Special** | Perfect Week, Night Owl, ETS God (Level 10) |

### Anti-Farming Rules
- Future dates are blocked
- Backdated reports save for records but earn 0 coins
- Holiday/Sick/Vacation: 0 coins, streak preserved (streak freeze)

### Smart Reminders
- Configurable reminder time and timezone
- Repeats every 5 minutes until you file your report
- Windows toast notifications with "Fill Report Now" button
- Friday weekly summary toast

### Modern UI
- Custom borderless window with themed title bar
- Dark and light mode with instant switching
- Profile indicator with avatar, name, and streak
- System tray with context menu

## Getting Started

### Prerequisites
- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build and Run

```powershell
# Clone the repo
git clone https://github.com/NemanjaGrokanic/ETS---reminder.git
cd "ETS---reminder"

# Build
dotnet build

# Run
dotnet run --project "ETS reminder\ETS reminder.csproj"
```

### Publish (Self-Contained)

Create a single executable that runs without .NET installed:

```powershell
dotnet publish "ETS reminder\ETS reminder.csproj" -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o .\publish
```

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+N` | New report |
| `Ctrl+F` | Search / Find |
| `Ctrl+S` | Save |
| `Ctrl+L` | View logs |
| `Escape` | Close / Clear |

## Data Storage

| Data | Location |
|------|----------|
| Reports | `Documents/ETS Reports/ETS_yyyy-MM-dd.txt` |
| Settings | `%APPDATA%/ETS Reminder/settings.json` |
| Profile | `%APPDATA%/ETS Reminder/profile.json` |

## Tech Stack

- **.NET 8** — Windows Desktop
- **WPF** — UI framework
- **Windows Forms** — System tray integration
- **Microsoft.Toolkit.Uwp.Notifications** — Toast notifications

## Screenshots

*Coming soon*

## License

This project is for internal team use.
