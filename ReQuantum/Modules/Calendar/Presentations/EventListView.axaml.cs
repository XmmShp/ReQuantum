using Avalonia.Controls;

namespace ReQuantum.Views;

public partial class EventListView : UserControl
{
    public EventListView()
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
//MoreButton_Click