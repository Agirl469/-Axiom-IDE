namespace Axiom.Effects;

public sealed class EffectSettings
{
    public bool Enabled { get; set; } = true;

    public string Profile { get; set; } = "Sakura";

    public bool PetalsEnabled { get; set; } = true;
    public bool LeavesEnabled { get; set; }
    public bool SnowEnabled { get; set; }
    public bool RainEnabled { get; set; }
    public bool FirefliesEnabled { get; set; }
    public bool StarsEnabled { get; set; }

    public bool NyanCatEnabled { get; set; } = true;

    public int Density { get; set; } = 6;

    public double Speed { get; set; } = 1.0;

    public double Opacity { get; set; } = 0.7;

    public double Size { get; set; } = 18;

    public double Wind { get; set; } = 0.2;

    public int MaxParticles { get; set; } = 90;

    public double NyanMinMinutes { get; set; } = 5;
    public double NyanMaxMinutes { get; set; } = 20;

    public List<string> EnabledCustomEffects { get; set; } = [];
}