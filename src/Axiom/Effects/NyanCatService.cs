using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Axiom.Effects;

public sealed class NyanCatService
{
    private readonly Canvas _canvas;

    private readonly DispatcherTimer _checkTimer;

    private readonly Random _random =
        new();

    private DateTime _nextAppearance;

    private TextBlock? _cat;

    public NyanCatService(
        Canvas canvas)
    {
        _canvas = canvas;

        _checkTimer =
            new DispatcherTimer
            {
                Interval =
                    TimeSpan.FromSeconds(15)
            };

        _checkTimer.Tick +=
            Check;

        ScheduleNext();
    }

    public void Start()
    {
        _checkTimer.Start();
    }

    public void Stop()
    {
        _checkTimer.Stop();

        if (_cat is not null)
        {
            _canvas.Children.Remove(_cat);
            _cat = null;
        }
    }

    public void Preview()
    {
        Spawn();
    }

    private void Check(
        object? sender,
        EventArgs e)
    {
        var settings =
            EffectService.Current.Settings;

        if (!settings.Enabled ||
            !settings.NyanCatEnabled)
        {
            return;
        }

        if (DateTime.UtcNow <
            _nextAppearance)
        {
            return;
        }

        Spawn();

        ScheduleNext();
    }

    private void ScheduleNext()
    {
        var settings =
            EffectService.Current.Settings;

        var min =
            Math.Max(
                0.1,
                settings.NyanMinMinutes);

        var max =
            Math.Max(
                min,
                settings.NyanMaxMinutes);

        var minutes =
            min +
            _random.NextDouble() *
            (max - min);

        _nextAppearance =
            DateTime.UtcNow
                .AddMinutes(minutes);
    }

    private void Spawn()
    {
        if (_cat is not null)
            return;

        if (_canvas.Bounds.Width <= 0)
            return;

        var cat =
            new TextBlock
            {
                Text = "🌈  🐱",
                FontSize = 28,
                IsHitTestVisible = false,
                Opacity = 0.95
            };

        _cat = cat;

        var x = -100.0;

        var maxX =
            _canvas.Bounds.Width + 120;

        var y =
            Math.Max(
                20,
                _canvas.Bounds.Height *
                (
                    0.2 +
                    _random.NextDouble() *
                    0.55
                ));

        Canvas.SetLeft(cat, x);
        Canvas.SetTop(cat, y);

        _canvas.Children.Add(cat);

        var timer =
            new DispatcherTimer
            {
                Interval =
                    TimeSpan.FromMilliseconds(16)
            };

        timer.Tick +=
            (_, _) =>
            {
                x += 5.5;

                Canvas.SetLeft(
                    cat,
                    x);

                if (x <= maxX)
                    return;

                timer.Stop();

                _canvas.Children.Remove(cat);

                if (ReferenceEquals(
                        _cat,
                        cat))
                {
                    _cat = null;
                }
            };

        timer.Start();
    }
}