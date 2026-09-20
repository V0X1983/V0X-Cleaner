using CommunityToolkit.Mvvm.ComponentModel;

namespace V0XCleaner.App.ViewModels;

/// <summary>Ligne avec un interrupteur qui applique le changement immédiatement et revient en arrière s'il échoue.</summary>
public abstract partial class ToggleEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    protected ToggleEntryViewModel(bool isEnabled)
    {
        _isEnabled = isEnabled; // affectation directe : ne déclenche pas OnIsEnabledChanged
    }

    /// <summary>Applique le nouvel état ; retourne false en cas d'échec.</summary>
    protected abstract Task<bool> ApplyEnabledAsync(bool value);

    protected virtual string FailureMessage => "Échec (droits administrateur peut-être requis).";

    partial void OnIsEnabledChanged(bool value) => _ = ApplyAsync(value);

    private async Task ApplyAsync(bool value)
    {
        IsBusy = true;
        StatusMessage = null;

        try
        {
            if (!await ApplyEnabledAsync(value))
            {
                StatusMessage = FailureMessage;

                // Écriture directe du champ généré : repasser par le setter relancerait ApplyAsync.
#pragma warning disable MVVMTK0034
                _isEnabled = !value;
#pragma warning restore MVVMTK0034
                OnPropertyChanged(nameof(IsEnabled));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
