# eGPU Config Switcher

A premium, lightweight Windows system tray utility built with **C# .NET 8 (WPF & Windows Forms)** to automatically manage, swap, and protect game configuration files for eGPU setups.

---

## Features

* **Active GPU Detection**: Queries active display devices at startup via WMI. Displays a high-visibility badge next to the title:
  - **eGPU Mode (Green)**: Connected to external graphics.
  - **iGPU Mode (Red)**: Running on integrated graphics.
* **Smart Profile Swapping**:
  - **Startup Swap**: Replaces game config files with their GPU-specific profile (`.eGPU` or `.iGPU`) depending on the detected hardware mode before locking.
  - **Add File (Case 1)**: Newly added configurations automatically write base profiles to both `.eGPU` and `.iGPU` locations.
  - **Freeze after Unfrozen (Case 2)**: Re-locking a previously unfrozen card captures the edits and updates the active GPU profile.
* **Passive File Locks**: Uses C# file sharing streams to make configurations read-only to external applications while maintaining read compatibility for games. Consumes exactly 0% CPU and under 1 KB of RAM per lock.
* **Sleek Dark UI**: Modern dark-themed, borderless window with drop shadows, gradient buttons, hover-reactive visual indicators, and a custom vector-drawn GPU tray icon.
* **User-Level Setup**: Seamless installation to Local AppData avoiding administrative UAC prompts.

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

## Installation

1. Run the generated `eGPUConfigSwitcherSetup.exe` installer.
2. Select whether to create a Desktop shortcut.
3. Once completed, the switcher will run automatically.
4. The utility registers itself in your Windows Startup directory (`shell:startup`) with a `--silent` flag to run hidden directly in the tray on boot.
