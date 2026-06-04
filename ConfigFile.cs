using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace eGPUConfigSwitcher
{
    /// <summary>
    /// Represents a game configuration file managed by the application.
    /// Supports property change notification to update WPF UI bindings.
    /// </summary>
    public class ConfigFile : INotifyPropertyChanged
    {
        /// <summary>
        /// The full system file path of the configuration file.
        /// </summary>
        private string _path = "";

        /// <summary>
        /// Indicates whether the configuration file is currently locked/frozen.
        /// </summary>
        private bool _isFrozen = true;

        /// <summary>
        /// Gets or sets the full system file path of the configuration file.
        /// </summary>
        public string Path
        {
            get => _path;
            set
            {
                if (_path != value)
                {
                    _path = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FileName));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the configuration file is currently locked/frozen.
        /// </summary>
        public bool IsFrozen
        {
            get => _isFrozen;
            set
            {
                if (_isFrozen != value)
                {
                    _isFrozen = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the filename component of the configuration path.
        /// </summary>
        public string FileName => System.IO.Path.GetFileName(Path);

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
