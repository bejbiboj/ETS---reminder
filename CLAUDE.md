# CLAUDE.md - Project Guidelines
# CLAUDE.md - Project Guidelines

## Project Overview

**ETS Reminder** is a Windows desktop application built with WPF (.NET 8) that helps users track and manage daily ETS (Employee Time Sheet) reports. The application runs in the system tray and provides notifications to remind users to fill in their daily reports.

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
```

## Project Structure

```
ETS reminder/
├── App.xaml                 # Application resources and theme brushes (DynamicResource)
├── App.xaml.cs              # System tray, timer, notification logic, icon generation
├── AppSettings.cs           # Persistent settings with JSON serialization
├── ThemeManager.cs          # Dark/light theme switching (swaps brush resources)
├── ReportWindow.xaml        # Daily report entry window with find bar
├── ReportWindow.xaml.cs     # Report window code-behind
├── LogViewerWindow.xaml     # Main window: report viewer with menu bar, search, month nav
├── LogViewerWindow.xaml.cs  # Log viewer code-behind
├── ReportLogItem.cs         # Data record for log viewer list binding
├── ReportStorage.cs         # File-based report persistence (save/load/search/draft)
├── SettingsWindow.xaml      # Settings UI (dark mode, timezone, reminder time, auto-save)
├── SettingsWindow.xaml.cs   # Settings window code-behind
└── ETS reminder.csproj      # Project file

Root files:
├── CreateDesktopShortcut.vbs    # Creates desktop shortcut with icon
├── CLAUDE.md                    # This file
└── .github/
    └── copilot-instructions.md  # Copilot rules
```

## Features

### Core Features
- **System Tray Application** — Runs in background with orange "ETS" icon
- **Daily Report Entry** — Text editor with find bar (Ctrl+F), word/character count
- **Report Log Viewer** — Main window with menu bar, browse reports by month
- **Toast Notifications** — Configurable reminder time on weekdays
- **Smart Reminders** — Only notifies if report not yet submitted
- **Spell Checking** — Built-in spell check in text boxes
- **Dark Mode** — Toggle between light and dark theme (applies instantly)
- **Configurable Timezone** — All system timezones available
- **Auto-save Drafts** — Recovers unsaved work

### Log Viewer Features
- **Menu Bar** — File (Fill Report, Open Folder, Exit), Reports (Search, Copy/Delete Month), Tools (Settings)
- **Month Navigation** — Left panel shows all months with reports
- **Double-click to Edit** — Opens report editor on double-click
- **Copy/Edit/Delete** — Action buttons appear when selecting a log entry
- **Copy Month** — Copy all reports for a month to clipboard
- **Delete Month** — Delete all reports for a month
- **Search** — Debounced search across all reports with keyword highlighting
- **Horizontal Scroll** — Shift + mouse wheel scrolls horizontally
- **Quick Add** — Add entries for holidays, sick leave, WFH

### Settings
- **Dark mode** — On/Off toggle, applies instantly without restart
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

## Coding Standards

### General C# Guidelines

- Use **file-scoped namespaces** (`namespace ETS_reminder;`)
- Enable **nullable reference types** (`<Nullable>enable</Nullable>`)
- Use **implicit usings** where appropriate
- Prefer **pattern matching** (`is not`, `is { }`, etc.)
- Use **target-typed new** expressions (`new()` when type is clear)
- Use **collection expressions** (`[]` for empty lists)
- Use **raw string literals** for multi-line strings
- Use **using aliases** to resolve ambiguities (`using Application = System.Windows.Application;`)

### Naming Conventions

- **PascalCase** for public members, types, and methods
- **_camelCase** with underscore prefix for private fields
- **camelCase** for local variables and parameters
- **Timezone-neutral names** for date parameters (e.g., `date`, `reportDate` — not `cestDate`)

### WPF/XAML Guidelines

- Use **code-behind** for event handlers and simple logic
- Keep XAML readable with proper indentation
- Define reusable styles in `Window.Resources` or `Application.Resources`
- Use **DynamicResource** for all theme-dependent colors
- Use **data binding** with `{Binding}` syntax for list items
- Use **DataTemplate.Triggers** for conditional visibility
- Enable **SpellCheck.IsEnabled="True"** on TextBox controls

### Theming

- All theme colors defined as `SolidColorBrush` resources in `App.xaml`
- `ThemeManager.ApplyTheme(bool)` swaps brushes at runtime
- Windows use `Background="{DynamicResource WindowBackground}"` etc.
- Menu dropdown items use `ItemContainerStyle` with `Foreground="Black"` for readability
- Orange accent `#E67E22` works in both light and dark modes

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
- Drafts: `draft_yyyy-MM-dd.tmp` (auto-deleted after save)
- Settings: `%APPDATA%/ETS Reminder/settings.json`

## Code Patterns

### Timezone Handling (Configurable)

```csharp
// Dynamic timezone from settings
public static TimeZoneInfo AppTimeZone => AppSettings.Instance.GetTimeZone();
var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, App.AppTimeZone);

// Get display name for current timezone
var tzName = AppSettings.GetTimeZoneAbbreviation(App.AppTimeZone, DateTime.UtcNow);
```

### Theme-Aware Resources

```xml
<!-- In App.xaml (defaults) -->
<SolidColorBrush x:Key="WindowBackground" Color="#FFF8F0"/>

<!-- In any Window -->
Background="{DynamicResource WindowBackground}"

<!-- ThemeManager swaps at runtime -->
resources["WindowBackground"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
```

### Event Handlers with Tag Binding

```csharp
private void Button_Click(object sender, RoutedEventArgs e)
{
    if (sender is not Button btn || btn.Tag is not DateTime date)
        return;
    // Handle click...
}
```

### Window with Custom Icon

```csharp
public SomeWindow()
{
    InitializeComponent();
    Icon = App.GetWindowIcon();  // Sets orange ETS icon
}
```

## Key Components

| Window | Purpose | Opens From |
|--------|---------|------------|
| LogViewerWindow | Main window: view/manage all reports | Startup, tray icon |
| ReportWindow | Create/edit daily report | Tray icon, toast, menu, double-click |
| SettingsWindow | Configure app settings | Menu bar → Tools → Settings |

| Class | Purpose |
|-------|---------|
| App | Main app, tray icon, timer, notifications, icon generation |
| AppSettings | Singleton settings with JSON persistence |
| ThemeManager | Static class to swap light/dark theme brushes |
| ReportStorage | Static class for file I/O (reports, drafts, search) |
| ReportLogItem | Record for ListView data binding |

## Important Notes

- App uses `ShutdownMode="OnExplicitShutdown"` — runs until explicitly closed
- Toast notifications require Windows 10/11
- GDI handles properly freed via `DestroyIcon` and `DeleteObject` P/Invoke
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
