using System.IO;
using WireManager.Models;

namespace WireManager.Common
{
    public class BatFileCreator
    {
        public static void CreateBatFiles(WireManagerConfig config)
        {
            Directory.CreateDirectory("bats");
            File.WriteAllText(WireManagerConfig.BatUploadSshKeyToServer,
                    $"""
					ssh {config.ServerUser}@{config.ServerIp} mkdir ~/.ssh
					scp %userprofile%/.ssh/id_rsa.pub {config.ServerUser}@{config.ServerIp}:~/.ssh/authorized_keys
					""");
            File.WriteAllText(WireManagerConfig.BatConnectToServer,
                    $"""
                    ssh {config.ServerUser}@{config.ServerIp}
                    """);
            File.WriteAllText(WireManagerConfig.BatInstallWireguardToServer,
                    $"""
                    ssh {config.ServerUser}@{config.ServerIp}
                    apt update && apt upgrade -y
                    apt install -y wireguard
                    wg genkey | tee /etc/wireguard/privatekey | wg pubkey | tee /etc/wireguard/publickey
                    chmod 600 /etc/wireguard/privatekey
                    echo "net.ipv4.ip_forward=1" >> /etc/sysctl.conf
                    systemctl enable wg-quick@wg0.service
                    systemctl start wg-quick@wg0.service
                    systemctl status wg-quick@wg0.service
                    sysctl -p
                    """);
            File.WriteAllText(WireManagerConfig.BatCreateSshKey,
                    $"""
                    ssh-keygen
                    """);
        }
    }
}
