namespace Axiom.Effects;

public sealed class EffectSettings
{
    public bool Enabled { get; set; } = true;

    public bool PetalsEnabled { get; set; } = true;

    public int Density { get; set; } = 6;

    public double Speed { get; set; } = 1.0;

    public double Opacity { get; set; } = 0.65;

    public double Size { get; set; } = 18;

    public double Wind { get; set; } = 0.25;

    public int MaxParticles { get; set; } = 80;

    public bool ReduceWhileTyping { get; set; } = true;
}