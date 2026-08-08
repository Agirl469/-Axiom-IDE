using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Axiom.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
