using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Axiom.Effects;

internal sealed class ParticleEngine
{
    private readonly Canvas _canvas;
    private readonly List<Particle> _particles = new();
    private readonly Random _random = new();

    private readonly DispatcherTimer _timer;

    private DateTime _lastFrame =
        DateTime.UtcNow;

    private double _spawnAccumulator;

    private EffectSettings Settings =>
        EffectService.Current.Settings;

    public ParticleEngine(
        Canvas canvas)
    {
        _canvas = canvas;

        _timer =
            new DispatcherTimer
            {
                Interval =
                    TimeSpan.FromMilliseconds(16)
            };

        _timer.Tick += Tick;
    }

    public void Start()
    {
        _lastFrame =
            DateTime.UtcNow;

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
        {
            _canvas.Children.Remove(
                particle.Control);
        }

        _particles.Clear();
    }

    public void Burst(int amount)
    {
        if (!Settings.Enabled ||
            !Settings.PetalsEnabled)
        {
            return;
        }

        amount =
            Math.Min(
                amount,
                Settings.MaxParticles);

        for (var i = 0; i < amount; i++)
        {
            SpawnPetal(
                randomY: true);
        }
    }

    private void Tick(
        object? sender,
        EventArgs e)
    {
        var now =
            DateTime.UtcNow;

        var delta =
            (now - _lastFrame)
            .TotalSeconds;

        _lastFrame = now;

        delta =
            Math.Clamp(
                delta,
                0,
                0.05);

        if (!Settings.Enabled ||
            !Settings.PetalsEnabled)
        {
            Clear();
            return;
        }

        Spawn(delta);
        Update(delta);
    }

    private void Spawn(
        double delta)
    {
        if (_canvas.Bounds.Width <= 0 ||
            _canvas.Bounds.Height <= 0)
        {
            return;
        }

        var spawnRate =
            Settings.Density * 0.8;

        _spawnAccumulator +=
            delta * spawnRate;

        while (_spawnAccumulator >= 1)
        {
            _spawnAccumulator--;

            if (_particles.Count >=
                Settings.MaxParticles)
            {
                break;
            }

            SpawnPetal(
                randomY: false);
        }
    }

    private void SpawnPetal(
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
                0.7,
                1.3);

        var petal =
            new TextBlock
            {
                Text =
                    RandomPetal(),

                FontSize = size,

                Opacity =
                    Math.Clamp(
                        Settings.Opacity *
                        RandomRange(
                            0.7,
                            1.0),
                        0.05,
                        1.0),

                Foreground =
                    CreatePetalBrush(),

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

        petal.RenderTransform =
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

        var particle =
            new Particle
            {
                Control = petal,

                X = x,
                Y = y,

                VelocityX =
                    RandomRange(
                        -8,
                        8),

                VelocityY =
                    RandomRange(
                        22,
                        48) *
                    Settings.Speed,

                Rotation =
                    RandomRange(
                        0,
                        360),

                RotationSpeed =
                    RandomRange(
                        -70,
                        70),

                DriftPhase =
                    RandomRange(
                        0,
                        Math.PI * 2),

                DriftSpeed =
                    RandomRange(
                        0.8,
                        2.0),

                Lifetime =
                    RandomRange(
                        8,
                        18)
            };

        rotation.Angle =
            particle.Rotation;

        Canvas.SetLeft(
            petal,
            x);

        Canvas.SetTop(
            petal,
            y);

        _canvas.Children.Add(
            petal);

        _particles.Add(
            particle);
    }

    private void Update(
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
            var particle =
                _particles[i];

            particle.Age += delta;

            particle.DriftPhase +=
                delta *
                particle.DriftSpeed;

            var drift =
                Math.Sin(
                    particle.DriftPhase) *
                16;

            var wind =
                Settings.Wind * 30;

            particle.X +=
                (
                    particle.VelocityX +
                    drift +
                    wind
                ) * delta;

            particle.Y +=
                particle.VelocityY *
                delta *
                Settings.Speed;

            particle.Rotation +=
                particle.RotationSpeed *
                delta;

            Canvas.SetLeft(
                particle.Control,
                particle.X);

            Canvas.SetTop(
                particle.Control,
                particle.Y);

            if (particle.Control.RenderTransform
                is RotateTransform rotation)
            {
                rotation.Angle =
                    particle.Rotation;
            }

            var expired =
                particle.Age >
                particle.Lifetime;

            var below =
                particle.Y >
                height + 60;

            var tooFar =
                particle.X <
                    -120 ||
                particle.X >
                    width + 120;

            if (!expired &&
                !below &&
                !tooFar)
            {
                continue;
            }

            _canvas.Children.Remove(
                particle.Control);

            _particles.RemoveAt(i);
        }
    }

    private string RandomPetal()
    {
        var values =
            new[]
            {
                "❀",
                "✿",
                "❁",
                "•"
            };

        return values[
            _random.Next(
                values.Length)];
    }

    private IBrush CreatePetalBrush()
    {
        var colors =
            new[]
            {
                Color.Parse("#F7A8C4"),
                Color.Parse("#E88DB3"),
                Color.Parse("#FFD0DF"),
                Color.Parse("#D77BA5")
            };

        return new SolidColorBrush(
            colors[
                _random.Next(
                    colors.Length)]);
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