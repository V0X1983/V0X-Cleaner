using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using V0XCleaner.App.WinUI.ViewModels;

namespace V0XCleaner.App.WinUI.Controls;

public sealed partial class LoadingOverlay : UserControl
{
    // Anneau de 196 px de diamètre tracé (200 - 2*2 de marge) avec un trait de 4 : circonférence exprimée en épaisseurs de trait.
    private const double CircumferenceInStrokes = Math.PI * 196 / 4;

    public LoadingOverlay()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (args.NewValue is ScanProgress newValue)
        {
            newValue.PropertyChanged += OnProgressChanged;
            Refresh(newValue);
        }
    }

    private void OnProgressChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is ScanProgress progress && e.PropertyName is nameof(ScanProgress.Progress) or nameof(ScanProgress.IsActive))
        {
            DispatcherQueue.TryEnqueue(() => Refresh(progress));
        }
    }

    private void Refresh(ScanProgress progress)
    {
        if (progress.Progress < 0)
        {
            PercentPanel.Visibility = Visibility.Collapsed;
            Arc.StrokeDashArray = [CircumferenceInStrokes * 0.25, CircumferenceInStrokes];
            if (progress.IsActive)
            {
                SpinStoryboard.Begin();
            }
        }
        else
        {
            SpinStoryboard.Stop();
            ArcRotation.Angle = -90;
            var percent = Math.Clamp(progress.Progress, 0, 100);
            Arc.StrokeDashArray = [percent / 100 * CircumferenceInStrokes, CircumferenceInStrokes];
            PercentText.Text = ((int)percent).ToString();
            PercentPanel.Visibility = Visibility.Visible;
        }

        if (!progress.IsActive)
        {
            SpinStoryboard.Stop();
        }
    }
}
