using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;

namespace eGPUConfigSwitcher
{
    /// <summary>
    /// The entry point class for the application. Sets AppUserModelID, registers startup shortcut,
    /// manages the system tray icon life cycle, and handles command-line arguments.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Gets a value indicating whether the application is currently exiting.
        /// </summary>
        public static bool IsExiting { get; private set; }

        /// <summary>
        /// The Windows Forms NotifyIcon instance displayed in the system tray.
        /// </summary>
        private static NotifyIcon? _trayIcon;

        /// <summary>
        /// The active instance of the MainWindow.
        /// </summary>
        private static MainWindow? _mainWindow;

        /// <summary>
        /// External Win32 API to explicitly set the process AppUserModelID for taskbar groupings.
        /// </summary>
        /// <param name="AppID">The unique Application User Model ID.</param>
        /// <returns>Zero on success.</returns>
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        /// <summary>
        /// Application entry point.
        /// </summary>
        /// <param name="args">The command line arguments passed to the application.</param>
        [STAThread]
        public static void Main(string[] args)
        {
            // 0. Check for CLI arguments
            foreach (string arg in args)
            {
                if (string.Equals(arg, "--test", StringComparison.OrdinalIgnoreCase))
                {
                    RunDiagnosticsTests();
                    return;
                }
            }

            // 1. Set AppUserModelID so taskbar links are properly grouped and styled
            try
            {
                SetCurrentProcessExplicitAppUserModelID("Brian.eGPUConfigSwitcher");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to set AppUserModelID: " + ex.Message);
            }

            // 3. Register Startup Shortcut (defaults to enabled on Windows log-on)
            ConfigManager.EnableStartupShortcut();

            var app = new System.Windows.Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 5. Initialize the main window (but don't show it yet)
            _mainWindow = new MainWindow();

            // 6. Initialize System Tray NotifyIcon and its ContextMenuStrip
            var contextMenu = new ContextMenuStrip();
            var closeItem = new ToolStripMenuItem("Close");
            closeItem.Click += (sender, e) => ExitApplication();
            contextMenu.Items.Add(closeItem);

            _trayIcon = new NotifyIcon
            {
                Icon = IconManager.GetTrayIcon(),
                Text = "eGPU Config Switcher",
                ContextMenuStrip = contextMenu,
                Visible = true
            };

            // Left-click opens/toggles the window
            _trayIcon.MouseClick += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ShowMainWindow();
                }
            };

            // 7. Handle silent startup vs regular startup
            bool silent = false;
            foreach (string arg in args)
            {
                if (string.Equals(arg, "--silent", StringComparison.OrdinalIgnoreCase))
                {
                    silent = true;
                    break;
                }
            }

            if (!silent)
            {
                ShowMainWindow();
            }

            // 8. Run the application event pump
            app.Run();

            // 9. Cleanup on exit
            Cleanup();
        }

        /// <summary>
        /// Displays the MainWindow and restores it to focus.
        /// </summary>
        public static void ShowMainWindow()
        {
            if (_mainWindow != null)
            {
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.Show();
                    _mainWindow.WindowState = WindowState.Normal;
                    _mainWindow.Activate();
                });
            }
        }

        /// <summary>
        /// Cleanly shuts down the WPF application process.
        /// </summary>
        public static void ExitApplication()
        {
            IsExiting = true;
            Cleanup();
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                System.Windows.Application.Current.Shutdown();
            });
        }

        /// <summary>
        /// Releases all allocated resources, closes tray notifications, and releases active locks.
        /// </summary>
        private static void Cleanup()
        {
            FileLockManager.UnlockAll();
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
            if (_mainWindow != null)
            {
                _mainWindow.Close();
                _mainWindow = null;
            }
        }

        /// <summary>
        /// Runs automated diagnostics checks simulating config file profile copying and startup swapping rules.
        /// </summary>
        private static void RunDiagnosticsTests()
        {
            Console.WriteLine("=== Starting eGPU Config Switcher Test Suite ===");

            // 1. Detect GPU
            var gpuType = GpuDetector.DetectGpuType();
            Console.WriteLine($"[TEST] Detected GPU: {gpuType}");

            // Create temporary test file
            string tempDir = Path.Combine(Path.GetTempPath(), "eGPUTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string mainConfigPath = Path.Combine(tempDir, "game_settings.ini");
            File.WriteAllText(mainConfigPath, "Version 1.0\nGPUSetting=Default");
            Console.WriteLine($"[TEST] Created main config file at: {mainConfigPath}");

            // --- Step 1: Adding config file for the first time ---
            Console.WriteLine("[TEST] Simulating profile creation on add...");
            try
            {
                File.Copy(mainConfigPath, mainConfigPath + ".eGPU", true);
                File.Copy(mainConfigPath, mainConfigPath + ".iGPU", true);
                Console.WriteLine("[TEST] Profiles copied to both .eGPU and .iGPU locations");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST] Step 1 Failed: {ex.Message}");
            }

            bool step1Success = File.Exists(mainConfigPath + ".eGPU") && 
                                File.Exists(mainConfigPath + ".iGPU") &&
                                File.ReadAllText(mainConfigPath + ".eGPU") == "Version 1.0\nGPUSetting=Default";
            Console.WriteLine($"[TEST] Step 1 Verification: {(step1Success ? "PASSED" : "FAILED")}");

            // --- Step 2: Freezing after unfrozen ---
            Console.WriteLine("[TEST] Simulating profile saving on freeze...");
            File.WriteAllText(mainConfigPath, "Version 1.0\nGPUSetting=UserEdited");
            Console.WriteLine("[TEST] Main config file modified while unfrozen.");

            string activeGpuPath = mainConfigPath + "." + gpuType.ToString();
            try
            {
                File.Copy(mainConfigPath, activeGpuPath, true);
                Console.WriteLine($"[TEST] Copied current config to active GPU path: {activeGpuPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST] Step 2 Failed: {ex.Message}");
            }

            bool step2Success = File.ReadAllText(activeGpuPath) == "Version 1.0\nGPUSetting=UserEdited";
            string inactiveGpu = gpuType == GpuType.iGPU ? "eGPU" : "iGPU";
            string inactiveGpuPath = mainConfigPath + "." + inactiveGpu;
            bool step2InactiveVerify = File.ReadAllText(inactiveGpuPath) == "Version 1.0\nGPUSetting=Default";
            Console.WriteLine($"[TEST] Active Profile Verification: {(step2Success ? "PASSED" : "FAILED")}");
            Console.WriteLine($"[TEST] Inactive Profile Verification: {(step2InactiveVerify ? "PASSED" : "FAILED")}");

            // --- STARTUP SWAP: Simulating startup swap ---
            Console.WriteLine("[TEST] Simulating Startup Swap...");
            File.WriteAllText(activeGpuPath, "Version 1.0\nGPUSetting=StartupApplied");
            
            if (File.Exists(activeGpuPath))
            {
                File.Copy(activeGpuPath, mainConfigPath, true);
                Console.WriteLine($"[TEST] Startup Swap: Overwrote main config with {activeGpuPath}");
            }

            bool startupSwapSuccess = File.ReadAllText(mainConfigPath) == "Version 1.0\nGPUSetting=StartupApplied";
            Console.WriteLine($"[TEST] Startup Swap Verification: {(startupSwapSuccess ? "PASSED" : "FAILED")}");

            try
            {
                Directory.Delete(tempDir, true);
                Console.WriteLine("[TEST] Cleaned up temporary test directory.");
            }
            catch { }

            if (step1Success && step2Success && step2InactiveVerify && startupSwapSuccess)
            {
                Console.WriteLine("=== Test Suite Result: ALL PASSED ===");
            }
            else
            {
                Console.WriteLine("=== Test Suite Result: SOME TESTS FAILED ===");
            }
        }
    }
}
