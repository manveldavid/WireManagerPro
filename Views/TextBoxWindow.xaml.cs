using System.Windows;

namespace WireManager.Views
{
    /// <summary>
    /// Логика взаимодействия для TextBoxWindow.xaml
    /// </summary>
    public partial class TextBoxWindow : Window
	{
		public TextBoxWindow(string alert = "")
		{
			InitializeComponent();
            HelpLabel.Content = alert;
			MainTextBox.Focus();
        }
        public string Text { get; set; }
		private void Apply_Click(object sender, RoutedEventArgs e)
		{
				Text = MainTextBox.Text;
				this.Close();
        }
    }
}
