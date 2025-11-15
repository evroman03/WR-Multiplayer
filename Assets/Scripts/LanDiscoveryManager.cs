using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using FishNet.Transporting.Tugboat;
using FishNet.Managing;

public class LanDiscoveryManager : MonoBehaviour
{
    public static LanDiscoveryManager Instance;

    [Header("Settings")]
    public bool isServer;
    public int broadcastPort = 47777;
    public int gamePort = 7770;
    public float broadcastInterval = 1.0f;

    [Header("References")]
    public NetworkManager networkManager;
    public Tugboat tugboat;

    // Event: UI listens to this
    public event Action<List<DiscoveredServer>> OnServerListUpdated;

    private Dictionary<string, DiscoveredServer> servers = new();
    private bool running;
    private UdpClient udp;

    // =========================
    //  PUBLIC STRUCT
    // =========================
    public struct DiscoveredServer
    {
        public string ip;
        public int port;
        public float lastSeen;
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        running = true;

        if (isServer)
        {
            PrintConnectionInfo();
            await StartServerBroadcast();
        }
        else
        {
            await StartClientListen();
        }
    }

    // ========================
    // SERVER: BROADCAST LOOP
    // ========================
    private async Task StartServerBroadcast()
    {
        udp = new UdpClient();
        udp.EnableBroadcast = true;

        List<IPAddress> broadcastTargets = GetSubnetBroadcastAddresses();

        while (running)
        {
            string msg = $"SERVER:{gamePort}";
            byte[] data = Encoding.UTF8.GetBytes(msg);

            foreach (var addr in broadcastTargets)
            {
                try
                {
                    await udp.SendAsync(data, data.Length,
                        new IPEndPoint(addr, broadcastPort));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Broadcast failed: " + ex.Message);
                }
            }

            await Task.Delay((int)(broadcastInterval * 1000));
        }
    }

    // ========================
    // CLIENT: LISTEN LOOP
    // ========================
    private async Task StartClientListen()
    {
        udp = new UdpClient();
        udp.Client.SetSocketOption(SocketOptionLevel.Socket,
            SocketOptionName.ReuseAddress, true);

        udp.Client.Bind(new IPEndPoint(IPAddress.Any, broadcastPort));

        while (running)
        {
            try
            {
                UdpReceiveResult result = await udp.ReceiveAsync();
                string msg = Encoding.UTF8.GetString(result.Buffer);

                if (msg.StartsWith("SERVER:"))
                {
                    string ip = result.RemoteEndPoint.Address.ToString();
                    int port = int.Parse(msg.Split(':')[1]);

                    if (!servers.ContainsKey(ip))
                        Debug.Log($"LAN server discovered: {ip}:{port}");

                    servers[ip] = new DiscoveredServer()
                    {
                        ip = ip,
                        port = port,
                        lastSeen = Time.time
                    };

                    OnServerListUpdated?.Invoke(GetServerList());
                }
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.LogError("LAN listen error: " + ex);
            }
        }
    }

    // ================================
    // GET SUBNET BROADCAST ADDRESSES
    // ================================
    private List<IPAddress> GetSubnetBroadcastAddresses()
    {
        List<IPAddress> result = new()
        {
            IPAddress.Broadcast // 255.255.255.255
        };

        foreach (var ni in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ni.AddressFamily != AddressFamily.InterNetwork)
                continue;

            byte[] bytes = ni.GetAddressBytes();
            bytes[3] = 255;
            result.Add(new IPAddress(bytes));
        }

        return result;
    }

    // =====================================
    // SERVER MODE: PRINT JOIN INSTRUCTIONS
    // =====================================
    private void PrintConnectionInfo()
    {
        Debug.Log("=== LAN DISCOVERY SERVER ===");

        foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                Debug.Log($"Local IP: {ip}");
        }

        Debug.Log($"Clients can join using port {gamePort}");
        Debug.Log($"Broadcasting on port {broadcastPort}");
    }

    // ====================
    // PUBLIC API
    // ====================
    public List<DiscoveredServer> GetServerList()
    {
        // prune servers older than 5 seconds
        List<string> expired = new();
        foreach (var kvp in servers)
        {
            if (Time.time - kvp.Value.lastSeen > 5)
                expired.Add(kvp.Key);
        }
        foreach (var ip in expired)
            servers.Remove(ip);

        return new List<DiscoveredServer>(servers.Values);
    }

    public void ConnectToServer(string ip)
    {
        Debug.Log($"Trying to connect to {ip}...");
        tugboat.SetClientAddress(ip);
        networkManager.ClientManager.StartConnection();
    }

    private void OnDestroy()
    {
        running = false;
        try { udp?.Close(); } catch { }
    }
}
