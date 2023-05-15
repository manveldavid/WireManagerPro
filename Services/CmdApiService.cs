using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WireManager.Common;
using WireManager.Models;

namespace WireManager.Services
{
    public class CmdApiService
	{
		private wgCmdProcess _cmdApi;
		private WireManagerConfig _config;
		private int _interval;

        private readonly string[] networkInterface = { "eth", "eno", "ens", "enp" };

		public CmdApiService(WireManagerConfig config, bool noWindow = true)
		{
			_config = config;
			_cmdApi = new wgCmdProcess(WireManagerConfig.PathToCmd, noWindow);
			int delay;
			var parceSuccess = Int32.TryParse(_config.Interval, out delay);
			if (parceSuccess)
				_interval = delay;
			else _interval = 10;
		}

        #region Preparations
        public bool InstallSSHAccessToServer()
		{
			File.WriteAllText(WireManagerConfig.BatUploadSshKeyToServer,
					$"""
					ssh {_config.ServerUser}@{_config.ServerIp} mkdir {WireManagerConfig.SshDirOnServer}"
					scp {WireManagerConfig.SshKeyPathOnLocal} {_config.ServerUser}@{_config.ServerIp}:{WireManagerConfig.SshKeyPathOnServer}
					""");
			Process.Start(WireManagerConfig.BatUploadSshKeyToServer).WaitForExit();
			return true;

		}
		public bool GenerateSshKeys()
		{
			Process.Start(WireManagerConfig.BatCreateSshKey).WaitForExit();
			return true;
		}
        public bool InstallWireGuardToServer()
        {
            GetResponce($"mkdir {WireManagerConfig.WgDirName}");
            Process.Start(WireManagerConfig.BatInstallWireguardToServer).WaitForExit();
            CreateNewWgConfig(GetServer(), new List<WireGuardUser>());
            GetResponce("systemctl enable wg - quick@wg0.service");
            GetResponce("systemctl start wg-quick@wg0.service");
            GetResponce("systemctl status wg-quick@wg0.service");
            GetResponce("sysctl - p");
            return true;
        }
        #endregion

        #region ServerPart
        public void ConnectToServerWithSshAccess(int delayExtend = 500)
		{
			GetResponce($"ssh {_config.ServerUser}@{_config.ServerIp}", delay: delayExtend * _interval);
			if (_config.NetworkInterface == "")
			{
				_config.NetworkInterface = GetNetworkInterface(delayExtend * _interval);
			}
			GoToWireguardDir(delayExtend);
		}
        public string GetNetworkInterface(int delay = 0)
        {
            var ipConfigResponce = GetResponce("ip a", delay: delay);
            string networkInterface = "";

            foreach (var str in ipConfigResponce.Split(':'))
            {
                if (this.networkInterface.Any(i => str.Contains(i)))
                {
                    networkInterface = str.Replace(" ", "");
                    break;
                }
            }
            return networkInterface;
        }
        public void RestartServer()
		{
			GetResponce("systemctl restart wg-quick@wg0");
			GetResponce("systemctl status wg-quick@wg0", delay:_interval);
		}
		public WireGuardUser GetServer()
		{
			var server = new WireGuardUser { Name = "_server", Ip = _config.StandartServerIp };

			SetServerKeys(server);

			return server;
		}
        public void SetServerKeys(WireGuardUser server)
        {
            server.PubKey = GetResponce($"cat {WireManagerConfig.ServerPubKeyName}", "=");
            server.PrvKey = GetResponce($"cat {WireManagerConfig.ServerPrvKeyName}", "=");
        }
        #endregion

        #region Users
        public IEnumerable<WireGuardUser> GetUsers()
		{
			var users = new List<WireGuardUser>();

			var usersFromStr = GetResponce("ls", WireManagerConfig.WgConfigFileName);
			foreach (var name in GetValidUsersFromStr(usersFromStr)) 
				users.Add(new WireGuardUser { Name = name });

			SetUserKeys(users);
			SetUserIps(users);
			return users;
		}
		public void SetUserKeys(IEnumerable<WireGuardUser> users)
		{
			foreach (var user in users)
			{
				user.PubKey = GetResponce("cat " + user + "_PubK", "=");
				user.PrvKey = GetResponce("cat " + user + "_PrvK", "=");
			}
		}
		public void SetUserIps(IEnumerable<WireGuardUser> users)
		{
			string pubKeyPropName = "PublicKey";
			string ipPropName = "AllowedIPs";
			string wgConfigFileBody = GetWgConfigBody(users.Count());
			var configParts = wgConfigFileBody.Split(']');
			foreach (var part in configParts)
			{
				var lines = part.Split('\n');
				var pubKeyLine = lines.SingleOrDefault(l => l.Contains(pubKeyPropName));
				var ipLine = lines.SingleOrDefault(l => l.Contains(ipPropName));
				if(!string.IsNullOrEmpty(pubKeyLine) && !string.IsNullOrEmpty(ipLine))
				{
					var pubKey = pubKeyLine.Substring(pubKeyLine.IndexOf('=')+1).Replace(" ", "");
					var ip = ipLine.Substring(ipLine.IndexOf('=') + 1).Replace(" ", "");

					var connectedUser = users.SingleOrDefault(u => u.PubKey == pubKey);
					if (connectedUser != null) { connectedUser.Ip = ip; }
				}
			}
		}
        public void RenameUser(IEnumerable<WireGuardUser> users, string oldName, string newName)
        {
            var user = users.SingleOrDefault(u => u.Name == oldName);
            if (user != null)
            {
                if (!string.IsNullOrEmpty(user.PubKey) &&
                   !string.IsNullOrEmpty(user.PrvKey) &&
                   user.Name != newName)
                {
                    GetResponce($"echo \"{user.PubKey}\" > {newName}_PubK");
                    GetResponce($"echo \"{user.PrvKey}\" > {newName}_PrvK");
                    GetResponce($"rm {user.Name}_PubK");
                    GetResponce($"rm {user.Name}_PrvK");
                    user.Name = newName;
                }
            }
        }
        public void RemoveUser(List<WireGuardUser> users, string username)
        {
            var user = users.SingleOrDefault(u => u.Name == username);
            if (user != null && string.IsNullOrEmpty(user.Ip))
            {
                if (!string.IsNullOrEmpty(user.PubKey) &&
                    !string.IsNullOrEmpty(user.PrvKey))
                {
                    GetResponce($"rm {user.Name}_PubK");
                    GetResponce($"rm {user.Name}_PrvK");
                    users.Remove(user);
                }
            }
        }
        public void CreateUser(List<WireGuardUser> users, string username)
        {
            var user = new WireGuardUser { Name = username };
            if (!users.Select(u => u.Name).Contains(user.Name))
            {
                if (string.IsNullOrEmpty(user.PrvKey) && string.IsNullOrEmpty(user.PubKey))
                {
                    GetResponce($"wg genkey | tee {WireManagerConfig.WgDirName}{user.Name}_PrvK | wg pubkey | tee {WireManagerConfig.WgDirName}{user.Name}_PubK", "=");
                    user.PrvKey = GetResponce("cat " + user.Name + "_PrvK", "=");
                    user.PubKey = GetResponce("cat " + user.Name + "_PubK", "=");
                    users.Add(user);
                }
            }
        }
        public void UploadUsersOnServer(IEnumerable<WireGuardUser> users)
        {
            foreach (var user in users)
                PutUserOnServer(user);
        }
        public void PutUserOnServer(WireGuardUser user)
        {
            if (!string.IsNullOrEmpty(user.PubKey) && !string.IsNullOrEmpty(user.PrvKey) && !string.IsNullOrWhiteSpace(user.Name))
            {
                GetResponce($"echo \"{user.PubKey}\" > {user.Name}_PubK");
                GetResponce($"echo \"{user.PrvKey}\" > {user.Name}_PrvK");
            }
            else
            {
                GetResponce($"wg genkey | tee {WireManagerConfig.WgDirName}{user.Name}_PrvK | wg pubkey | tee {WireManagerConfig.WgDirName}{user.Name}_PubK", "=");
                user.PubKey = GetResponce("cat " + user.Name + "_PubK", "=");
                user.PrvKey = GetResponce("cat " + user.Name + "_PrvK", "=");
            }
        }
        public void BackUpUsers(IEnumerable<WireGuardUser> users, string path = "")
        {
            if (string.IsNullOrWhiteSpace(path))
                path = _config.PathToSaves;

            var serializedString =
                JsonSerializer.Serialize<IEnumerable<WireGuardUser>>(users, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(Path.Combine(path, WireManagerConfig.UsersBackUpFileName), serializedString);
        }
        public IEnumerable<WireGuardUser> GetUsersFromBackUp(string path = "")
        {
            if (string.IsNullOrWhiteSpace(path))
                path = _config.PathToSaves;

            IEnumerable<WireGuardUser> users;
            using (var stream = File.OpenRead(Path.Combine(path, WireManagerConfig.UsersBackUpFileName)))
            {
                users = JsonSerializer.Deserialize<IEnumerable<WireGuardUser>>(stream);
            }

            return users;
        }
        public void ConnectUser(IEnumerable<WireGuardUser> users, string username, WireGuardUser server)
        {
            var user = users.SingleOrDefault(u => u.Name == username);
            if (user != null && string.IsNullOrEmpty(user.Ip))
            {
                user.Ip = WireGuardUser.GetIpFromUserId(_config.StandartUserIp,
                    WireGuardUser.GetNewUserId(users.ToList(), server));
            }
        }
        public void DisConnectUser(IEnumerable<WireGuardUser> users, string username)
        {
            var user = users.SingleOrDefault(u => u.Name == username);
            if (user != null && !string.IsNullOrEmpty(user.Ip))
            {
                user.Ip = "";
            }
        }
        public void SaveFullFormatUserAccess(IEnumerable<WireGuardUser> users, string username,
            WireGuardUser server)
        {
            var user = users.SingleOrDefault(u => u.Name == username);
            string userAccess = "";
            if (user != null && !string.IsNullOrEmpty(user.Ip))
            {
                userAccess = GetTextUserAccess(user, server);
                var savePath = Path.Combine(_config.PathToSaves, _config.UsersAccessDir);
                if (!Directory.Exists(savePath)) { Directory.CreateDirectory(savePath); }
                savePath = Path.Combine(savePath, username);
                if (!Directory.Exists(savePath)) { Directory.CreateDirectory(savePath); }
                SaveAsPngQrCode(userAccess, Path.Combine(savePath, WireManagerConfig.UsersAccessPng));
                SaveAsTxtFile(userAccess, Path.Combine(savePath, WireManagerConfig.UsersAccessTxt));
            }
        }
        public string GetTextUserAccess(WireGuardUser user, WireGuardUser server)
        {
            var userAccessText =
                $"""
				[Interface]
				PrivateKey = {user.PrvKey}
				Address = {user.Ip}
				DNS = {_config.StandartDNS}

				[Peer]
				PublicKey = {server.PubKey}
				AllowedIPs = {_config.StandartAllowedIpServer}
				EndPoint = {_config.ServerIp}:{_config.StandartListenPort}
				PersistentKeepalive = {_config.KeepAlive}
				""";

            return userAccessText;
        }
        public void SaveAsPngQrCode(string data, string path)
        {
            var qrSaver = new QRCodeSaver();
            qrSaver.SaveQRCodeAsPng(data, path);
        }
        public void SaveAsTxtFile(string data, string path)
        {
            File.WriteAllText(path, data);
        }
        public IEnumerable<string> GetValidUsersFromStr(string str) =>
             str.Split('\n')
                .Where(u => u.Contains("_PubK") || u.Contains("_PrvK"))
                .GroupBy(x => x.Substring(0, x.IndexOf('_')))
                .Where(group => group.Count() > 1)
                .Select(p => p.Key);//get all username, that have two key-file on server: username_PubK, username_PrvK

        #endregion

        #region WgConfig
        public void PutOnServerWgConfigFromFile(string path = "")
		{
			if (string.IsNullOrEmpty(path))
				path = Path.Combine(_config.PathToSaves, WireManagerConfig.WgConfigFileName);

			if (File.Exists(path))
			{
				var newWgConfig = File.ReadAllText(path);
				GetResponce($"rm {WireManagerConfig.WgConfigFileName}");
				GetResponce($"echo \"{newWgConfig}\" > {WireManagerConfig.WgConfigFileName}");
			}
		}
		public void BackUpWgConfig(string path = "", string wgConfigFileBody = "", int usersCount = 100)
		{
			if (string.IsNullOrWhiteSpace(path)) 
				path = _config.PathToSaves;
			if (string.IsNullOrWhiteSpace(wgConfigFileBody))
				wgConfigFileBody = GetWgConfigBody(usersCount);

			File.WriteAllText(Path.Combine(path, WireManagerConfig.WgConfigFileName), wgConfigFileBody);
		}
		public void CreateNewWgConfig(WireGuardUser server, IEnumerable<WireGuardUser> users)
		{
			string newWgConfig =
				$"""
				[Interface]
				PrivateKey = {server.PrvKey}
				Address = {server.Ip}
				ListenPort = {_config.StandartListenPort}
				PostUp = {_config.StandartPostUpArg} {_config.NetworkInterface} -j MASQUERADE
				PostDown = {_config.StandartPostDownArg} {_config.NetworkInterface} -j MASQUERADE

				""";

			foreach (var user in users
				.Where(u => !string.IsNullOrEmpty(u.Ip))
				.OrderBy(u => u.UserId))
			{
				newWgConfig +=
					$"""


					[Peer]
					PublicKey = {user.PubKey}
					AllowedIPs = {user.Ip}
					""";
			}
			BackUpWgConfig(_config.PathToSaves, newWgConfig);
			GetResponce($"rm {WireManagerConfig.WgConfigFileName}");
			GetResponce($"echo \"{newWgConfig}\" > {WireManagerConfig.WgConfigFileName}");
			RestartServer();
		}
		public string GetWgConfigBody(int delayExtend = 1)
		{
			return GetResponce($"cat {WireManagerConfig.WgConfigFileName}", "Interface", delayExtend * _interval);
		}
        #endregion

        public void GoToWireguardDir(int delayExtend = 1) =>
            GetResponce($"cd {WireManagerConfig.WgDirName}", delay: delayExtend * _interval);
        public string GetResponce(string request, string responceContains = "", int delay = 0)
		{
			var oldHistory = _cmdApi.MessageBuffer;
			_cmdApi.input.WriteLine(request);
			while (!_cmdApi.MessageBuffer.Contains(responceContains)) ;
			if (delay > 0) { Task.Delay(delay).Wait(); }
			var newHistory = _cmdApi.MessageBuffer;
			var result = 
				newHistory.Length > oldHistory.Length+1?
				newHistory.Substring(oldHistory.Length+1):
				"";
			_cmdApi.MessageBuffer = "";
			return result;
		}
		public string MessageHistory => _cmdApi.MessageHistory;
		public void ExitApp() => Environment.Exit(0);
		public void ExitApp(WireGuardUser server, IEnumerable<WireGuardUser> users) 
		{ 
			CreateNewWgConfig(server, users);
            BackUpUsers(users);
			RestartServer();
			Environment.Exit(0);
		}
	}
}
