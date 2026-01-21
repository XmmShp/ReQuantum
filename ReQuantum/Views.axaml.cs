using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ReQuantum.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            GreetingText.Text = $"Hello, {NameInput.Text}!";
        }
    }
}
