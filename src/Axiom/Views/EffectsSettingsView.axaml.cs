using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Axiom.Effects;

namespace Axiom.Views;

public partial class EffectsSettingsView : UserControl
{
    private readonly EffectService _effects =
        EffectService.Current;
    private readonly EffectPackageService _packages =
    new();
    private readonly ToggleSwitch _effectsEnabled;
    private readonly ToggleSwitch _petalsEnabled;

    private readonly Slider _densitySlider;
    private readonly Slider _speedSlider;
    private readonly Slider _opacitySlider;
    private readonly Slider _sizeSlider;
    private readonly Slider _windSlider;
    private readonly Slider _particleLimitSlider;

    private readonly TextBlock _densityText;
    private readonly TextBlock _speedText;
    private readonly TextBlock _opacityText;
    private readonly TextBlock _sizeText;
    private readonly TextBlock _windText;
    private readonly TextBlock _particleLimitText;

    private readonly TextBlock _statusText;

    private bool _loading;

    public EffectsSettingsView()
    {
        AvaloniaXamlLoader.Load(this);

        _effectsEnabled =
            Get<ToggleSwitch>(
                "EffectsEnabled");

        _petalsEnabled =
            Get<ToggleSwitch>(
                "PetalsEnabled");

        _densitySlider =
            Get<Slider>(
                "DensitySlider");

        _speedSlider =
            Get<Slider>(
                "SpeedSlider");

        _opacitySlider =
            Get<Slider>(
                "OpacitySlider");

        _sizeSlider =
            Get<Slider>(
                "SizeSlider");

        _windSlider =
            Get<Slider>(
                "WindSlider");

        _particleLimitSlider =
            Get<Slider>(
                "ParticleLimitSlider");

        _densityText =
            Get<TextBlock>(
                "DensityText");

        _speedText =
            Get<TextBlock>(
                "SpeedText");

        _opacityText =
            Get<TextBlock>(
                "OpacityText");

        _sizeText =
            Get<TextBlock>(
                "SizeText");

        _windText =
            Get<TextBlock>(
                "WindText");

        _particleLimitText =
            Get<TextBlock>(
                "ParticleLimitText");

        _statusText =
            Get<TextBlock>(
                "StatusText");

        LoadSettings();
    }

    private T Get<T>(
        string name)
        where T : Control
    {
        return this.FindControl<T>(name)
            ?? throw new InvalidOperationException(
                $"{name} was not found.");
    }
    private async void ImportEffect_Click(
    object? sender,
    RoutedEventArgs e)
    {
        var top =
            TopLevel.GetTopLevel(this);

        if (top is null)
            return;

        var files =
            await top.StorageProvider
                .OpenFilePickerAsync(
                    new FilePickerOpenOptions
                    {
                        Title =
                            "Import Axiom Effect",

                        AllowMultiple =
                            false,

                        FileTypeFilter =
                        [
                            new FilePickerFileType(
                            "Axiom Effect")
                        {
                            Patterns =
                            [
                                "*.axfx"
                            ]
                        }
                        ]
                    });

        var path =
            files
                .FirstOrDefault()?
                .TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(
                path))
        {
            return;
        }

        try
        {
            _statusText.Text =
                "Checking package...";

            var imported =
                await _packages.ImportAsync(
                    path);

            _statusText.Text =
                $"Imported {imported.Manifest.Name}.";
        }
        catch (Exception ex)
        {
            _statusText.Text =
                $"Import failed: {ex.Message}";
        }
    }



    private async void ExportEffect_Click(
    object? sender,
    RoutedEventArgs e)
    {
        var top =
            TopLevel.GetTopLevel(this);

        if (top is null)
            return;

        var folders =
            await top.StorageProvider
                .OpenFolderPickerAsync(
                    new FolderPickerOpenOptions
                    {
                        Title =
                            "Choose Custom Effect Folder",

                        AllowMultiple =
                            false
                    });

        var effectFolder =
            folders
                .FirstOrDefault()?
                .TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(
                effectFolder))
        {
            return;
        }

        var destination =
            await top.StorageProvider
                .SaveFilePickerAsync(
                    new FilePickerSaveOptions
                    {
                        Title =
                            "Export Axiom Effect",

                        SuggestedFileName =
                            "effect.axfx",

                        FileTypeChoices =
                        [
                            new FilePickerFileType(
                            "Axiom Effect")
                        {
                            Patterns =
                            [
                                "*.axfx"
                            ]
                        }
                        ]
                    });

        var destinationPath =
            destination?
                .TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(
                destinationPath))
        {
            return;
        }

        if (!destinationPath.EndsWith(
                ".axfx",
                StringComparison.OrdinalIgnoreCase))
        {
            destinationPath +=
                ".axfx";
        }

        try
        {
            _statusText.Text =
                "Exporting...";

            await _packages.ExportAsync(
                effectFolder,
                destinationPath);

            _statusText.Text =
                "Effect exported.";
        }
        catch (Exception ex)
        {
            _statusText.Text =
                $"Export failed: {ex.Message}";
        }
    }

    private void LoadSettings()
    {
        _loading = true;

        var settings =
            _effects.Settings;

        _effectsEnabled.IsChecked =
            settings.Enabled;

        _petalsEnabled.IsChecked =
            settings.PetalsEnabled;

        _densitySlider.Value =
            settings.Density;

        _speedSlider.Value =
            settings.Speed;

        _opacitySlider.Value =
            settings.Opacity;

        _sizeSlider.Value =
            settings.Size;

        _windSlider.Value =
            settings.Wind;

        _particleLimitSlider.Value =
            settings.MaxParticles;

        UpdateLabels();

        _loading = false;
    }

    private void ApplyControls()
    {
        _effects.Update(
            settings =>
            {
                settings.Enabled =
                    _effectsEnabled.IsChecked == true;

                settings.PetalsEnabled =
                    _petalsEnabled.IsChecked == true;

                settings.Density =
                    (int)Math.Round(
                        _densitySlider.Value);

                settings.Speed =
                    _speedSlider.Value;

                settings.Opacity =
                    _opacitySlider.Value;

                settings.Size =
                    _sizeSlider.Value;

                settings.Wind =
                    _windSlider.Value;

                settings.MaxParticles =
                    (int)Math.Round(
                        _particleLimitSlider.Value);
            });

        UpdateLabels();
    }

    private void UpdateLabels()
    {
        _densityText.Text =
            Math.Round(
                _densitySlider.Value)
            .ToString();

        _speedText.Text =
            $"{_speedSlider.Value:0.0}x";

        _opacityText.Text =
            $"{_opacitySlider.Value * 100:0}%";

        _sizeText.Text =
            $"{_sizeSlider.Value:0}px";

        _windText.Text =
            _windSlider.Value.ToString(
                "0.0");

        _particleLimitText.Text =
            Math.Round(
                _particleLimitSlider.Value)
            .ToString();
    }

    private void SettingChanged(
     object? sender,
     RoutedEventArgs e)
    {
        if (_loading)
            return;

        ApplyControls();
    }

    private void SliderChanged(
        object? sender,
        Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_loading)
            return;

        ApplyControls();
    }

    private void Preview_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ApplyControls();

        _effects.Preview();

        _statusText.Text =
            "Previewing effect.";
    }

    private async void Save_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ApplyControls();

        await _effects.SaveAsync();

        _statusText.Text =
            "Effects saved.";
    }

    private async void Reset_Click(
        object? sender,
        RoutedEventArgs e)
    {
        await _effects.ResetAsync();

        LoadSettings();

        _statusText.Text =
            "Effects reset.";
    }
}