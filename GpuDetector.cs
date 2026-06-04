using System;
using System.Management;

namespace eGPUConfigSwitcher
{
    /// <summary>
    /// Supported graphics card configuration types.
    /// </summary>
    public enum GpuType
    {
        /// <summary>
        /// Integrated graphics mode.
        /// </summary>
        iGPU,

        /// <summary>
        /// External graphics card mode.
        /// </summary>
        eGPU
    }

    /// <summary>
    /// Utility class to query WMI and check if the user is running on an external GPU (eGPU) or an integrated GPU (iGPU).
    /// </summary>
    public static class GpuDetector
    {
        /// <summary>
        /// Queries Windows active display devices to identify if an external GPU is connected and active.
        /// </summary>
        /// <returns>GpuType.eGPU if multiple adapters or discrete graphics are detected; otherwise GpuType.iGPU.</returns>
        public static GpuType DetectGpuType()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, PNPDeviceID FROM Win32_VideoController"))
                {
                    int gpuCount = 0;
                    bool hasDiscreteEgpu = false;

                    foreach (ManagementObject mo in searcher.Get())
                    {
                        gpuCount++;
                        string name = mo["Name"]?.ToString() ?? "";
                        string pnpId = mo["PNPDeviceID"]?.ToString() ?? "";

                        FileLockManager.Log($"Detected GPU: {name} (PNPDeviceID: {pnpId})");

                        // Identify discrete/external class GPU
                        if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("GeForce", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("RTX", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("GTX", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Quadro", StringComparison.OrdinalIgnoreCase) ||
                            (name.Contains("Radeon", StringComparison.OrdinalIgnoreCase) && 
                             !name.Contains("890M", StringComparison.OrdinalIgnoreCase) && 
                             !name.Contains("Graphics", StringComparison.OrdinalIgnoreCase) &&
                             !name.Contains("Radeon(TM) Graphics", StringComparison.OrdinalIgnoreCase)) ||
                            name.Contains("Intel(R) Arc(TM)", StringComparison.OrdinalIgnoreCase))
                        {
                            hasDiscreteEgpu = true;
                        }
                    }

                    // If more than 1 active GPU, or we explicitly found a discrete card
                    if (gpuCount > 1 || hasDiscreteEgpu)
                    {
                        FileLockManager.Log("GPU Detection result: eGPU");
                        return GpuType.eGPU;
                    }
                }
            }
            catch (Exception ex)
            {
                FileLockManager.Log($"Error detecting GPU: {ex.Message}");
            }

            FileLockManager.Log("GPU Detection result: iGPU");
            return GpuType.iGPU;
        }
    }
}
