namespace Axiom.Effects;

public static class EffectProfiles
{
    public static IReadOnlyList<string> Names { get; } =
    [
        "Minimal",
        "Sakura",
        "Snowy",
        "Night Sky",
        "Rainy",
        "Chaos",
        "Custom"
    ];

    public static void Apply(
        EffectSettings settings,
        string profile)
    {
        settings.Profile = profile;

        switch (profile)
        {
            case "Minimal":
                Clear(settings);

                settings.Enabled = true;
                settings.Density = 2;
                settings.MaxParticles = 25;
                break;

            case "Sakura":
                Clear(settings);

                settings.Enabled = true;
                settings.PetalsEnabled = true;

                settings.Density = 7;
                settings.Speed = 0.8;
                settings.Opacity = 0.72;
                settings.Size = 18;
                settings.Wind = 0.25;
                settings.MaxParticles = 90;
                break;

            case "Snowy":
                Clear(settings);

                settings.Enabled = true;
                settings.SnowEnabled = true;

                settings.Density = 10;
                settings.Speed = 0.55;
                settings.Opacity = 0.75;
                settings.Size = 15;
                settings.Wind = 0.05;
                settings.MaxParticles = 120;
                break;

            case "Night Sky":
                Clear(settings);

                settings.Enabled = true;
                settings.FirefliesEnabled = true;
                settings.StarsEnabled = true;

                settings.Density = 5;
                settings.Speed = 0.35;
                settings.Opacity = 0.8;
                settings.Size = 12;
                settings.Wind = 0;
                settings.MaxParticles = 70;
                break;

            case "Rainy":
                Clear(settings);

                settings.Enabled = true;
                settings.RainEnabled = true;

                settings.Density = 16;
                settings.Speed = 1.8;
                settings.Opacity = 0.45;
                settings.Size = 17;
                settings.Wind = -0.1;
                settings.MaxParticles = 160;
                break;

            case "Chaos":
                settings.Enabled = true;

                settings.PetalsEnabled = true;
                settings.LeavesEnabled = true;
                settings.SnowEnabled = true;
                settings.RainEnabled = false;
                settings.FirefliesEnabled = true;
                settings.StarsEnabled = true;
                settings.NyanCatEnabled = true;

                settings.Density = 13;
                settings.Speed = 1.25;
                settings.Opacity = 0.8;
                settings.Size = 19;
                settings.Wind = 0.4;
                settings.MaxParticles = 180;
                break;

            case "Custom":
                break;
        }
    }

    private static void Clear(
        EffectSettings settings)
    {
        settings.PetalsEnabled = false;
        settings.LeavesEnabled = false;
        settings.SnowEnabled = false;
        settings.RainEnabled = false;
        settings.FirefliesEnabled = false;
        settings.StarsEnabled = false;
    }
}