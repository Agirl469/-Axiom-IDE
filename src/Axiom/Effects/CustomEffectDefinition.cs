namespace Axiom.Effects;

public sealed class CustomEffectDefinition
{
    public int Format { get; set; } = 1;

    public string Name { get; set; } =
        "Custom Effect";

    public string Author { get; set; } =
        "Unknown";

    public string Type { get; set; } =
        "particle";

    public string? Texture { get; set; }

    public EffectSpawn Spawn { get; set; } =
        new();

    public EffectParticle Particle { get; set; } =
        new();

    public EffectMotion Motion { get; set; } =
        new();
}

public sealed class EffectSpawn
{
    public string Location { get; set; } =
        "top";

    public double Rate { get; set; } =
        5;
}

public sealed class EffectParticle
{
    public double SizeMin { get; set; } =
        12;

    public double SizeMax { get; set; } =
        24;

    public double Opacity { get; set; } =
        0.7;

    public double Lifetime { get; set; } =
        8;

    public bool Rotation { get; set; } =
        true;
}

public sealed class EffectMotion
{
    public double SpeedMin { get; set; } =
        20;

    public double SpeedMax { get; set; } =
        40;

    public double Wind { get; set; }

    public double Drift { get; set; } =
        10;
}