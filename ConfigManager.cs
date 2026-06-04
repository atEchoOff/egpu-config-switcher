using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace eGPUConfigSwitcher
{
    /// <summary>
    /// Holds the persistent settings of the application.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets the list of configuration files managed by the switcher.
        /// </summary>
        public List<ConfigFile> Configs { get; set; } = new List<ConfigFile>();
    }

    /// <summary>
    /// Static manager to handle loading and saving configuration settings,
    /// and managing the Windows startup shortcut.
    /// </summary>
    public static class ConfigManager
    {
        /// <summary>
        /// The dedicated directory in Local AppData for switcher data.
        /// </summary>
        private static readonly string AppDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "eGPUConfigSwitcher"
        );

        /// <summary>
        /// The full path to the settings file.
        /// </summary>
        private static readonly string SettingsFile = Path.Combine(AppDataDir, "settings.json");

        /// <summary>
        /// Initializes the ConfigManager class and ensures the AppData folder exists.
        /// </summary>
        static ConfigManager()
        {
            if (!Directory.Exists(AppDataDir))
            {
                Directory.CreateDirectory(AppDataDir);
            }
        }

        /// <summary>
        /// Gets the path to the application data directory.
        /// </summary>
        /// <returns>The string path of the AppData directory.</returns>
        public static string GetAppDataDir() => AppDataDir;

        /// <summary>
        /// Loads the application settings from settings.json.
        /// </summary>
        /// <returns>An AppSettings object populated from disk, or a new empty instance if not found/corrupted.</returns>
        public static AppSettings LoadSettings()
        {
            if (!File.Exists(SettingsFile))
            {
                return new AppSettings();
            }

            try
            {
                string json = File.ReadAllText(SettingsFile);
                AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        /// <summary>
        /// Saves the application settings to settings.json.
        /// </summary>
        /// <param name="settings">The AppSettings object to serialize and save.</param>
        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to save settings: " + ex.Message);
            }
        }

        /// <summary>
        /// Registers a shortcut for the application in the Windows Startup folder.
        /// Configures it to launch with the --silent flag.
        /// </summary>
        public static void EnableStartupShortcut()
        {
            try
            {
                string startupPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Windows\Start Menu\Programs\Startup"
                );
                string shortcutPath = Path.Combine(startupPath, "eGPUConfigSwitcher.lnk");
                
                string? exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath))
                {
                    exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    if (exePath.EndsWith(".dll"))
                    {
                        exePath = Path.ChangeExtension(exePath, ".exe");
                    }
                }

                if (!string.IsNullOrEmpty(exePath) && 
                    !exePath.Contains("dotnet.exe") && 
                    File.Exists(exePath))
                {
                    Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                    if (shellType != null)
                    {
                        dynamic shell = Activator.CreateInstance(shellType)!;
                        var shortcut = shell.CreateShortcut(shortcutPath);
                        shortcut.TargetPath = exePath;
                        shortcut.Arguments = "--silent";
                        shortcut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        shortcut.Description = "eGPU Config Switcher";
                        shortcut.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to create startup shortcut: " + ex.Message);
            }
        }
    }
}
