using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using WireManager.Common;
using WireManager.Models;
using WireManager.Services;
using WireManager.Views;

namespace WireManager
{
    public partial class MainWindow : Window
	{ 
		List<WireGuardUser> users;
		CmdApiService wgCmdApi;
		WireGuardUser server;
		WireManagerConfig config;
		Window HelpWindow;

		const string _ConnectServerPhrase = "Connecting to server...";

		public MainWindow()
		{
			InitializeComponent();
			
			HelpWindow = new LoadingWindow(_ConnectServerPhrase);
			HelpWindow.Show();

			StartUpConfigure();

			HelpWindow.Close();
			ServerIpLabel.Content += config.ServerIp;
		}

        private List<WireGuardUser> connectedUsers =>
            users.Where(u => !string.IsNullOrWhiteSpace(u.Ip)).ToList();
        private List<WireGuardUser> disConnectedUsers =>
            users.Where(u => string.IsNullOrWhiteSpace(u.Ip)).ToList();

        #region CustomFunc
        private void StartUpConfigure()
		{
			config = WireManagerConfig.Deserialize();
            wgCmdApi = new CmdApiService(config);

            PrepareForConfig(config);
			PrepareBeforeConnectToServer(config);
			wgCmdApi.ConnectToServerWithSshAccess();
			PrepareAfterConnectToServer(config);

			server = wgCmdApi.GetServer();
			users = wgCmdApi.GetUsers().ToList();

			HelpWindow.Close();
		}
		public void PrepareForConfig(WireManagerConfig config)
		{
			HelpWindow.Close();

			if (string.IsNullOrWhiteSpace(config.ServerIp)) 
			{
				string ipAddress = "";

				HelpWindow = new TextBoxWindow(
					"Write the server ip here:");
				HelpWindow.ShowDialog();
				ipAddress = (HelpWindow as TextBoxWindow).Text;

				if (!string.IsNullOrWhiteSpace(ipAddress))
					config.ServerIp = ipAddress;
				else wgCmdApi.ExitApp();
			}

            if (string.IsNullOrWhiteSpace(config.ServerUser))
            {
                string username = "";

                HelpWindow = new TextBoxWindow(
					"Write the login on the server here:\n(default value is 'root')");
                HelpWindow.ShowDialog();
                username = (HelpWindow as TextBoxWindow).Text;

				if (!string.IsNullOrWhiteSpace(username))
					config.ServerUser = username;
				else config.ServerUser = WireManagerConfig.DefaultUserName;
            }

            if (config.SshAccessIsSetUp == WireManagerConfig.falseArg)
            {
                bool choise;

                HelpWindow = new BoolChoiseWindow(
					"Is ssh-key access already installed\n on the server?");
                HelpWindow.ShowDialog();
                choise = (HelpWindow as BoolChoiseWindow).Choise;

				if (choise)
				{
					config.SshAccessIsSetUp = WireManagerConfig.trueArg;
					config.HasSshKey = WireManagerConfig.trueArg;
				}
            }

            if (config.WgHasInstall == WireManagerConfig.falseArg) 
			{
				bool choise;
                HelpWindow = new BoolChoiseWindow(
					"Is WireGuard already\n installed on the server?");
                HelpWindow.ShowDialog();
                choise = (HelpWindow as BoolChoiseWindow).Choise;
				if (choise)
					config.WgHasInstall = WireManagerConfig.trueArg;
            }

            HelpWindow = new LoadingWindow(_ConnectServerPhrase);
            HelpWindow.Show();

            if (!Directory.Exists(WireManagerConfig.BatDir)) 
				BatFileCreator.CreateBatFiles(config);

            if (!Directory.Exists(config.PathToSaves))
                Directory.CreateDirectory(config.PathToSaves);

            config.SaveConfigBackUp();
		}
        public void PrepareBeforeConnectToServer(WireManagerConfig config)
        {
            if (config.HasSshKey == WireManagerConfig.falseArg)
            {
                config.HasSshKey =
                    wgCmdApi.GenerateSshKeys() ? WireManagerConfig.trueArg : WireManagerConfig.falseArg;
            }

            if (config.SshAccessIsSetUp == WireManagerConfig.falseArg)
            {
                config.SshAccessIsSetUp =
                    wgCmdApi.InstallSSHAccessToServer() ? WireManagerConfig.trueArg : WireManagerConfig.falseArg;
            }
        }
        public void PrepareAfterConnectToServer(WireManagerConfig config)
		{
            if (config.WgHasInstall == WireManagerConfig.falseArg)
            {
                config.WgHasInstall =
                    wgCmdApi.InstallWireGuardToServer() ? WireManagerConfig.trueArg : WireManagerConfig.falseArg;
            }
        }
        #endregion

        #region Button_OnClicks
        private void Button_CreateUser(object sender, RoutedEventArgs e)
		{
			this.Hide();

			string username = "";

			HelpWindow = new TextBoxWindow("Write the Username here:");
			HelpWindow.ShowDialog();
			username = (HelpWindow as TextBoxWindow).Text;

			if (!string.IsNullOrWhiteSpace(username))
				wgCmdApi.CreateUser(users, username);

			this.Show();
		}

		private void Button_RenameUser(object sender, RoutedEventArgs e)
		{
			this.Hide();

			string username = "";
			WireGuardUser selectedUser;

			HelpWindow = new ListBoxWindow(users, true);
			HelpWindow.ShowDialog();
			selectedUser = (HelpWindow as ListBoxWindow).selectedItems?.FirstOrDefault();

			if (selectedUser != null)
			{
				HelpWindow = new TextBoxWindow("Write the Username here:");
				HelpWindow.ShowDialog();
				username = (HelpWindow as TextBoxWindow).Text;
			}

			if (!string.IsNullOrWhiteSpace(username) && selectedUser != null)
				wgCmdApi.RenameUser(users, selectedUser.Name, username);

			this.Show();
		}

		private void Button_RemoveUsers(object sender, RoutedEventArgs e)
		{
			this.Hide();

			List<WireGuardUser> selectedUsers;

			HelpWindow = new ListBoxWindow(disConnectedUsers, false);
			HelpWindow.ShowDialog();
			selectedUsers = (HelpWindow as ListBoxWindow).selectedItems;

			if (selectedUsers != null && selectedUsers?.Count > 0)
				foreach(var user in selectedUsers)
					wgCmdApi.RemoveUser(users, user.Name);

			this.Show();
		}

		private void Button_ConnectUsers(object sender, RoutedEventArgs e)
		{
			this.Hide();

			List<WireGuardUser> selectedUsers;

			HelpWindow = new ListBoxWindow(disConnectedUsers, false);
			HelpWindow.ShowDialog();
			selectedUsers = (HelpWindow as ListBoxWindow).selectedItems;

			if (selectedUsers != null && selectedUsers?.Count > 0)
			{
				foreach (var user in selectedUsers)
					wgCmdApi.ConnectUser(users, user.Name, server);
				wgCmdApi.CreateNewWgConfig(server, users);
			}

			this.Show();
		}

		private void Button_DisConnectUsers(object sender, RoutedEventArgs e)
		{
			this.Hide();

			List<WireGuardUser> selectedUsers;

			HelpWindow = new ListBoxWindow(connectedUsers, false);
			HelpWindow.ShowDialog();
			selectedUsers = (HelpWindow as ListBoxWindow).selectedItems;

			if (selectedUsers != null && selectedUsers?.Count > 0)
			{
				foreach (var user in selectedUsers)
					wgCmdApi.DisConnectUser(users, user.Name);
				wgCmdApi.CreateNewWgConfig(server, users);
			}

			this.Show();
		}

		private void Button_SaveUsersAccess(object sender, RoutedEventArgs e)
		{
			this.Hide();

			List<WireGuardUser> selectedUsers;

			HelpWindow = new ListBoxWindow(connectedUsers, false);
			HelpWindow.ShowDialog();
			selectedUsers = (HelpWindow as ListBoxWindow).selectedItems;

			if (selectedUsers != null && selectedUsers?.Count > 0)
			{
				foreach (var user in selectedUsers)
					wgCmdApi.SaveFullFormatUserAccess(users, user.Name, server);
			}

			this.Show();
		}

        private void Button_BackUpUsers(object sender, RoutedEventArgs e)
        {
			wgCmdApi.BackUpUsers(users);
        }

        private void Button_UsersFromBackUp(object sender, RoutedEventArgs e)
        {
            users = wgCmdApi.GetUsersFromBackUp().ToList();
            wgCmdApi.UploadUsersOnServer(users);
			wgCmdApi.CreateNewWgConfig(server, users);
        }

        private void Button_OpenAccessDir(object sender, RoutedEventArgs e)
		{
			Process.Start("explorer.exe", 
				Path.Combine(Directory.GetCurrentDirectory(), config.PathToSaves, config.UsersAccessDir));
		}
        #endregion
    }
}
