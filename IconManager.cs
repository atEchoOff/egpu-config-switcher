using System;
using System.Drawing;
using System.IO;

namespace eGPUConfigSwitcher
{
    /// <summary>
    /// Utility class to load custom application icons from the pre-generated icon.ico file.
    /// </summary>
    public static class IconManager
    {
        /// <summary>
        /// Retrieves the custom GPU chassis icon from the local icon.ico file for tray display.
        /// </summary>
        /// <returns>A System.Drawing.Icon representing the 16x16 tray icon.</returns>
        public static Icon GetTrayIcon()
        {
            try
            {
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(icoPath))
                {
                    return new Icon(icoPath, 16, 16);
                }
            }
            catch (Exception ex)
            {
                FileLockManager.Log($"Failed to load tray icon: {ex.Message}");
            }
            return SystemIcons.Application;
        }
    }
}
