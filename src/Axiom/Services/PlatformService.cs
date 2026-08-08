namespace Axiom.Services;

using System.IO;
public sealed class PlatformService
{
    public string PlatformName
    {
        get
        {
            if (OperatingSystem.IsLinux())
                return "Linux";

            if (OperatingSystem.IsWindows())
                return "Windows";

            if (OperatingSystem.IsMacOS())
                return "macOS";

            return "Unknown";
        }
    }
    public string LinuxPackageManager
    {
        get
        {
            if (!OperatingSystem.IsLinux())
                return "none";

            if (File.Exists("/usr/bin/apt"))
                return "apt";

            if (File.Exists("/usr/bin/pacman"))
                return "pacman";

            if (File.Exists("/usr/bin/dnf"))
                return "dnf";

            if (File.Exists("/usr/bin/zypper"))
                return "zypper";

            return "unknown";
        }
    }
}
