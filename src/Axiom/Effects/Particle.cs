using Avalonia.Controls;

namespace Axiom.Effects;

internal sealed class Particle
{
    public required Control Control { get; init; }

    public double X { get; set; }
    public double Y { get; set; }

    public double VelocityX { get; set; }
    public double VelocityY { get; set; }

    public double Rotation { get; set; }
    public double RotationSpeed { get; set; }

    public double DriftPhase { get; set; }
    public double DriftSpeed { get; set; }

    public double Age { get; set; }
    public double Lifetime { get; set; }

    public ParticleKind Kind { get; set; }
}

internal enum ParticleKind
{
    Petal,
    Leaf,
    Snow,
    Rain,
    Firefly,
    Star
}