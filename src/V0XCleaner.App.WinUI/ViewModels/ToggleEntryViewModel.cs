using CommunityToolkit.Mvvm.ComponentModel;

namespace V0XCleaner.App.WinUI.ViewModels;

/// <summary>Ligne avec un interrupteur qui applique le changement immédiatement et revient en arrière s'il échoue.</summary>
public abstract partial class ToggleEntryViewModel : ObservableObject
{
    // Garde le retour arrière (en cas d'échec) de re-déclencher un nouvel appel à ApplyEnabledAsync :
    // remplace le contournement WPF (écrire le champ généré _isEnabled directement) qui n'est plus
    // possible avec la syntaxe [ObservableProperty] sur propriété partielle (le champ est généré,
    // pas déclaré par nous).
    private bool _suppressApply;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    protected ToggleEntryViewModel(bool isEnabled)
    {
        _suppressApply = true;
        IsEnabled = isEnabled;
        _suppressApply = false;
    }

    /// <summary>Applique le nouvel état ; retourne false en cas d'échec.</summary>
    protected abstract Task<bool> ApplyEnabledAsync(bool value);

    protected virtual string FailureMessage => "Échec (droits administrateur peut-être requis).";

    partial void OnIsEnabledChanged(bool value)
    {
        if (_suppressApply)
        {
            return;
        }

        _ = ApplyAsync(value);
    }

    private async Task ApplyAsync(bool value)
    {
        IsBusy = true;
        StatusMessage = null;

        try
        {
            if (!await ApplyEnabledAsync(value))
            {
                StatusMessage = FailureMessage;

                _suppressApply = true;
                IsEnabled = !value;
                _suppressApply = false;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
