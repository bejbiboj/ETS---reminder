On Error Resume Next
On Error Resume Next

Set fso = CreateObject("Scripting.FileSystemObject")
Set WshShell = CreateObject("WScript.Shell")

' Resolve paths relative to this script's location
scriptDir = fso.GetParentFolderName(WScript.ScriptFullName)
exePath = fso.BuildPath(scriptDir, "ETS reminder\bin\Release\net8.0-windows10.0.17763.0\ETS reminder.exe")

' Fall back to Debug build if Release does not exist
If Not fso.FileExists(exePath) Then
    exePath = fso.BuildPath(scriptDir, "ETS reminder\bin\Debug\net8.0-windows10.0.17763.0\ETS reminder.exe")
End If

If Not fso.FileExists(exePath) Then
    WScript.Echo "Error: Could not find ETS reminder.exe." & vbCrLf & _
                 "Please build the project first (Release or Debug)."
    WScript.Quit 1
End If

exeDir = fso.GetParentFolderName(exePath)
desktopPath = WshShell.SpecialFolders("Desktop")
shortcutPath = fso.BuildPath(desktopPath, "ETS Reminder.lnk")

' Warn if shortcut already exists
If fso.FileExists(shortcutPath) Then
    result = MsgBox("A shortcut already exists on the desktop. Overwrite?", vbYesNo + vbQuestion, "ETS Reminder")
    If result = vbNo Then
        WScript.Quit 0
    End If
End If

Set shortcut = WshShell.CreateShortcut(shortcutPath)
shortcut.TargetPath = exePath
shortcut.WorkingDirectory = exeDir
shortcut.IconLocation = exePath & ",0"
shortcut.Description = "ETS Daily Report Reminder"
shortcut.Save()

If Err.Number <> 0 Then
    WScript.Echo "Error creating shortcut: " & Err.Description
    WScript.Quit 1
End If

WScript.Echo "Desktop shortcut created!" & vbCrLf & "Target: " & exePath
