using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.ViewModels;

public sealed class DriverItemViewModel(DriverInfo info)
{
    public string DeviceName => info.DeviceName;

    public string Details => $"{info.Manufacturer} — v{info.Version}";

    public string DateText => info.Date is { } date ? date.ToString("dd/MM/yyyy") : "—";

    public string DeviceClass => info.DeviceClass;
}

public partial class DriversViewModel : ObservableObject
{
    private readonly IDriverCatalog _catalog;
    private readonly ILogger<DriversViewModel> _logger;

    public ObservableCollection<DriverItemViewModel> Drivers { get; } = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Cliquez sur \"Analyser\" pour lister les pilotes installés.";

    public DriversViewModel(IDriverCatalog catalog, ILogger<DriversViewModel> logger)
    {
        _catalog = catalog;
        _logger = logger;
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ScanAsync()
    {
        IsBusy = true;
        StatusMessage = "Lecture des pilotes installés...";

        try
        {
            var drivers = await _catalog.GetDriversAsync();
            Drivers.Clear();
            foreach (var driver in drivers)
            {
                Drivers.Add(new DriverItemViewModel(driver));
            }

            StatusMessage = $"{Drivers.Count} pilote(s) installé(s).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la lecture des pilotes.");
            StatusMessage = "Erreur lors de la lecture des pilotes.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenWindowsUpdate()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:windowsupdate") { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            _logger.LogWarning(ex, "Impossible d'ouvrir Windows Update.");
        }
    }

    private bool CanRun() => !IsBusy;

    partial void OnIsBusyChanged(bool value) => ScanCommand.NotifyCanExecuteChanged();
}
