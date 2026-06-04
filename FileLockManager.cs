using System;
using System.Collections.Generic;
using System.IO;

namespace eGPUConfigSwitcher
{
    /// <summary>
    /// Static class responsible for locking and unlocking configuration files
    /// to make them read-only to external processes while allowing the switcher to write to them.
    /// </summary>
    public static class FileLockManager
    {
        /// <summary>
        /// Dictionary tracking active file streams holding locks on disk files.
        /// </summary>
        private static readonly Dictionary<string, FileStream> _activeLocks = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Synchronization root object for thread-safe lock management.
        /// </summary>
        private static readonly object _lockObject = new();

        /// <summary>
        /// Appends a message to the diagnostics debug log in AppData.
        /// </summary>
        /// <param name="message">The text message to log.</param>
        public static void Log(string message)
        {
            try
            {
                string appDataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "eGPUConfigSwitcher"
                );
                if (!Directory.Exists(appDataDir))
                {
                    Directory.CreateDirectory(appDataDir);
                }
                string logPath = Path.Combine(appDataDir, "debug.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        /// <summary>
        /// Acquires a read-only stream lock on a file, blocking external write operations.
        /// </summary>
        /// <param name="path">The full file system path to lock.</param>
        public static void LockFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            lock (_lockObject)
            {
                Log($"Attempting to lock: {path}");
                if (_activeLocks.ContainsKey(path))
                {
                    Log($"File already locked in dictionary: {path}");
                    return;
                }

                try
                {
                    if (File.Exists(path))
                    {
                        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                        _activeLocks[path] = stream;
                        Log($"Successfully locked: {path}");
                    }
                    else
                    {
                        Log($"File does not exist on disk: {path}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Failed to lock file {path}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Releases the stream lock on a file, making it writable again by any process.
        /// </summary>
        /// <param name="path">The full file system path to unlock.</param>
        public static void UnlockFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            lock (_lockObject)
            {
                Log($"Attempting to unlock: {path}");
                if (_activeLocks.TryGetValue(path, out var stream))
                {
                    try
                    {
                        stream.Dispose();
                        Log($"Successfully disposed lock stream for: {path}");
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to dispose stream for {path}: {ex.Message}");
                    }
                    _activeLocks.Remove(path);
                }
                else
                {
                    Log($"File was not locked in dictionary: {path}");
                }
            }
        }

        /// <summary>
        /// Releases all active locks. Called during application shutdown.
        /// </summary>
        public static void UnlockAll()
        {
            lock (_lockObject)
            {
                Log("Unlocking all files");
                foreach (var kvp in _activeLocks)
                {
                    try
                    {
                        kvp.Value.Dispose();
                        Log($"Successfully disposed lock stream for: {kvp.Key}");
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to dispose stream for {kvp.Key} during UnlockAll: {ex.Message}");
                    }
                }
                _activeLocks.Clear();
            }
        }

        /// <summary>
        /// Reads file content using a share-compatible read stream.
        /// </summary>
        /// <param name="path">The full path of the file to read.</param>
        /// <returns>The text content of the file, or null if reading failed.</returns>
        public static string? ReadFileContent(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to read file content for {path}: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Overwrites file content by temporarily releasing the active sharing lock and re-establishing it.
        /// </summary>
        /// <param name="path">The file path to edit.</param>
        /// <param name="content">The new text content to write.</param>
        /// <returns>True if the write succeeded; otherwise false.</returns>
        public static bool WriteFileContent(string path, string content)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;

            lock (_lockObject)
            {
                Log($"Switcher attempting to write to: {path}");
                bool wasLocked = _activeLocks.TryGetValue(path, out var existingStream);
                if (wasLocked && existingStream != null)
                {
                    try
                    {
                        existingStream.Dispose();
                        Log($"Temporarily released lock stream to write: {path}");
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to temporarily dispose stream to write: {ex.Message}");
                    }
                    _activeLocks.Remove(path);
                }

                bool success = false;
                try
                {
                    File.WriteAllText(path, content);
                    success = true;
                    Log($"Successfully wrote text to: {path}");
                }
                catch (Exception ex)
                {
                    Log($"Failed to write text to {path}: {ex.Message}");
                }

                if (wasLocked)
                {
                    try
                    {
                        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                        _activeLocks[path] = stream;
                        Log($"Successfully re-locked after write: {path}");
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to restore lock after write {path}: {ex.Message}");
                    }
                }

                return success;
            }
        }
    }
}
