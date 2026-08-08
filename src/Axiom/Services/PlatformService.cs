namespace Axiom.Services;

public sealed class PlatformService
{
    public string PlatformName => OperatingSystem.IsWindows() ? "Windows" : "Linux";

    public string LinuxPackageManager
    {
        get
        {
            if (OperatingSystem.IsWindows())
                return "winget";

            if (File.Exists("/usr/bin/pacman")) return "pacman";
            if (File.Exists("/usr/bin/apt") || File.Exists("/usr/bin/apt-get")) return "apt";
            if (File.Exists("/usr/bin/dnf")) return "dnf";
            if (File.Exists("/usr/bin/zypper")) return "zypper";

            return "unknown";
        }
    }
}
