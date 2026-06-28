using System.Text.Json.Serialization;

namespace SlayTheSpire2.LAN.Multiplayer.Models
{
    [JsonSerializable(typeof(LanLobbyInfo))]
    public partial class LanLobbyInfoContext : JsonSerializerContext;

    public class LanLobbyInfo
    {
        [JsonPropertyName("host_name")] public string HostName { get; set; } = string.Empty;
        [JsonPropertyName("port")] public ushort Port { get; set; }
        [JsonPropertyName("max_players")] public int MaxPlayers { get; set; }
        [JsonPropertyName("mode")] public string Mode { get; set; } = string.Empty;

        [JsonIgnore] public string Address { get; set; } = string.Empty;
    }
}
