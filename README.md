# eGPU Config Switcher

A premium, lightweight Windows system tray utility built with **C# .NET 8 (WPF & Windows Forms)** to automatically manage, swap, and protect game configuration files for eGPU setups.

---

## Key Features

* **Active GPU Detection**: Queries active display devices at startup via WMI. Displays a high-visibility badge next to the title:
  - **eGPU Mode (Green)**: Connected to external graphics.
  - **iGPU Mode (Red)**: Running on integrated graphics.
* **Smart Profile Swapping**:
  - **Startup Swap**: Replaces game config files with their GPU-specific profile (`.eGPU` or `.iGPU`) depending on the detected hardware mode before locking.
  - **Add File (Case 1)**: Newly added configurations automatically write base profiles to both `.eGPU` and `.iGPU` locations.
  - **Freeze after Unfrozen (Case 2)**: Re-locking a previously unfrozen card captures the edits and updates the active GPU profile.
* **Passive File Locks**: Uses C# file sharing streams to make configurations read-only to external applications while maintaining read compatibility for games. Consumes exactly 0% CPU and under 1 KB of RAM per lock.
* **Sleek Dark UI**: Modern dark-themed, borderless window with drop shadows, gradient buttons, hover-reactive visual indicators, and a custom vector-drawn GPU tray icon.
* **User-Level Setup**: Seamless installation to Local AppData avoiding administrative UAC elevation requirements.

---

## Local Development & Running

To run or compile the switcher locally:

1. **Requirements**: Make sure you have the **.NET 8.0 SDK** installed.
2. **Run in Debug mode**:
   ```powershell
   dotnet run
   ```
3. **Run Diagnostic Tests**:
   To run the automated diagnostic verification suite (which validates Cases 1, 2, and startup swapping rules in a temporary folder):
   ```powershell
   dotnet run -- --test
   ```

---

## Directory & File Locations

* **Settings File**: `%APPDATA%\eGPUConfigSwitcher\settings.json` (Stores list of tracked files and their freeze states).
* **Diagnostics Log**: `%APPDATA%\eGPUConfigSwitcher\debug.log` (Tracks locking/swapping events).

---

## Building and Packaging

The project includes an automation script to build and compile the installation package using **Inno Setup 6**.

To compile the installer:
1. Open a PowerShell console.
2. Execute the build script (bypassing local script execution policies):
   ```powershell
   powershell -ExecutionPolicy Bypass -File build_installer.ps1
   ```

Upon completion, a single setup executable will be generated at:
```
Output\eGPUConfigSwitcherSetup.exe
```

---

## Usage & Operations

1. **System Tray**:
   - **Left-Click**: Toggles the main user interface window (Show/Hide).
   - **Right-Click**: Opens a context menu with **Close** to terminate the switcher process completely and release all locks.
2. **Closing the Window**: Clicking the "✕" button in the title bar minimizes the application back to the tray, keeping the background file locks active.
3. **Windows Startup**: The switcher automatically places a shortcut pointing to itself inside the Windows Startup folder (`shell:startup`) with a `--silent` flag to run minimized directly in the tray on system boot.
