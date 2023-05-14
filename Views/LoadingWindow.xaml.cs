using System.Windows;

namespace WireManager.Views
{
    /// <summary>
    /// Логика взаимодействия для LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
	{
		public LoadingWindow(string content, int delay = 500)
		{
			InitializeComponent();
			ContentLabel.Content = content;
		}
	}
}
