using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.ViewModels;

public partial class DriveWiperViewModel : ObservableObject
{
    private readonly IDriveWiper _wiper;
    private readonly ILogger<DriveWiperViewModel> _logger;
    private CancellationTokenSource? _cts;

    public ObservableCollection<string> Drives { get; } = [];

    public IReadOnlyList<WipeMethodOption> MethodOptions { get; } =
    [
        new(WipeMethod.SinglePassZero, "1 passe (zéros)",
            "Rapide. Suffisant sur un SSD moderne et pour un usage courant."),
        new(WipeMethod.Dod3Pass, "DoD 3 passes",
            "Norme DoD 5220.22-M simplifiée (zéros, uns, aléatoire). Plus lent, pensé pour les disques mécaniques."),
        new(WipeMethod.Gutmann35Pass, "Gutmann 35 passes",
            "Hérité des disques magnétiques des années 1990. Extrêmement lent et sans bénéfice réel sur SSD : à réserver à un besoin spécifique.")
    ];

    [ObservableProperty]
    private string? _selectedDrive;

    [ObservableProperty]
    private WipeMethodOption _selectedMethod;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _statusMessage = "Choisissez un lecteur et une méthode, puis cliquez sur \"Effacer l'espace libre\".";

    [ObservableProperty]
    private string _passLabel = string.Empty;

    [ObservableProperty]
    private bool _canStartNow;

    public DriveWiperViewModel(IDriveWiper wiper, ILogger<DriveWiperViewModel> logger)
    {
        _wiper = wiper;
        _logger = logger;
        _selectedMethod = MethodOptions[0];

        // DriveInfo.IsReady peut bloquer plusieurs secondes sur un lecteur optique ou une carte
        // mémoire sans média : cette énumération ne doit jamais se faire de façon synchrone dans
        // le constructeur (appelé sur le thread UI lors de la résolution DI/navigation).
        _ = LoadDrivesAsync();
    }

    private async Task LoadDrivesAsync()
    {
        var drives = await Task.Run(() =>
            DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => d.RootDirectory.FullName).ToList());

        Drives.Clear();
        foreach (var drive in drives)
        {
            Drives.Add(drive);
        }

        SelectedDrive = Drives.FirstOrDefault();
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        if (string.IsNullOrEmpty(SelectedDrive))
        {
            return;
        }

        IsRunning = true;
        ProgressPercent = 0;
        PassLabel = string.Empty;
        StatusMessage = "Effacement en cours...";
        _cts = new CancellationTokenSource();

        var progress = new Progress<WipeProgress>(p =>
        {
            ProgressPercent = p.TotalBytesEstimate > 0
                ? Math.Min(100, p.BytesWritten * 100.0 / p.TotalBytesEstimate)
                : 0;
            PassLabel = $"Passe {p.CurrentPass}/{p.TotalPasses}";
        });

        try
        {
            await _wiper.WipeFreeSpaceAsync(SelectedDrive, SelectedMethod.Method, progress, _cts.Token);
            StatusMessage = "Effacement de l'espace libre terminé.";
            ProgressPercent = 100;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Effacement annulé.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur pendant l'effacement de {Drive}", SelectedDrive);
            StatusMessage = "Erreur pendant l'effacement.";
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Cancel() => _cts?.Cancel();

    private bool CanStart() => !IsRunning && !string.IsNullOrEmpty(SelectedDrive);

    partial void OnIsRunningChanged(bool value)
    {
        StartCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        CanStartNow = CanStart();
    }

    partial void OnSelectedDriveChanged(string? value) => CanStartNow = CanStart();
}
