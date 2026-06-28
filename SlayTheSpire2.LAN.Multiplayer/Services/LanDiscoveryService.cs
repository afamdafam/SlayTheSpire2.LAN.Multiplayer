using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;
using SlayTheSpire2.LAN.Multiplayer.Models;

namespace SlayTheSpire2.LAN.Multiplayer.Services
{
    internal class LanDiscoveryService
    {
        private const ushort DiscoveryPort = 33772;
        private const string QueryToken = "STS2LAN_QUERY";
        private const string ResponsePrefix = "STS2LAN_PONG:";

        private static readonly Lazy<LanDiscoveryService> Lazy = new(() => new LanDiscoveryService());

        public static LanDiscoveryService Instance => Lazy.Value;

        private UdpClient? _announceClient;
        private CancellationTokenSource? _announceCts;

        private LanDiscoveryService()
        {
        }

        public void StartHosting(ushort gamePort, int maxPlayers, string mode)
        {
            StopHosting();

            UdpClient announceClient;

            try
            {
                announceClient = new UdpClient(AddressFamily.InterNetwork) { EnableBroadcast = true };
                announceClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                announceClient.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));
            }
            catch (Exception ex)
            {
                Log.Warn($"LAN discovery: failed to bind announce socket on port {DiscoveryPort}: {ex.Message}");
                return;
            }

            _announceClient = announceClient;
            _announceCts = new CancellationTokenSource();
            _ = AnnounceLoop(announceClient, gamePort, maxPlayers, mode, _announceCts.Token);
        }

        public void StopHosting()
        {
            _announceCts?.Cancel();
            _announceCts?.Dispose();
            _announceCts = null;

            _announceClient?.Dispose();
            _announceClient = null;
        }

        private static async Task AnnounceLoop(UdpClient client, ushort gamePort, int maxPlayers, string mode,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult result;

                try
                {
                    result = await client.ReceiveAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Log.Debug($"LAN discovery: announce receive error: {ex.Message}");
                    continue;
                }

                if (Encoding.UTF8.GetString(result.Buffer) != QueryToken)
                    continue;

                var hostName = SettingsService.Instance.SettingsModel.PlayerName;

                var info = new LanLobbyInfo
                {
                    HostName = string.IsNullOrWhiteSpace(hostName) ? "Host" : hostName,
                    Port = gamePort,
                    MaxPlayers = maxPlayers,
                    Mode = mode
                };

                var json = JsonSerializer.Serialize(info, LanLobbyInfoContext.Default.LanLobbyInfo);
                var payload = Encoding.UTF8.GetBytes(ResponsePrefix + json);

                try
                {
                    await client.SendAsync(payload, payload.Length, result.RemoteEndPoint);
                }
                catch (Exception ex)
                {
                    Log.Debug($"LAN discovery: announce send error: {ex.Message}");
                }
            }
        }

        public async Task BrowseAsync(Action<LanLobbyInfo> onLobbyFound, CancellationToken cancellationToken)
        {
            UdpClient client;

            try
            {
                client = new UdpClient(0) { EnableBroadcast = true };
            }
            catch (Exception ex)
            {
                Log.Warn($"LAN discovery: failed to open browse socket: {ex.Message}");
                return;
            }

            using (client)
            {
                var listenTask = ListenForResponses(client, onLobbyFound, cancellationToken);

                var queryBytes = Encoding.UTF8.GetBytes(QueryToken);
                var broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);

                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await client.SendAsync(queryBytes, queryBytes.Length, broadcastEndPoint);
                        await Task.Delay(1000, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Log.Debug($"LAN discovery: browse send error: {ex.Message}");
                }

                await listenTask;
            }
        }

        private static async Task ListenForResponses(UdpClient client, Action<LanLobbyInfo> onLobbyFound,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult result;

                try
                {
                    result = await client.ReceiveAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Log.Debug($"LAN discovery: browse receive error: {ex.Message}");
                    continue;
                }

                var text = Encoding.UTF8.GetString(result.Buffer);

                if (!text.StartsWith(ResponsePrefix, StringComparison.Ordinal))
                    continue;

                LanLobbyInfo? info;

                try
                {
                    info = JsonSerializer.Deserialize(text[ResponsePrefix.Length..],
                        LanLobbyInfoContext.Default.LanLobbyInfo);
                }
                catch (Exception ex)
                {
                    Log.Debug($"LAN discovery: failed to parse response: {ex.Message}");
                    continue;
                }

                if (info == null)
                    continue;

                info.Address = result.RemoteEndPoint.Address.ToString();

                onLobbyFound(info);
            }
        }
    }
}
