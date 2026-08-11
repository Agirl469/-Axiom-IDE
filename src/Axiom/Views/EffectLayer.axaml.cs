using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Axiom.Effects;

namespace Axiom.Views;

public partial class EffectLayer : UserControl
{
    private readonly Canvas _canvas;

    private readonly ParticleEngine _particles;

    private readonly NyanCatService _nyan;

    public EffectLayer()
    {
        AvaloniaXamlLoader.Load(this);

        _canvas =
            this.FindControl<Canvas>(
                "ParticleCanvas")
            ?? throw new InvalidOperationException(
                "ParticleCanvas was not found.");

        _particles =
            new ParticleEngine(
                _canvas);

        _nyan =
            new NyanCatService(
                _canvas);

        EffectService.Current.SettingsChanged +=
            SettingsChanged;

        EffectService.Current.PreviewRequested +=
            PreviewRequested;

        AttachedToVisualTree +=
            (_, _) =>
            {
                _particles.Start();
                _nyan.Start();
            };

        DetachedFromVisualTree +=
            (_, _) =>
            {
                _particles.Stop();
                _nyan.Stop();
            };
    }

    private void SettingsChanged(
        object? sender,
        EventArgs e)
    {
        if (!EffectService.Current.Settings.Enabled)
            _particles.Clear();
    }

    private void PreviewRequested(
        object? sender,
        EventArgs e)
    {
        _particles.Burst(30);

        if (EffectService.Current.Settings.NyanCatEnabled)
            _nyan.Preview();
    }
}