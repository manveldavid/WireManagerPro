using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WireManager.Models
{
    public class WireManagerConfig
	{
		public WireManagerConfig() { }

        #region Methods
        public static WireManagerConfig Deserialize(string configFilePath = ConfigFileName)
        {
            WireManagerConfig config = new WireManagerConfig();
            if (!string.IsNullOrWhiteSpace(configFilePath) && File.Exists(configFilePath))
            {
                using (var stream = File.OpenRead(configFilePath))
                {
                    var configFromJson = JsonSerializer.Deserialize<WireManagerConfig>(stream,
                        new JsonSerializerOptions
                        {

                        });

                    if (configFromJson != null)
                    {
                        config = configFromJson;
                    }
                }
            }
            return config;
        }
        public static string Serialize(WireManagerConfig config) =>
            JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        public static void SaveConfigBackUp(WireManagerConfig config, string path = ConfigFileName)
        {
            var jsonString = Serialize(config);
            File.WriteAllText(path, jsonString);
        }
        public void SaveConfigBackUp(string path = ConfigFileName)
        {
            var jsonString = Serialize(this);
            File.WriteAllText(path, jsonString);
        }
        #endregion

        #region ConstValues
        public const string falseArg = "false";
        public const string trueArg = "true";
        public const string DefaultUserName = "root";
        public const string ServerPubKeyName = "publickey";
        public const string ServerPrvKeyName = "privatekey";

        public const string BatDir = "bats/";
        public const string WgDirName = "/etc/wireguard/";
        public const string SshDirOnServer = "~/.ssh/";

        public const string SshKeyPathOnLocal = "%userprofile%/.ssh/id_rsa.pub";
        public const string SshKeyPathOnServer = SshDirOnServer + "authorized_keys";

        public const string ConfigFileName = "WireManagerConfig.json";
        public const string ServerBackUpFileName = "serverBackUp.json";
        public const string UsersBackUpFileName = "usersBackUp.json";

        public const string WgConfigFileName = "wg0.conf";

		public const string PathToCmd = "C:\\WINDOWS\\system32\\cmd.exe";

        public const string BatUploadSshKeyToServer = BatDir + "uploadSshKeyToServer.bat";
        public const string BatCreateSshKey = BatDir + "createSshKey.bat";
        public const string BatConnectToServer = BatDir + "connectToServerAndInstallWireGuard.bat";
        public const string BatInstallWireguardToServer = BatDir + "connectToServerAndInstallWireGuard.bat";

        public const string UsersAccessTxt = "Access.txt";
        public const string UsersAccessPng = "Access.png";
        #endregion


        [JsonPropertyName("serverIp")]
		public string ServerIp { get; set; } = "";

        [JsonPropertyName("networkInterface")]
        public string NetworkInterface { get; set; } = "";

        [JsonPropertyName("serverUser")]
        public string ServerUser { get; set; } = "";


        #region PropsWithDefaultValues
        [JsonPropertyName("hasSshKey")]
        public string HasSshKey { get; set; } = falseArg;

        [JsonPropertyName("wgHasInstall")]
		public string WgHasInstall { get; set; } = falseArg;

        [JsonPropertyName("sshAccessIsSetUp")]
		public string SshAccessIsSetUp { get; set; } = falseArg;

		[JsonPropertyName("interval")]
		public string Interval { get; set; } = "100";

		[JsonPropertyName("pathToSaves")]
		public string PathToSaves { get; set; } = "usersData\\";

        [JsonPropertyName("usersAccessDir")]
        public string UsersAccessDir { get; set; } = "usersAccess\\";

		[JsonPropertyName("serverKeepAlive")]
		public string KeepAlive { get; set; } = "30";

        [JsonPropertyName("standartDNS")]
		public string StandartDNS { get; set; } = "8.8.8.8";

		[JsonPropertyName("standartServerIp")]
		public string StandartServerIp { get; set; } = "10.0.0.1/24";

		[JsonPropertyName("standartUserIp")]
		public string StandartUserIp { get; set; } = "10.0.0.2/24";

		[JsonPropertyName("standartAllowedIpServer")]
		public string StandartAllowedIpServer { get; set; } = "0.0.0.0/0";

		[JsonPropertyName("standartListenPort")]
		public string StandartListenPort { get; set; } = "51830";

		[JsonPropertyName("standartServerPostUpArg")]
		public string StandartPostUpArg { get; set; } = "iptables -A FORWARD -i %i -j ACCEPT; iptables -A FORWARD -o %i -j ACCEPT; iptables -t nat -A POSTROUTING -o";

		[JsonPropertyName("standartServerPostDownArg")]
		public string StandartPostDownArg { get; set; } = "iptables -D FORWARD -i %i -j ACCEPT; iptables -D FORWARD -o %i -j ACCEPT; iptables -t nat -D POSTROUTING -o";
		#endregion
	}
}
