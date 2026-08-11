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

    private readonly CustomEffectsManager _custom =
        new();

    private readonly ToggleSwitch _effectsEnabled;

    private readonly ComboBox _profileBox;

    private readonly CheckBox _petalsEnabled;
    private readonly CheckBox _leavesEnabled;
    private readonly CheckBox _snowEnabled;
    private readonly CheckBox _rainEnabled;
    private readonly CheckBox _firefliesEnabled;
    private readonly CheckBox _starsEnabled;
    private readonly CheckBox _nyanCatEnabled;

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

    private readonly ListBox _customEffectList;

    private readonly TextBlock _statusText;

    private bool _loading;

    public EffectsSettingsView()
    {
        AvaloniaXamlLoader.Load(this);

        _effectsEnabled =
            Get<ToggleSwitch>(
                "EffectsEnabled");

        _profileBox =
            Get<ComboBox>(
                "ProfileBox");

        _petalsEnabled =
            Get<CheckBox>(
                "PetalsEnabled");

        _leavesEnabled =
            Get<CheckBox>(
                "LeavesEnabled");

        _snowEnabled =
            Get<CheckBox>(
                "SnowEnabled");

        _rainEnabled =
            Get<CheckBox>(
                "RainEnabled");

        _firefliesEnabled =
            Get<CheckBox>(
                "FirefliesEnabled");

        _starsEnabled =
            Get<CheckBox>(
                "StarsEnabled");

        _nyanCatEnabled =
            Get<CheckBox>(
                "NyanCatEnabled");

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

        _customEffectList =
            Get<ListBox>(
                "CustomEffectList");

        _statusText =
            Get<TextBlock>(
                "StatusText");

        _profileBox.ItemsSource =
            EffectProfiles.Names;

        LoadSettings();

        RefreshCustomEffects();
    }

    private T Get<T>(
        string name)
        where T : Control
    {
        return this.FindControl<T>(name)
            ?? throw new InvalidOperationException(
                $"{name} was not found.");
    }

    private void LoadSettings()
    {
        _loading = true;

        var s =
            _effects.Settings;

        _effectsEnabled.IsChecked =
            s.Enabled;

        _profileBox.SelectedItem =
            s.Profile;

        _petalsEnabled.IsChecked =
            s.PetalsEnabled;

        _leavesEnabled.IsChecked =
            s.LeavesEnabled;

        _snowEnabled.IsChecked =
            s.SnowEnabled;

        _rainEnabled.IsChecked =
            s.RainEnabled;

        _firefliesEnabled.IsChecked =
            s.FirefliesEnabled;

        _starsEnabled.IsChecked =
            s.StarsEnabled;

        _nyanCatEnabled.IsChecked =
            s.NyanCatEnabled;

        _densitySlider.Value =
            s.Density;

        _speedSlider.Value =
            s.Speed;

        _opacitySlider.Value =
            s.Opacity;

        _sizeSlider.Value =
            s.Size;

        _windSlider.Value =
            s.Wind;

        _particleLimitSlider.Value =
            s.MaxParticles;

        UpdateLabels();

        _loading = false;
    }

    private void ApplyControls()
    {
        if (_loading)
            return;

        _effects.Update(
            s =>
            {
                s.Enabled =
                    _effectsEnabled.IsChecked == true;

                s.Profile =
                    _profileBox.SelectedItem?
                        .ToString()
                    ?? "Custom";

                s.PetalsEnabled =
                    _petalsEnabled.IsChecked == true;

                s.LeavesEnabled =
                    _leavesEnabled.IsChecked == true;

                s.SnowEnabled =
                    _snowEnabled.IsChecked == true;

                s.RainEnabled =
                    _rainEnabled.IsChecked == true;

                s.FirefliesEnabled =
                    _firefliesEnabled.IsChecked == true;

                s.StarsEnabled =
                    _starsEnabled.IsChecked == true;

                s.NyanCatEnabled =
                    _nyanCatEnabled.IsChecked == true;

                s.Density =
                    (int)Math.Round(
                        _densitySlider.Value);

                s.Speed =
                    _speedSlider.Value;

                s.Opacity =
                    _opacitySlider.Value;

                s.Size =
                    _sizeSlider.Value;

                s.Wind =
                    _windSlider.Value;

                s.MaxParticles =
                    (int)Math.Round(
                        _particleLimitSlider.Value);
            });

        UpdateLabels();
    }

    private void UpdateLabels()
    {
        _densityText.Text =
            $"{_densitySlider.Value:0}";

        _speedText.Text =
            $"{_speedSlider.Value:0.0}x";

        _opacityText.Text =
            $"{_opacitySlider.Value * 100:0}%";

        _sizeText.Text =
            $"{_sizeSlider.Value:0}px";

        _windText.Text =
            $"{_windSlider.Value:0.0}";

        _particleLimitText.Text =
            $"{_particleLimitSlider.Value:0}";
    }

    private void SettingChanged(
        object? sender,
        RoutedEventArgs e)
    {
        if (_loading)
            return;

        SetCustomProfile();

        ApplyControls();
    }

    private void SliderChanged(
        object? sender,
        Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_loading)
            return;

        SetCustomProfile();

        ApplyControls();
    }

    private void SetCustomProfile()
    {
        _loading = true;

        _profileBox.SelectedItem =
            "Custom";

        _loading = false;
    }

    private void ProfileChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (_loading)
            return;

        var profile =
            _profileBox.SelectedItem?
                .ToString();

        if (string.IsNullOrWhiteSpace(profile))
            return;

        _effects.Update(
            settings =>
                EffectProfiles.Apply(
                    settings,
                    profile));

        LoadSettings();

        _effects.Preview();
    }

    private void Preview_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ApplyControls();

        _effects.Preview();

        _statusText.Text =
            "Previewing effects.";
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
            files.FirstOrDefault()?
                .TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            var imported =
                await _packages.ImportAsync(
                    path);

            RefreshCustomEffects();

            _statusText.Text =
                $"Imported {imported.Manifest.Name}.";
        }
        catch (Exception ex)
        {
            _statusText.Text =
                $"Import failed: {ex.Message}";
        }
    }

    private void RefreshCustomEffects()
    {
        _customEffectList.ItemsSource =
            _custom.GetInstalled();
    }

    private InstalledEffect? SelectedEffect =>
        _customEffectList.SelectedItem
            as InstalledEffect;

    private async void ToggleCustomEffect_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var effect =
            SelectedEffect;

        if (effect is null)
            return;

        var enabled =
            !_custom.IsEnabled(effect);

        _custom.SetEnabled(
            effect,
            enabled);

        await _effects.SaveAsync();

        _statusText.Text =
            enabled
                ? $"Enabled {effect.Name}."
                : $"Disabled {effect.Name}.";
    }

    private void PreviewCustomEffect_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var effect =
            SelectedEffect;

        if (effect is null)
            return;

        _effects.Preview();

        _statusText.Text =
            $"Previewing {effect.Name}.";
    }

    private async void ExportCustomEffect_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var effect =
            SelectedEffect;

        if (effect is null)
            return;

        var top =
            TopLevel.GetTopLevel(this);

        if (top is null)
            return;

        var file =
            await top.StorageProvider
                .SaveFilePickerAsync(
                    new FilePickerSaveOptions
                    {
                        Title =
                            "Export Effect",

                        SuggestedFileName =
                            effect.Name +
                            ".axfx",

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

        var output =
            file?.TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(output))
            return;

        if (!output.EndsWith(
                ".axfx",
                StringComparison.OrdinalIgnoreCase))
        {
            output += ".axfx";
        }

        try
        {
            await _packages.ExportAsync(
                effect.Directory,
                output);

            _statusText.Text =
                $"Exported {effect.Name}.";
        }
        catch (Exception ex)
        {
            _statusText.Text =
                $"Export failed: {ex.Message}";
        }
    }

    private void OpenCustomEffectFolder_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var effect =
            SelectedEffect;

        if (effect is null)
            return;

        _custom.OpenFolder(effect);
    }

    private void RemoveCustomEffect_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var effect =
            SelectedEffect;

        if (effect is null)
            return;

        _custom.Remove(effect);

        RefreshCustomEffects();

        _statusText.Text =
            $"Removed {effect.Name}.";
    }
}