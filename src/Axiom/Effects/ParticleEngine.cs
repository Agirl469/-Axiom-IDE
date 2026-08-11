using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Axiom.Effects;

internal sealed class ParticleEngine
{
    private readonly Canvas _canvas;

    private readonly List<Particle> _particles = [];

    private readonly Random _random = new();

    private readonly DispatcherTimer _timer;

    private DateTime _lastFrame =
        DateTime.UtcNow;

    private double _spawnAccumulator;

    private EffectSettings Settings =>
        EffectService.Current.Settings;

    public ParticleEngine(Canvas canvas)
    {
        _canvas = canvas;

        _timer = new DispatcherTimer
        {
            Interval =
                TimeSpan.FromMilliseconds(16)
        };

        _timer.Tick += Tick;
    }

    public void Start()
    {
        _lastFrame = DateTime.UtcNow;

        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();

        Clear();
    }

    public void Clear()
    {
        foreach (var particle in _particles)
            _canvas.Children.Remove(particle.Control);

        _particles.Clear();
    }

    public void Burst(int amount)
    {
        if (!Settings.Enabled)
            return;

        for (var i = 0;
             i < amount &&
             _particles.Count < Settings.MaxParticles;
             i++)
        {
            var kind =
                GetRandomEnabledKind();

            if (kind is null)
                return;

            Spawn(
                kind.Value,
                true);
        }
    }

    private void Tick(
        object? sender,
        EventArgs e)
    {
        var now =
            DateTime.UtcNow;

        var delta =
            Math.Clamp(
                (now - _lastFrame)
                    .TotalSeconds,
                0,
                0.05);

        _lastFrame = now;

        if (!Settings.Enabled)
        {
            Clear();
            return;
        }

        SpawnParticles(delta);

        UpdateParticles(delta);
    }

    private void SpawnParticles(
        double delta)
    {
        if (_canvas.Bounds.Width <= 0 ||
            _canvas.Bounds.Height <= 0)
        {
            return;
        }

        _spawnAccumulator +=
            delta *
            Settings.Density;

        while (_spawnAccumulator >= 1)
        {
            _spawnAccumulator--;

            if (_particles.Count >=
                Settings.MaxParticles)
            {
                break;
            }

            var kind =
                GetRandomEnabledKind();

            if (kind is null)
                break;

            Spawn(
                kind.Value,
                false);
        }
    }

    private ParticleKind? GetRandomEnabledKind()
    {
        var kinds =
            new List<ParticleKind>();

        if (Settings.PetalsEnabled)
            kinds.Add(ParticleKind.Petal);

        if (Settings.LeavesEnabled)
            kinds.Add(ParticleKind.Leaf);

        if (Settings.SnowEnabled)
            kinds.Add(ParticleKind.Snow);

        if (Settings.RainEnabled)
        {
            kinds.Add(ParticleKind.Rain);
            kinds.Add(ParticleKind.Rain);
            kinds.Add(ParticleKind.Rain);
        }

        if (Settings.FirefliesEnabled)
            kinds.Add(ParticleKind.Firefly);

        if (Settings.StarsEnabled)
            kinds.Add(ParticleKind.Star);

        if (kinds.Count == 0)
            return null;

        return kinds[
            _random.Next(kinds.Count)];
    }

    private void Spawn(
        ParticleKind kind,
        bool randomY)
    {
        var width =
            Math.Max(
                _canvas.Bounds.Width,
                1);

        var height =
            Math.Max(
                _canvas.Bounds.Height,
                1);

        var size =
            Settings.Size *
            RandomRange(
                0.65,
                1.35);

        var text =
            new TextBlock
            {
                Text =
                    GetGlyph(kind),

                FontSize =
                    size,

                Opacity =
                    Math.Clamp(
                        Settings.Opacity *
                        RandomRange(
                            0.7,
                            1),
                        0.05,
                        1),

                Foreground =
                    GetBrush(kind),

                IsHitTestVisible =
                    false,

                RenderTransformOrigin =
                    new RelativePoint(
                        0.5,
                        0.5,
                        RelativeUnit.Relative)
            };

        var rotation =
            new RotateTransform();

        text.RenderTransform =
            rotation;

        var x =
            RandomRange(
                -20,
                width + 20);

        var y =
            randomY
                ? RandomRange(
                    -20,
                    height)
                : RandomRange(
                    -80,
                    -10);

        var speed =
            GetSpeed(kind) *
            Settings.Speed;

        var particle =
            new Particle
            {
                Control = text,

                Kind = kind,

                X = x,
                Y = y,

                VelocityX =
                    RandomRange(
                        -6,
                        6),

                VelocityY =
                    speed,

                Rotation =
                    RandomRange(
                        0,
                        360),

                RotationSpeed =
                    kind == ParticleKind.Rain
                        ? 0
                        : RandomRange(
                            -60,
                            60),

                DriftPhase =
                    RandomRange(
                        0,
                        Math.PI * 2),

                DriftSpeed =
                    RandomRange(
                        0.5,
                        2),

                Lifetime =
                    RandomRange(
                        7,
                        18)
            };

        rotation.Angle =
            particle.Rotation;

        Canvas.SetLeft(
            text,
            particle.X);

        Canvas.SetTop(
            text,
            particle.Y);

        _canvas.Children.Add(text);

        _particles.Add(particle);
    }

    private void UpdateParticles(
        double delta)
    {
        var width =
            _canvas.Bounds.Width;

        var height =
            _canvas.Bounds.Height;

        for (var i =
                 _particles.Count - 1;
             i >= 0;
             i--)
        {
            var p =
                _particles[i];

            p.Age += delta;

            p.DriftPhase +=
                delta *
                p.DriftSpeed;

            var drift =
                Math.Sin(
                    p.DriftPhase) *
                GetDrift(p.Kind);

            var wind =
                Settings.Wind * 28;

            p.X +=
                (
                    p.VelocityX +
                    drift +
                    wind
                ) * delta;

            p.Y +=
                p.VelocityY *
                delta;

            p.Rotation +=
                p.RotationSpeed *
                delta;

            Canvas.SetLeft(
                p.Control,
                p.X);

            Canvas.SetTop(
                p.Control,
                p.Y);

            if (p.Control.RenderTransform
                is RotateTransform rotation)
            {
                rotation.Angle =
                    p.Rotation;
            }

            if (p.Kind ==
                ParticleKind.Firefly)
            {
                p.Control.Opacity =
                    Math.Clamp(
                        0.35 +
                        Math.Sin(
                            p.DriftPhase * 2) *
                        0.3,
                        0.1,
                        Settings.Opacity);
            }

            var remove =
                p.Age > p.Lifetime ||
                p.Y > height + 80 ||
                p.X < -120 ||
                p.X > width + 120;

            if (!remove)
                continue;

            _canvas.Children.Remove(
                p.Control);

            _particles.RemoveAt(i);
        }
    }

    private string GetGlyph(
        ParticleKind kind)
    {
        return kind switch
        {
            ParticleKind.Petal =>
                _random.Next(2) == 0
                    ? "❀"
                    : "✿",

            ParticleKind.Leaf =>
                _random.Next(2) == 0
                    ? "❧"
                    : "❦",

            ParticleKind.Snow =>
                "❄",

            ParticleKind.Rain =>
                "│",

            ParticleKind.Firefly =>
                "•",

            ParticleKind.Star =>
                _random.Next(2) == 0
                    ? "✦"
                    : "✧",

            _ =>
                "•"
        };
    }

    private IBrush GetBrush(
        ParticleKind kind)
    {
        var color =
            kind switch
            {
                ParticleKind.Petal =>
                    Pick(
                        "#F7A8C4",
                        "#E88DB3",
                        "#FFD0DF",
                        "#D77BA5"),

                ParticleKind.Leaf =>
                    Pick(
                        "#C68B59",
                        "#DDA15E",
                        "#BC6C25",
                        "#8A9A5B"),

                ParticleKind.Snow =>
                    Pick(
                        "#FFFFFF",
                        "#DCEBFF",
                        "#EEF6FF"),

                ParticleKind.Rain =>
                    Pick(
                        "#7EA7D8",
                        "#6F95C5",
                        "#A0BDE0"),

                ParticleKind.Firefly =>
                    Pick(
                        "#FFF59D",
                        "#F9F871",
                        "#FFE66D"),

                ParticleKind.Star =>
                    Pick(
                        "#FFFFFF",
                        "#EADFFF",
                        "#FFDCF1"),

                _ =>
                    "#FFFFFF"
            };

        return new SolidColorBrush(
            Color.Parse(color));
    }

    private double GetSpeed(
        ParticleKind kind)
    {
        return kind switch
        {
            ParticleKind.Petal =>
                RandomRange(22, 45),

            ParticleKind.Leaf =>
                RandomRange(28, 55),

            ParticleKind.Snow =>
                RandomRange(12, 30),

            ParticleKind.Rain =>
                RandomRange(180, 300),

            ParticleKind.Firefly =>
                RandomRange(3, 10),

            ParticleKind.Star =>
                RandomRange(5, 15),

            _ =>
                30
        };
    }

    private double GetDrift(
        ParticleKind kind)
    {
        return kind switch
        {
            ParticleKind.Rain =>
                0,

            ParticleKind.Snow =>
                12,

            ParticleKind.Firefly =>
                22,

            ParticleKind.Star =>
                8,

            _ =>
                16
        };
    }

    private string Pick(
        params string[] values)
    {
        return values[
            _random.Next(
                values.Length)];
    }

    private double RandomRange(
        double min,
        double max)
    {
        return min +
               _random.NextDouble() *
               (max - min);
    }
}