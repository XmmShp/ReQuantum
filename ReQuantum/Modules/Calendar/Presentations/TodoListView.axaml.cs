using Avalonia.Controls;

namespace ReQuantum.Views;

public partial class TodoListView : UserControl
{
    public TodoListView()
    {
        InitializeComponent();
    }
    private void UnfoldList(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.ContextMenu != null)
        {
            button.ContextMenu.Open(button);
            e.Handled = true;
        }
    }
}
