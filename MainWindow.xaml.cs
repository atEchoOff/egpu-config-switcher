using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace eGPUConfigSwitcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml. Handles visual layout cards, 
    /// file additions/removals, card locking/unlocking, and active GPU badges.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Observable collection of configurations bound directly to the WPF card ListBox.
        /// </summary>
        public ObservableCollection<ConfigFile> Configs { get; } = new ObservableCollection<ConfigFile>();

        /// <summary>
        /// Local instance of application configuration settings.
        /// </summary>
        private AppSettings _settings;

        /// <summary>
        /// Gets the detected GPU hardware mode for this application execution session.
        /// </summary>
        public static GpuType ActiveGpuType { get; private set; }

        /// <summary>
        /// Initializes a new instance of the MainWindow class, detects active GPU mode,
        /// loads settings, and triggers configuration file startup swaps and locks.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Detect GPU type and update badge
            ActiveGpuType = GpuDetector.DetectGpuType();
            UpdateGpuBadge();

            // Set Window Icon dynamically from the multi-resolution AppData icon.ico to support High-DPI scaling
            try
            {
                string appDataDir = ConfigManager.GetAppDataDir();
                string icoPath = Path.Combine(appDataDir, "icon.ico");
                if (File.Exists(icoPath))
                {
                    this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(
                        new Uri(icoPath), 
                        System.Windows.Media.Imaging.BitmapCreateOptions.None, 
                        System.Windows.Media.Imaging.BitmapCacheOption.OnLoad
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to set window icon from ico: " + ex.Message);
            }
            
            // Load settings
            _settings = ConfigManager.LoadSettings();
            foreach (var cfg in _settings.Configs)
            {
                Configs.Add(cfg);
                if (cfg.IsFrozen)
                {
                    // Startup Swap: Replace main config file with corresponding GPU-specific file before locking
                    string gpuSpecificPath = cfg.Path + "." + ActiveGpuType.ToString();
                    if (File.Exists(gpuSpecificPath))
                    {
                        try
                        {
                            FileLockManager.Log($"Startup Swap: Overwriting {cfg.Path} with {gpuSpecificPath}");
                            File.Copy(gpuSpecificPath, cfg.Path, true);
                        }
                        catch (Exception ex)
                        {
                            FileLockManager.Log($"Startup Swap failed for {cfg.Path}: {ex.Message}");
                        }
                    }
                    else
                    {
                        FileLockManager.Log($"Startup Swap: {gpuSpecificPath} does not exist. Skipping replacement.");
                    }

                    FileLockManager.LockFile(cfg.Path);
                }
            }
            
            LstConfigs.ItemsSource = Configs;
            UpdatePlaceholderVisibility();

            // Refresh placeholder if list changes
            Configs.CollectionChanged += (s, e) => UpdatePlaceholderVisibility();
        }

        /// <summary>
        /// Updates the empty state text visibility when the config list becomes empty.
        /// </summary>
        private void UpdatePlaceholderVisibility()
        {
            TxtPlaceholder.Visibility = Configs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Sets up the color theme and text of the window header badge depending on the detected GPU type.
        /// </summary>
        private void UpdateGpuBadge()
        {
            if (ActiveGpuType == GpuType.eGPU)
            {
                GpuBadge.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#022c22"));
                TxtGpuBadge.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#34d399"));
                TxtGpuBadge.Text = "eGPU";
            }
            else
            {
                GpuBadge.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#450a0a"));
                TxtGpuBadge.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f87171"));
                TxtGpuBadge.Text = "iGPU";
            }
        }

        /// <summary>
        /// Handles custom window dragging by holding down the title bar.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        /// <summary>
        /// Minimizes/hides the main window to the system tray.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        /// <summary>
        /// Overrides window close behavior to hide the window instead of exiting the process, 
        /// unless an explicit application shutdown was requested.
        /// </summary>
        /// <param name="e">A CancelEventArgs containing event data.</param>
        protected override void OnClosing(CancelEventArgs e)
        {
            if (!Program.IsExiting)
            {
                e.Cancel = true;
                Hide();
            }
            base.OnClosing(e);
        }

        /// <summary>
        /// Opens a file selection dialog to add a configuration file.
        /// Performs Base profile initialization by copying the added file to both .eGPU and .iGPU suffixes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void BtnAddConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Config files (*.ini;*.cfg;*.txt;*.json)|*.ini;*.cfg;*.txt;*.json|All files (*.*)|*.*",
                Title = "Select Game Config File"
            };

            if (dialog.ShowDialog(this) == true)
            {
                string selectedPath = dialog.FileName;

                // Check for duplicate
                foreach (var cfg in Configs)
                {
                    if (string.Equals(cfg.Path, selectedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        System.Windows.MessageBox.Show(this, "This config file has already been added.", "Duplicate Config", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }

                var newConfig = new ConfigFile { Path = selectedPath, IsFrozen = true };

                // Case 1: First time added config is copied to both .eGPU and .iGPU locations
                try
                {
                    File.Copy(selectedPath, selectedPath + ".eGPU", true);
                    File.Copy(selectedPath, selectedPath + ".iGPU", true);
                    FileLockManager.Log($"Added config first time: Copied {selectedPath} to both .eGPU and .iGPU locations");
                }
                catch (Exception ex)
                {
                    FileLockManager.Log($"Failed to copy newly added config to .eGPU/.iGPU: {ex.Message}");
                }

                Configs.Add(newConfig);
                _settings.Configs.Add(newConfig);
                ConfigManager.SaveSettings(_settings);
                FileLockManager.LockFile(newConfig.Path);
            }
        }

        /// <summary>
        /// Removes a configuration file from tracking, releases its file lock, and updates settings.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is ConfigFile cfg)
            {
                FileLockManager.UnlockFile(cfg.Path);
                Configs.Remove(cfg);
                _settings.Configs.Remove(cfg);
                ConfigManager.SaveSettings(_settings);
            }
        }

        /// <summary>
        /// Freezes (locks) a configuration file card, copying the main file to the active GPU suffix profile.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void BtnCardFreeze_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is ConfigFile cfg)
            {
                cfg.IsFrozen = true;
                ConfigManager.SaveSettings(_settings);

                // Case 2: Save the user's custom changes to current GPU-specific location upon re-freezing after unfrozen
                string gpuSpecificPath = cfg.Path + "." + ActiveGpuType.ToString();
                try
                {
                    if (File.Exists(cfg.Path))
                    {
                        FileLockManager.Log($"User Freezing: Copying current config {cfg.Path} to {gpuSpecificPath}");
                        File.Copy(cfg.Path, gpuSpecificPath, true);
                    }
                }
                catch (Exception ex)
                {
                    FileLockManager.Log($"Failed to copy current config to {gpuSpecificPath} on freeze: {ex.Message}");
                }

                FileLockManager.LockFile(cfg.Path);
            }
        }

        /// <summary>
        /// Unfreezes (unlocks) a configuration file card, making it editable.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void BtnCardUnfreeze_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is ConfigFile cfg)
            {
                cfg.IsFrozen = false;
                ConfigManager.SaveSettings(_settings);
                FileLockManager.UnlockFile(cfg.Path);
            }
        }
    }
}
