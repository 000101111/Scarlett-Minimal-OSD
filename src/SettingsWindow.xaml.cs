using System;
using System.Windows;
using System.Windows.Input;

namespace ScarlettMonitorOSD;

public partial class SettingsWindow : Window
{
    private readonly MainWindow _mainWindow;
    private readonly AppSettings _settings;
    private bool _initializing = true;

    public SettingsWindow(MainWindow mainWindow, AppSettings settings)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _settings = settings;

        OpacitySlider.Value = _settings.BackgroundOpacity * 100.0;
        OpacityValueText.Text = $"{OpacitySlider.Value:0}%";

        DurationSlider.Value = _settings.DisplayDurationSeconds;
        DurationValueText.Text = $"{DurationSlider.Value:0.0} 秒";

        StepSlider.Value = _settings.VolumeStepDb;
        StepValueText.Text = $"{StepSlider.Value:0.0} dB";

        MaxGainSlider.Value = _settings.MaxGainLimit;
        MaxGainValueText.Text = $"{MaxGainSlider.Value:0.0} dB";

        AutoLaunchFc2Check.IsChecked = _settings.AutoLaunchFc2;
        KillFc2OnExitCheck.IsChecked = _settings.KillFc2OnExit;

        _initializing = false;

        _mainWindow.SetPreviewMode(true);
    }

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityValueText == null) return;
        OpacityValueText.Text = $"{e.NewValue:0}%";
        if (!_initializing)
        {
            _settings.BackgroundOpacity = e.NewValue / 100.0;
            _mainWindow.ApplyOpacity(_settings.BackgroundOpacity);
            _mainWindow.SetPreviewMode(true);
        }
    }

    private void DurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DurationValueText == null) return;
        DurationValueText.Text = $"{e.NewValue:0.0} 秒";
        if (!_initializing)
        {
            _settings.DisplayDurationSeconds = e.NewValue;
        }
    }

    private void StepSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (StepValueText == null) return;
        StepValueText.Text = $"{e.NewValue:0.0} dB";
        if (!_initializing)
        {
            _settings.VolumeStepDb = e.NewValue;
        }
    }

    private void MaxGainSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxGainValueText == null) return;
        MaxGainValueText.Text = $"{e.NewValue:0.0} dB";
        if (!_initializing)
        {
            _settings.MaxGainLimit = e.NewValue;
        }
    }

    private void TestOsdButton_Click(object sender, RoutedEventArgs e)
    {
        _mainWindow.SetPreviewMode(false);
        _mainWindow.TriggerOSD(TimeSpan.FromSeconds(_settings.DisplayDurationSeconds));
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        CommitSettings();
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CommitSettings();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        CommitSettings();
        _mainWindow.SetPreviewMode(false);
    }

    private void CommitSettings()
    {
        _settings.BackgroundOpacity = OpacitySlider.Value / 100.0;
        _settings.DisplayDurationSeconds = DurationSlider.Value;
        _settings.VolumeStepDb = StepSlider.Value;
        _settings.MaxGainLimit = MaxGainSlider.Value;
        _settings.AutoLaunchFc2 = AutoLaunchFc2Check.IsChecked ?? true;
        _settings.KillFc2OnExit = KillFc2OnExitCheck.IsChecked ?? false;

        _settings.Save();
        _mainWindow.ApplySettings(_settings);
    }
}
