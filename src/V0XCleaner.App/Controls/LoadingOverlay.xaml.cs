using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App.Controls;

public partial class LoadingOverlay : UserControl
{
    // Anneau de 196 px de diamètre tracé (200 - 2*2 de marge) avec un trait de 4 : circonférence exprimée en épaisseurs de trait.
    private const double CircumferenceInStrokes = Math.PI * 196 / 4;

    public LoadingOverlay()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ScanProgress oldValue)
        {
            oldValue.PropertyChanged -= OnProgressChanged;
        }

        if (e.NewValue is ScanProgress newValue)
        {
            newValue.PropertyChanged += OnProgressChanged;
            Refresh(newValue);
        }
    }

    private void OnProgressChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is ScanProgress progress && e.PropertyName is nameof(ScanProgress.Progress) or nameof(ScanProgress.IsActive))
        {
            Dispatcher.Invoke(() => Refresh(progress));
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
                ArcRotation.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(-90, 270, TimeSpan.FromSeconds(1.2))
                {
                    RepeatBehavior = RepeatBehavior.Forever
                });
            }
        }
        else
        {
            ArcRotation.BeginAnimation(RotateTransform.AngleProperty, null);
            ArcRotation.Angle = -90;
            var percent = Math.Clamp(progress.Progress, 0, 100);
            Arc.StrokeDashArray = [percent / 100 * CircumferenceInStrokes, CircumferenceInStrokes];
            PercentText.Text = ((int)percent).ToString();
            PercentPanel.Visibility = Visibility.Visible;
        }

        if (!progress.IsActive)
        {
            ArcRotation.BeginAnimation(RotateTransform.AngleProperty, null);
        }
    }
}
