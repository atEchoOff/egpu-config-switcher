using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace eGPUConfigSwitcher
{
    /// <summary>
    /// Utility class to generate, render, and write multi-resolution custom application icons in memory and on disk.
    /// </summary>
    public static class IconManager
    {
        /// <summary>
        /// Retrieves the custom GPU chassis icon as a System.Drawing.Icon object for tray notification display.
        /// </summary>
        /// <returns>A System.Drawing.Icon representing the 16x16 tray icon.</returns>
        public static Icon GetTrayIcon()
        {
            using (Bitmap bmp = GetIconBitmap(16))
            {
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        /// <summary>
        /// Programmatically renders the premium GPU graphics card design onto a canvas of the specified size.
        /// </summary>
        /// <param name="size">The target square dimension in pixels (width and height).</param>
        /// <returns>A rendered Bitmap containing the custom GPU graphic.</returns>
        public static Bitmap GetIconBitmap(int size)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; // Use Anti-Aliasing for smooth circles
                g.Clear(Color.Transparent);

                // Define colors for a sleek, modern GPU card
                using (SolidBrush shroudBrush = new SolidBrush(Color.FromArgb(255, 30, 41, 59)))   // Slate 800 (#1e293b)
                {
                    using (SolidBrush fanBrush = new SolidBrush(Color.FromArgb(255, 6, 182, 212)))      // Cyan 500 (#06b6d4)
                    {
                        using (SolidBrush bracketBrush = new SolidBrush(Color.FromArgb(255, 148, 163, 184))) // Slate 400 (#94a3b8)
                        {
                            using (SolidBrush connBrush = new SolidBrush(Color.FromArgb(255, 251, 191, 36)))    // Gold (#fbbf24)
                            {
                                int shroudY = (int)(size * 0.3);
                                int shroudH = (int)(size * 0.38);
                                int shroudX = (int)(size * 0.16);
                                int shroudW = (int)(size * 0.7);

                                // 1. Draw PCIe Connector (Gold teeth at the bottom)
                                int connX = shroudX + (int)(size * 0.15);
                                int connY = shroudY + shroudH;
                                int connW = (int)(size * 0.45);
                                int connH = Math.Max(1, (int)(size * 0.05));
                                g.FillRectangle(connBrush, connX, connY, connW, connH);

                                // 2. Draw Shroud (GPU main body)
                                g.FillRectangle(shroudBrush, shroudX, shroudY, shroudW, shroudH);

                                // 3. Draw PCIe Bracket (Silver bracket on the left side)
                                int bracketX = shroudX - Math.Max(1, (int)(size * 0.04)) - 1;
                                int bracketY = shroudY - (int)(size * 0.08);
                                int bracketW = Math.Max(1, (int)(size * 0.04));
                                int bracketH = shroudH + (int)(size * 0.16);
                                g.FillRectangle(bracketBrush, bracketX, bracketY, bracketW, bracketH);

                                // 4. Draw Fans (two glowing cyan circles for all sizes)
                                float fanRadius = (float)(shroudH * 0.35);
                                float centerY = (float)(shroudY + shroudH / 2.0);
                                
                                float fan1X = (float)(shroudX + shroudW * 0.28 - fanRadius);
                                float fan2X = (float)(shroudX + shroudW * 0.72 - fanRadius);
                                float fanY = centerY - fanRadius;

                                g.FillEllipse(fanBrush, fan1X, fanY, fanRadius * 2, fanRadius * 2);
                                g.FillEllipse(fanBrush, fan2X, fanY, fanRadius * 2, fanRadius * 2);
                            }
                        }
                    }
                }
            }
            return bmp;
        }

        /// <summary>
        /// Creates a 48x48 PNG icon in AppData for Taskbar grouping usage.
        /// </summary>
        /// <param name="appDataDir">The path to the local application data directory.</param>
        /// <returns>The string path of the generated PNG file.</returns>
        public static string CreateAppIcon(string appDataDir)
        {
            string iconPath = Path.Combine(appDataDir, "icon_gpu.png");
            if (File.Exists(iconPath) && new FileInfo(iconPath).Length > 0) return iconPath;
            
            try
            {
                if (File.Exists(iconPath)) File.Delete(iconPath);

                using (Bitmap bmp = GetIconBitmap(48))
                {
                    bmp.Save(iconPath, ImageFormat.Png);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to create taskbar icon file: " + ex.Message);
            }
            return iconPath;
        }

        /// <summary>
        /// Generates a multi-resolution Windows .ico file containing 15 standard sizes for High-DPI matching.
        /// </summary>
        /// <param name="outputPath">The file path where the generated .ico file should be written.</param>
        public static void GenerateIcoFile(string outputPath)
        {
            int[] sizes = { 16, 20, 24, 28, 32, 36, 40, 48, 56, 64, 72, 80, 96, 128, 256 };
            byte[][] pngBytes = new byte[sizes.Length][];

            for (int i = 0; i < sizes.Length; i++)
            {
                using (Bitmap bmp = GetIconBitmap(sizes[i]))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png);
                        pngBytes[i] = ms.ToArray();
                    }
                }
            }

            using (FileStream fs = File.OpenWrite(outputPath))
            {
                fs.WriteByte(0); fs.WriteByte(0); // Reserved
                fs.WriteByte(1); fs.WriteByte(0); // Type: 1 = Icon
                
                fs.WriteByte((byte)(sizes.Length & 0xFF));
                fs.WriteByte((byte)((sizes.Length >> 8) & 0xFF));

                int offset = 6 + sizes.Length * 16; // Header (6) + DirEntries (16 per image)

                for (int i = 0; i < sizes.Length; i++)
                {
                    int size = sizes[i];
                    int len = pngBytes[i].Length;

                    fs.WriteByte((byte)(size == 256 ? 0 : size));
                    fs.WriteByte((byte)(size == 256 ? 0 : size));
                    fs.WriteByte(0);
                    fs.WriteByte(0);
                    fs.WriteByte(1); fs.WriteByte(0);
                    fs.WriteByte(32); fs.WriteByte(0);
                    fs.WriteByte((byte)(len & 0xFF));
                    fs.WriteByte((byte)((len >> 8) & 0xFF));
                    fs.WriteByte((byte)((len >> 16) & 0xFF));
                    fs.WriteByte((byte)((len >> 24) & 0xFF));
                    fs.WriteByte((byte)(offset & 0xFF));
                    fs.WriteByte((byte)((offset >> 8) & 0xFF));
                    fs.WriteByte((byte)((offset >> 16) & 0xFF));
                    fs.WriteByte((byte)((offset >> 24) & 0xFF));

                    offset += len;
                }

                for (int i = 0; i < sizes.Length; i++)
                {
                    fs.Write(pngBytes[i], 0, pngBytes[i].Length);
                }
            }
        }
    }
}
