using System;
using System.Windows;

namespace WireManager.Views
{
    /// <summary>
    /// Логика взаимодействия для BoolChoiseWindow.xaml
    /// </summary>
    public partial class BoolChoiseWindow : Window
    {
        public BoolChoiseWindow(string alert = "")
        {
            InitializeComponent();
            HelpLabel.Content = alert;
        }
        public bool Choise { get; set; }
        private void Yes_Click(object sender, RoutedEventArgs e)
        {
                Choise = true;
                this.Close();
        }
        private void No_Click(object sender, RoutedEventArgs e)
        {
            Choise = false;
            this.Close();
        }
    }
}
