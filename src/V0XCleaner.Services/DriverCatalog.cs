using System.Management;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.Services;

public sealed class DriverCatalog : IDriverCatalog
{
    public Task<IReadOnlyList<DriverInfo>> GetDriversAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<DriverInfo>>(() =>
        {
            var drivers = new List<DriverInfo>();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT DeviceName, Manufacturer, DriverVersion, DriverDate, DeviceClass FROM Win32_PnPSignedDriver WHERE DeviceName IS NOT NULL");
                foreach (ManagementBaseObject obj in searcher.Get())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    DateTime? date = null;
                    if (obj["DriverDate"] is string raw && raw.Length >= 8)
                    {
                        try
                        {
                            date = ManagementDateTimeConverter.ToDateTime(raw);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // Date invalide : laissée vide.
                        }
                    }

                    drivers.Add(new DriverInfo(
                        obj["DeviceName"]?.ToString() ?? string.Empty,
                        obj["Manufacturer"]?.ToString() ?? string.Empty,
                        obj["DriverVersion"]?.ToString() ?? string.Empty,
                        date,
                        obj["DeviceClass"]?.ToString() ?? string.Empty));
                }
            }
            catch (ManagementException)
            {
                // WMI indisponible : liste vide.
            }

            return drivers.OrderBy(d => d.DeviceClass).ThenBy(d => d.DeviceName).ToList();
        }, cancellationToken);
    }
}
