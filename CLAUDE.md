# CLAUDE.md - Project Guidelines
# CLAUDE.md - Project Guidelines

## Project Overview

**ETS Reminder** is a Windows desktop application built with WPF (.NET 8) that helps users track and manage daily ETS (Employee Time Sheet) reports. The application runs in the system tray, provides notifications, and gamifies daily reporting with coins, streaks, achievements, and a theme shop.

## Technology Stack

- **.NET 8** (Windows Desktop)
- **WPF** (Windows Presentation Foundation) for UI
- **Windows Forms** for system tray integration
- **Microsoft.Toolkit.Uwp.Notifications** for toast notifications
- **Target Framework**: `net8.0-windows10.0.17763.0`

## Build Commands

```powershell
# Build the solution
dotnet build

# Build in Release mode
dotnet build -c Release

# Run the application
dotnet run --project "ETS reminder\ETS reminder.csproj"

# Clean build artifacts
dotnet clean

# Create desktop shortcut (after building)
cscript //nologo CreateDesktopShortcut.vbs

# Publish as single self-contained exe
dotnet publish "ETS reminder\ETS reminder.csproj" -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o .\publish
```

## Project Structure

```
ETS reminder/
├── App.xaml                      # Application resources and theme brushes (DynamicResource)
├── App.xaml.cs                   # System tray, timer, notifications, single-instance, achievement checks
├── AppSettings.cs                # Persistent settings with JSON serialization (incl. ActiveThemeId)
├── ThemeManager.cs               # Dark/light/custom theme switching (swaps brush resources)
├── ReportWindow.xaml             # Daily report entry window with rich text editor
├── ReportWindow.xaml.cs          # Report window code-behind
├── LogViewerWindow.xaml          # Main window: custom title bar, report viewer, menu, search, profile
├── LogViewerWindow.xaml.cs       # Log viewer code-behind
├── ReportLogItem.cs              # Data record for log viewer list binding
├── ReportStorage.cs              # File-based report persistence (save/load/search/draft/metadata)
├── SettingsWindow.xaml           # Settings UI (dark mode, timezone, reminder time, auto-save)
├── SettingsWindow.xaml.cs        # Settings window code-behind
├── UserProfile.cs                # User profile model (name, email, role, avatar, coins, achievements)
├── StatsEngine.cs                # Calculates streaks, coins, completion rate, weekly stats
├── AchievementManager.cs         # 20 achievements with auto-detection and toast notifications
├── ShopCatalog.cs                # Theme shop catalog (10 color themes with pricing)
├── ProfileSetupWindow.xaml       # First-launch profile setup / edit profile
├── ProfileSetupWindow.xaml.cs    # Profile setup code-behind
├── StatsDashboardWindow.xaml     # Stats dashboard: streaks, coins, levels, weekly, achievements
├── StatsDashboardWindow.xaml.cs  # Stats dashboard code-behind
├── ThemeShopWindow.xaml          # Theme shop: buy/equip color themes with coins
├── ThemeShopWindow.xaml.cs       # Theme shop code-behind
├── DiagnosticsWindow.xaml          # System diagnostics tool
├── DiagnosticsWindow.xaml.cs       # Diagnostics window code-behind
├── app.ico                       # Application icon
└── ETS reminder.csproj           # Project file

Root files:
├── CreateDesktopShortcut.vbs     # Creates desktop shortcut with icon
├── .gitignore                    # Git ignore rules
├── CLAUDE.md                     # This file
└── .github/
    └── copilot-instructions.md   # Copilot rules
```

## Features

### Core Features
- **System Tray Application** — Runs in background with orange "ETS" icon
- **Single Instance** — Mutex prevents multiple instances; shows message if already running
- **Daily Report Entry** — Rich text editor (bold, italic, underline, bullets, font size)
- **Report Log Viewer** — Custom chrome window with menu bar, browse reports by month
- **Custom Title Bar** — Borderless window with themed minimize/maximize/close buttons
- **Toast Notifications** — Configurable reminder time on weekdays
- **Smart Reminders** — Only notifies if report not yet submitted, repeats every 5 minutes
- **Dark Mode** — Toggle between light and dark theme (applies instantly)
- **Custom Themes** — 10 color themes purchasable with coins (Midnight Blue, Cyberpunk, etc.)
- **Configurable Timezone** — All system timezones available
- **Auto-save Drafts** — Recovers unsaved work

### Gamification System
- **Coins** — 1 coin per same-day report + streak bonuses; backdated reports earn 0
- **BonusCoins** — Extra coins that persist across stats recalculations
- **Streaks** — Consecutive weekday reports; leave days (Holiday/Sick/Vacation) freeze streak
- **Levels** — 10 levels from "Newcomer" (Lvl 1) to "ETS God" (Lvl 10) based on coins
- **Achievements** — 20 badges across Milestone, Streak, and Special categories
- **Weekly Summary** — Friday toast recap with reports filed, coins earned, streak status
- **Theme Shop** — Spend coins to unlock color themes

### Anti-Farming Rules
- **Future dates blocked** — Cannot create reports for future dates
- **Backdated reports** — Saved for records but earn 0 coins, no streak impact
- **Leave entries** (Holiday/Sick/Vacation) — 0 coins, streak preserved (streak freeze)
- **WFH** — Treated as regular report (full coins if same-day)
- **Metadata tracking** — `.meta` files record when reports were actually created

### Profile System
- **User Profile** — Name, email, team/role, avatar color (12 colors)
- **First-launch setup** — Required before app starts
- **Profile indicator** — Avatar + name + streak shown in LogViewer menu bar
- **Profile persistence** — `%APPDATA%/ETS Reminder/profile.json`
- **Auth provider field** — "Local" (ready for future Microsoft Entra ID integration)

### Achievements (20 total)

| Category | Achievements |
|----------|-------------|
| **Milestone** | First Step (1), Getting Started (10), Quarter Century (25), Half Century (50), Centurion (100), Double Century (200), Report Machine (500) |
| **Streak** | Warming Up (3), On Fire (5), Unstoppable (10), Legendary (20), Iron Will (50), Unbreakable (100) |
| **Special** | Perfect Week, Perfect Month, Early Bird (<10AM), Night Owl (>8PM), Coin Collector (100), Rich (500), Halfway There (Lvl 5), ETS God (Lvl 10) |

### Theme Shop (10 themes)

| Theme | Price | Accent |
|-------|-------|--------|
| Classic Dark | Free | Orange |
| Classic Light | Free | Orange |
| Midnight Blue | 25 | Blue |
| Forest Green | 25 | Green |
| Sunset Orange | 20 | Warm Orange |
| Royal Purple | 30 | Purple |
| Cherry Red | 30 | Red |
| Arctic | 35 | Cyan |
| Cyberpunk | 40 | Neon Pink |
| Golden | 50 | Gold |

### Log Viewer Features
- **Custom Title Bar** — Borderless window, themed controls, full drag support
- **Menu Bar** — File, Reports, Tools (Stats, Theme Shop, Edit Profile, Settings)
- **Month Navigation** — Left panel shows all months with reports
- **Double-click to Edit** — Opens report editor on double-click
- **Copy/Edit/Delete** — Action buttons appear when selecting a log entry
- **Copy Month** — Copy all reports for a month to clipboard
- **Delete Month** — Delete all reports for a month
- **Search** — Debounced search across all reports with keyword highlighting
- **Quick Add** — Holiday, Sick Leave, Vacation, WFH buttons
- **Profile Indicator** — Avatar, name, and streak fire emoji in menu bar

### Settings
- **Dark mode** — On/Off toggle, syncs with active theme
- **Auto-save draft** — On/Off with configurable interval (seconds)
- **Show Log Viewer on startup** — On/Off
- **Timezone** — Searchable dropdown with all system timezones
- **Reminder start time** — Hour and minute (5-min increments)

### Keyboard Shortcuts
| Shortcut | Window | Action |
|----------|--------|--------|
| `Ctrl+N` | Log Viewer | Fill ETS Report |
| `Ctrl+F` | Log Viewer / Report | Search / Find bar |
| `Ctrl+S` | Log Viewer / Report | Save entry / Save report |
| `Ctrl+L` | Report | View Logs |
| `Escape` | Any | Close find bar / Clear search / Close window |

### Notification Logic
- Starts at **configurable time** in **configurable timezone** (weekdays only)
- Repeats every **5 minutes** if no report submitted
- **Checks disk** for existing reports before notifying
- Stops when report is saved
- **Friday weekly summary** — Toast with week recap at reminder time
- **Achievement toasts** — Pop up when new badges are unlocked

## Coding Standards

### General C# Guidelines

- Use **file-scoped namespaces** (`namespace ETS_reminder;`)
- Enable **nullable reference types** (`<Nullable>enable</Nullable>`)
- Use **implicit usings** where appropriate
- Prefer **pattern matching** (`is not`, `is { }`, etc.)
- Use **target-typed new** expressions (`new()` when type is clear)
- Use **collection expressions** (`[]` for empty lists)
- Use **raw string literals** for multi-line strings
- Use **using aliases** to resolve WPF/WinForms ambiguities:
  ```csharp
  using Color = System.Windows.Media.Color;
  using ColorConverter = System.Windows.Media.ColorConverter;
  using MessageBox = System.Windows.MessageBox;
  ```

### Naming Conventions

- **PascalCase** for public members, types, and methods
- **_camelCase** with underscore prefix for private fields
- **camelCase** for local variables and parameters
- **Timezone-neutral names** for date parameters (e.g., `date`, `reportDate`)

### WPF/XAML Guidelines

- Use **code-behind** for event handlers and simple logic
- Keep XAML readable with proper indentation
- Define reusable styles in `Window.Resources` or `Application.Resources`
- Use **DynamicResource** for all theme-dependent colors
- Use **data binding** with `{Binding}` syntax for list items
- Use **DataTemplate.Triggers** for conditional visibility
- Enable **SpellCheck.IsEnabled="True"** on TextBox controls
- Use **WindowStyle="None" + AllowsTransparency="True"** for custom chrome windows
- Use **FontFamily="Segoe UI Emoji"** for emoji TextBlocks in WPF

### Theming

- All theme colors defined as `SolidColorBrush` resources in `App.xaml`
- `ThemeManager.ApplyTheme(bool)` swaps brushes at runtime
- Custom themes use `ApplyCustomDarkTheme(ShopTheme)` with auto-generated variants
- Theme colors are derived from 3 base colors: dark1, dark2, accent
- `Lighten()` and `Darken()` helpers generate border/hover colors
- Active theme ID persisted in `AppSettings.ActiveThemeId`
- Classic Light = `DarkMode=false`, all others = `DarkMode=true`

### Coin Economy

- **Earned coins** — Calculated from report data by `StatsEngine` (cannot be manipulated)
- **BonusCoins** — Supplemental coins, persisted separately
- **TotalCoins** = earned + BonusCoins (recalculated each time stats run)
- **SpendCoins()** deducts from BonusCoins first (so earned coins can't be "spent" then recalculated back)
- Streak bonuses: +1 (3 days), +2 (5 days), +3 (10 days), +5 (20 days)

### UI Styling

| Element | Light Mode | Dark Mode | Usage |
|---------|-----------|-----------|-------|
| Primary Accent | `#E67E22` | `#E67E22` | Headers, buttons, accents |
| Hover | `#D35400` | `#D35400` | Button hover states |
| Window BG | `#FFF8F0` | `#1E1E1E` | Window backgrounds |
| Panel BG | `#FDEBD0` | `#2D2D30` | Menu bar, search bar, panels |
| Side Panel | `#FDF2E9` | `#252528` | Month list panel |
| Border | `#E0C5A0` | `#3E3E42` | Borders |
| Text | `#2C3E50` | `#E0E0E0` | Primary text |
| Sub Text | `#7F8C8D` | `#9E9E9E` | Secondary text, status |

### File Storage

- Reports stored in: `Documents/ETS Reports/`
- File naming: `ETS_yyyy-MM-dd.txt`
- Metadata: `ETS_yyyy-MM-dd.meta` (creation date for backdating detection)
- Drafts: `draft_yyyy-MM-dd.tmp` (auto-deleted after save)
- Settings: `%APPDATA%/ETS Reminder/settings.json`
- Profile: `%APPDATA%/ETS Reminder/profile.json`

## Key Components

| Window | Purpose | Opens From |
|--------|---------|------------|
| LogViewerWindow | Main window: view/manage all reports | Startup, tray icon |
| ReportWindow | Create/edit daily report | Tray icon, toast, menu, double-click |
| SettingsWindow | Configure app settings | Menu → Tools → Settings |
| StatsDashboardWindow | Stats: streaks, coins, levels, weekly, achievements | Menu → Tools, profile click, tray |
| ThemeShopWindow | Buy/equip color themes | Menu → Tools → Theme Shop, tray |
| ProfileSetupWindow | Create/edit user profile | First launch, Menu → Tools → Edit Profile |
| DiagnosticsWindow | System diagnostics | Internal |

| Class | Purpose |
|-------|---------|
| App | Main app, tray icon, timer, notifications, icon generation, single-instance |
| AppSettings | Singleton settings with JSON persistence |
| ThemeManager | Dark/light/custom theme switching |
| ReportStorage | File I/O for reports, drafts, search, metadata |
| UserProfile | Singleton profile with JSON persistence (coins, achievements, themes) |
| StatsEngine | Calculates streaks, coins, completion rate, weekly stats from report files |
| AchievementManager | Defines 20 achievements, checks unlock conditions, shows toasts |
| ShopCatalog | Defines 10 color themes with pricing |
| ReportLogItem | Record for ListView data binding |

## Important Notes

- App uses `ShutdownMode="OnExplicitShutdown"` — runs until explicitly closed
- Single-instance enforced via named `Mutex` ("ETS_Reminder_SingleInstance")
- Toast notifications require Windows 10/11
- GDI handles properly freed via `DestroyIcon` and `DeleteObject` P/Invoke
- LogViewerWindow uses `AllowsTransparency="True"` for custom chrome (no Windows border)
- Git repo: https://github.com/bejbiboj/ETS---reminder.git
- Auto-save timer stopped in `ReportWindow.OnClosed` to prevent leaks
- `using` aliases required in files that mix WPF and WinForms types
- Use `ShowDialog()` for edit operations, `Show()` for viewing windows
- Icon is generated programmatically at runtime (no embedded resource)
- Windows icon cache can cause stale icons - restart Explorer to refresh

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.Toolkit.Uwp.Notifications | 7.1.3 | Toast notifications |

## Deployment

1. Build in Release mode: `dotnet build -c Release`
2. Run app once to generate icon file
3. Run `CreateDesktopShortcut.vbs` to create desktop shortcut
4. (Optional) Copy shortcut to Startup folder for auto-start

## Testing Checklist

- [ ] Report creation and saving
- [ ] Report editing from log viewer  
- [ ] Report deletion (single and month)
- [ ] Copy to clipboard (single entry and month)
- [ ] System tray icon and context menu
- [ ] Toast notifications at 12:00 PM CEST
- [ ] Notification stops after report saved
- [ ] Spell checking in text boxes
- [ ] Shift + scroll for horizontal scrolling
- [ ] Window icons display correctly
- [ ] Desktop shortcut with correct icon
- [ ] Report deletion (single and month)
- [ ] Copy to clipboard functionality
- [ ] System tray icon and menu
- [ ] Toast notifications at scheduled times
- [ ] Application startup and shutdown
- [ ] Application startup and shutdown
