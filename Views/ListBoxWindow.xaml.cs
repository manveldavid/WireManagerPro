using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WireManager.Models;

namespace WireManager.Views
{
    /// <summary>
    /// Логика взаимодействия для ListBoxWindow.xaml
    /// </summary>
    public partial class ListBoxWindow : Window
	{
		public ListBoxWindow(List<WireGuardUser> users, bool singleSelectionMode = true)
		{
			InitializeComponent();
			MainListBox.ItemsSource = users;
			MainListBox.SelectionMode = singleSelectionMode ? SelectionMode.Single : SelectionMode.Extended;
		}
		public List<WireGuardUser> selectedItems { get; set; }

		private void Apply_Click(object sender, RoutedEventArgs e)
		{
			if(MainListBox.SelectedItems?.Count > 0)
			{
				selectedItems = MainListBox.SelectedItems.Cast<WireGuardUser>().ToList();
				this.Close();
			}
		}
	}
}
