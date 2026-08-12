using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System.Text.Json;

namespace Axiom.Effects;

internal sealed class ParticleEngine
{
    private readonly Canvas _canvas;

    private readonly List<Particle> _particles =
        [];

    private readonly Random _random =
        new();

    private readonly DispatcherTimer _timer;

    private readonly EffectTextureService _textures =
        new();

    private readonly CustomEffectsManager _customEffects =
        new();

    private DateTime _lastFrame =
        DateTime.UtcNow;

    private double _spawnAccumulator;

    private EffectSettings Settings =>
        EffectService.Current.Settings;

    public ParticleEngine(Canvas canvas)
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
        if (!Settings.Enabled)
            return;

        for (var i = 0;
             i < amount &&
             _particles.Count < Settings.MaxParticles;
             i++)
        {
            SpawnRandom(true);
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

        _lastFrame =
            now;

        if (!Settings.Enabled)
        {
            Clear();
            return;
        }

        SpawnParticles(delta);
        UpdateParticles(delta);
    }

    private void SpawnParticles(double delta)
    {
        if (_canvas.Bounds.Width <= 0 ||
            _canvas.Bounds.Height <= 0)
        {
            return;
        }

        _spawnAccumulator +=
            delta *
            Math.Max(
                1,
                Settings.Density);

        while (_spawnAccumulator >= 1)
        {
            _spawnAccumulator--;

            if (_particles.Count >=
                Settings.MaxParticles)
            {
                break;
            }

            SpawnRandom(false);
        }
    }

    private void SpawnRandom(bool randomY)
    {
        var kinds =
            GetEnabledKinds();

        var custom =
            _customEffects
                .GetInstalled()
                .Where(_customEffects.IsEnabled)
                .ToList();

        if (custom.Count > 0)
            kinds.Add(ParticleKind.Custom);

        if (kinds.Count == 0)
            return;

        var kind =
            kinds[
                _random.Next(
                    kinds.Count)];

        if (kind == ParticleKind.Custom)
        {
            var effect =
                custom[
                    _random.Next(
                        custom.Count)];

            SpawnCustom(
                effect,
                randomY);

            return;
        }

        SpawnBuiltIn(
            kind,
            randomY);
    }

    private List<ParticleKind> GetEnabledKinds()
    {
        var kinds =
            new List<ParticleKind>();

        if (Settings.PetalsEnabled)
            kinds.Add(ParticleKind.Petal);

        if (Settings.LeavesEnabled)
            kinds.Add(ParticleKind.Leaf);

        if (Settings.SnowEnabled)
            kinds.Add(ParticleKind.Snow);

        if (Settings.StarsEnabled)
            kinds.Add(ParticleKind.Star);

        if (Settings.FirefliesEnabled)
            kinds.Add(ParticleKind.Firefly);

        if (Settings.RainEnabled)
        {
            kinds.Add(ParticleKind.Rain);
            kinds.Add(ParticleKind.Rain);
        }

        if (Settings.HeartsEnabled)
            kinds.Add(ParticleKind.Heart);

        if (Settings.ButterfliesEnabled)
            kinds.Add(ParticleKind.Butterfly);

        if (Settings.FeathersEnabled)
            kinds.Add(ParticleKind.Feather);

        return kinds;
    }

    private void SpawnBuiltIn(
        ParticleKind kind,
        bool randomY)
    {
        var asset =
            PickAsset(kind);

        var bitmap =
            _textures.LoadBuiltIn(asset);

        if (bitmap is null)
            return;

        var profile =
            GetMotionProfile(kind);

        SpawnImage(
            bitmap,
            kind,
            randomY,
            profile);
    }

    private string PickAsset(ParticleKind kind)
    {
        return kind switch
        {
            ParticleKind.Petal =>
                $"Petals/petal_{_random.Next(1, 5):00}.png",

            ParticleKind.Leaf =>
                $"Leaves/leaf_{_random.Next(1, 3):00}.png",

            ParticleKind.Snow =>
                $"Snow/snow_{_random.Next(1, 3):00}.png",

            ParticleKind.Star =>
                $"Stars/star_{_random.Next(1, 3):00}.png",

            ParticleKind.Firefly =>
                "Fireflies/firefly_01.png",

            ParticleKind.Rain =>
                "Rain/rain_01.png",

            ParticleKind.Heart =>
                "Hearts/heart_01.png",

            ParticleKind.Butterfly =>
                "Butterflies/butterfly_01.png",

            ParticleKind.Feather =>
                "Feathers/feather_01.png",

            _ =>
                "Stars/star_01.png"
        };
    }

    private ParticleMotionProfile GetMotionProfile(
        ParticleKind kind)
    {
        return kind switch
        {
            ParticleKind.Petal =>
                new(
                    RandomRange(18, 38),
                    18,
                    1.3,
                    true,
                    false),

            ParticleKind.Leaf =>
                new(
                    RandomRange(28, 55),
                    25,
                    1.1,
                    true,
                    false),

            ParticleKind.Snow =>
                new(
                    RandomRange(8, 22),
                    12,
                    0.55,
                    true,
                    false),

            ParticleKind.Rain =>
                new(
                    RandomRange(230, 350),
                    0,
                    0,
                    false,
                    false),

            ParticleKind.Firefly =>
                new(
                    RandomRange(-4, 5),
                    26,
                    1.8,
                    false,
                    true),

            ParticleKind.Star =>
                new(
                    RandomRange(3, 10),
                    8,
                    0.5,
                    false,
                    true),

            ParticleKind.Heart =>
                new(
                    RandomRange(15, 30),
                    12,
                    0.7,
                    true,
                    false),

            ParticleKind.Butterfly =>
                new(
                    RandomRange(8, 20),
                    34,
                    2.2,
                    false,
                    false),

            ParticleKind.Feather =>
                new(
                    RandomRange(13, 27),
                    25,
                    0.8,
                    true,
                    false),

            _ =>
                new(
                    25,
                    12,
                    1,
                    true,
                    false)
        };
    }

    private void SpawnCustom(
        InstalledEffect installed,
        bool randomY)
    {
        CustomEffectDefinition? definition;

        try
        {
            var json =
                File.ReadAllText(
                    installed.DefinitionPath);

            definition =
                JsonSerializer
                    .Deserialize<CustomEffectDefinition>(
                        json);
        }
        catch
        {
            return;
        }

        if (definition is null ||
            string.IsNullOrWhiteSpace(
                definition.Texture))
        {
            return;
        }

        var bitmap =
            _textures.LoadImported(
                installed.Directory,
                definition.Texture);

        if (bitmap is null)
            return;

        var minSize =
            Math.Clamp(
                definition.Particle.SizeMin,
                2,
                128);

        var maxSize =
            Math.Clamp(
                definition.Particle.SizeMax,
                minSize,
                128);

        var profile =
            new ParticleMotionProfile(
                RandomRange(
                    definition.Motion.SpeedMin,
                    definition.Motion.SpeedMax),

                Math.Clamp(
                    definition.Motion.Drift,
                    0,
                    100),

                1,
                definition.Particle.Rotation,
                false);

        SpawnImage(
            bitmap,
            ParticleKind.Custom,
            randomY,
            profile,

            RandomRange(
                minSize,
                maxSize),

            Math.Clamp(
                definition.Particle.Opacity,
                0.05,
                1),

            Math.Clamp(
                definition.Particle.Lifetime,
                0.5,
                60),

            Math.Clamp(
                definition.Motion.Wind,
                -100,
                100));
    }

    private void SpawnImage(
        Bitmap bitmap,
        ParticleKind kind,
        bool randomY,
        ParticleMotionProfile profile,
        double? customSize = null,
        double? customOpacity = null,
        double? customLifetime = null,
        double? customWind = null)
    {
        var canvasWidth =
            Math.Max(
                _canvas.Bounds.Width,
                1);

        var canvasHeight =
            Math.Max(
                _canvas.Bounds.Height,
                1);

        var size =
            customSize ??
            Settings.Size *
            RandomRange(
                0.65,
                1.3);

        if (kind == ParticleKind.Rain)
            size *= 1.35;

        var image =
            new Image
            {
                Source = bitmap,

                Width =
                    kind == ParticleKind.Rain
                        ? Math.Max(4, size * 0.25)
                        : size,

                Height =
                    kind == ParticleKind.Rain
                        ? Math.Max(18, size * 1.8)
                        : size,

                Stretch =
                    Stretch.Uniform,

                Opacity =
                    customOpacity ??
                    Math.Clamp(
                        Settings.Opacity *
                        RandomRange(
                            0.75,
                            1),
                        0.05,
                        1),

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

        image.RenderTransform =
            rotation;

        var x =
            RandomRange(
                -30,
                canvasWidth + 30);

        var y =
            randomY
                ? RandomRange(
                    -30,
                    canvasHeight)
                : RandomRange(
                    -100,
                    -15);

        var particle =
            new Particle
            {
                Control =
                    image,

                Kind =
                    kind,

                X =
                    x,

                Y =
                    y,

                VelocityX =
                    RandomRange(
                        -5,
                        5)
                    +
                    (customWind ??
                     Settings.Wind * 22),

                VelocityY =
                    profile.VerticalSpeed *
                    Settings.Speed,

                Rotation =
                    RandomRange(
                        0,
                        360),

                RotationSpeed =
                    profile.Rotate
                        ? RandomRange(
                            -55,
                            55)
                        : 0,

                DriftPhase =
                    RandomRange(
                        0,
                        Math.PI * 2),

                DriftSpeed =
                    profile.DriftSpeed,

                DriftAmount =
                    profile.DriftAmount,

                Lifetime =
                    customLifetime ??
                    RandomRange(
                        8,
                        18),

                BaseOpacity =
                    image.Opacity,

                Pulse =
                    profile.Pulse
            };

        rotation.Angle =
            particle.Rotation;

        Canvas.SetLeft(
            image,
            x);

        Canvas.SetTop(
            image,
            y);

        _canvas.Children.Add(
            image);

        _particles.Add(
            particle);
    }

    private void UpdateParticles(double delta)
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

            particle.Age +=
                delta;

            particle.DriftPhase +=
                delta *
                particle.DriftSpeed;

            var drift =
                Math.Sin(
                    particle.DriftPhase) *
                particle.DriftAmount;

            if (particle.Kind ==
                ParticleKind.Firefly)
            {
                particle.X +=
                    (
                        particle.VelocityX +
                        drift
                    ) *
                    delta;

                particle.Y +=
                    (
                        particle.VelocityY +
                        Math.Cos(
                            particle.DriftPhase) *
                        particle.DriftAmount
                    ) *
                    delta;
            }
            else if (particle.Kind ==
                     ParticleKind.Butterfly)
            {
                particle.X +=
                    (
                        particle.VelocityX +
                        drift * 1.3
                    ) *
                    delta;

                particle.Y +=
                    (
                        particle.VelocityY +
                        Math.Sin(
                            particle.DriftPhase * 2)
                        * 12
                    ) *
                    delta;
            }
            else
            {
                particle.X +=
                    (
                        particle.VelocityX +
                        drift
                    ) *
                    delta;

                particle.Y +=
                    particle.VelocityY *
                    delta;
            }

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
                is RotateTransform transform)
            {
                transform.Angle =
                    particle.Rotation;
            }

            if (particle.Pulse)
            {
                particle.Control.Opacity =
                    Math.Clamp(
                        particle.BaseOpacity *
                        (
                            0.65 +
                            Math.Sin(
                                particle.DriftPhase * 2)
                            * 0.35
                        ),
                        0.08,
                        1);
            }

            var remove =
                particle.Age >
                    particle.Lifetime ||
                particle.Y >
                    height + 120 ||
                particle.X <
                    -180 ||
                particle.X >
                    width + 180;

            if (!remove)
                continue;

            _canvas.Children.Remove(
                particle.Control);

            _particles.RemoveAt(i);
        }
    }

    private double RandomRange(
        double min,
        double max)
    {
        if (max < min)
            (min, max) =
                (max, min);

        return min +
               _random.NextDouble() *
               (max - min);
    }

    private readonly record struct ParticleMotionProfile(
        double VerticalSpeed,
        double DriftAmount,
        double DriftSpeed,
        bool Rotate,
        bool Pulse);
}