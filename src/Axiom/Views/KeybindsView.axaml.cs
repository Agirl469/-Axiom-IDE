using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;

using Axiom.Services;

namespace Axiom.Views;

public partial class KeybindsView : UserControl
{
    private readonly KeybindService _keybinds =
        KeybindService.Current;

    private readonly StackPanel _bindingList;
    private readonly TextBlock _statusText;

    private string? _waitingFor;
    private Button? _waitingButton;

    public KeybindsView()
    {
        AvaloniaXamlLoader.Load(this);

        _bindingList =
            this.FindControl<StackPanel>("BindingList")
            ?? throw new InvalidOperationException(
                "BindingList was not found.");

        _statusText =
            this.FindControl<TextBlock>("StatusText")
            ?? throw new InvalidOperationException(
                "StatusText was not found.");

        RefreshBindings();
    }

    private void RefreshBindings()
    {
        _bindingList.Children.Clear();

        foreach (var entry in _keybinds.GetBindings())
        {
            var action =
                new TextBlock
                {
                    Text = entry.Action,
                    VerticalAlignment =
                        VerticalAlignment.Center
                };

            var keyButton =
                new Button
                {
                    Content =
                        KeybindService.Format(
                            entry.Bind),

                    MinWidth = 150,
                    HorizontalContentAlignment =
                        HorizontalAlignment.Center
                };

            var actionName =
                entry.Action;

            keyButton.Click +=
                (_, _) =>
                BeginCapture(
                    actionName,
                    keyButton);

            var row =
                new Grid
                {
                    ColumnDefinitions =
                        new ColumnDefinitions(
                            "*,Auto"),

                    Margin =
                        new Thickness(
                            0,
                            2)
                };

            row.Children.Add(action);

            Grid.SetColumn(
                keyButton,
                1);

            row.Children.Add(
                keyButton);

            var panel =
                new Border
                {
                    Classes =
                    {
                        "panel"
                    },

                    Padding =
                        new Thickness(
                            12,
                            8),

                    Child = row
                };

            _bindingList.Children.Add(
                panel);
        }
    }

    private void BeginCapture(
        string action,
        Button button)
    {
        _waitingFor = action;
        _waitingButton = button;

        button.Content =
            "Press keys...";

        _statusText.Text =
            $"Setting shortcut for {action}";

        Focus();
    }

    private async void View_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (_waitingFor is null)
            return;

        e.Handled = true;

        if (e.Key == Key.Escape)
        {
            CancelCapture();
            return;
        }

        if (KeybindService.IsModifierKey(
                e.Key))
        {
            return;
        }

        var bind =
            new Keybind(
                e.Key,
                e.KeyModifiers);

        var conflict =
            _keybinds.FindConflict(
                _waitingFor,
                bind);

        if (conflict is not null)
        {
            _statusText.Text =
                $"{KeybindService.Format(bind)} is already used by {conflict}.";

            return;
        }

        var action =
            _waitingFor;

        _keybinds.SetBinding(
            action,
            bind);

        await _keybinds.SaveAsync();

        _waitingFor = null;
        _waitingButton = null;

        RefreshBindings();

        _statusText.Text =
            $"{action}: {KeybindService.Format(bind)}";
    }

    private void CancelCapture()
    {
        _waitingFor = null;
        _waitingButton = null;

        RefreshBindings();

        _statusText.Text =
            "Shortcut change cancelled.";
    }

    private async void Reset_Click(
        object? sender,
        RoutedEventArgs e)
    {
        await _keybinds.ResetAsync();

        _waitingFor = null;
        _waitingButton = null;

        RefreshBindings();

        _statusText.Text =
            "Default shortcuts restored.";
    }
}