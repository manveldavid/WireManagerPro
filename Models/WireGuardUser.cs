using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace WireManager.Models
{
    public class WireGuardUser
	{
		public override string ToString() => Name;
		public WireGuardUser() { }
		
		public bool IsConected() => !string.IsNullOrWhiteSpace(this.Ip);
		
		[JsonPropertyName("userId")]
		public int UserId { get; private set; } = -1;

		private string _ip = "";

        [JsonPropertyName("ip")]
        public string Ip { 
			get 
			{ 
				return _ip;
			} 
			set 
			{
				if (value.Contains('.') && value.Contains('/'))
				{
					_ip = value;
					UserId = GetUserIdFromIp(value);
				}
				else
				{
					_ip = "";
					UserId = -1;
				}
			} 
		}

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("pubKey")]
        public string PubKey { get; set; } = "";

        [JsonPropertyName("prvKey")]
        public string PrvKey { get; set; } = "";

		public static int GetNewUserId(List<WireGuardUser> users, WireGuardUser server)
		{
			var ExistedIp = users.Select(u => u.UserId);
			for (int i = 1; i < 256; i++)
				if (i != server.UserId && !ExistedIp.Contains(i))
					return i;
			return -1;
		}
		public static int GetUserIdFromIp(string ip)
		{
			if (!string.IsNullOrWhiteSpace(ip) &&
				ip.LastIndexOf('.') != -1 &&
				ip.LastIndexOf('/') != -1)
			{
				string strPos = ip.Substring(ip.LastIndexOf('.') + 1,
						  ip.LastIndexOf('/') - ip.LastIndexOf('.') - 1);
				return Convert.ToInt32(strPos);
			}
			else return -1;
		}
		public static string GetIpFromUserId(string standartIp, int userId)
		{
			string IpFromPos = standartIp.Substring(0, standartIp.LastIndexOf('.') + 1)
				+ userId.ToString()
				+ standartIp.Substring(standartIp.IndexOf('/'));
			return IpFromPos;
		}
	}
}
