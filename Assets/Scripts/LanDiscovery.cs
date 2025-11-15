using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Transporting.Tugboat;
using FishNet.Managing;

public class LanDiscoveryUI : MonoBehaviour
{
    [Header("Settings")]
    public bool isServer;
    public int broadcastPort = 47777;
    public int gamePort = 7770;
    public float broadcastInterval = 2f;

    [Header("UI")]
    public TMP_Text statusText;
    public Button connectButton;

    [Header("References")]
    public Tugboat tugboat;
    public NetworkManager networkManager;

    private bool discovering;
    private string foundIP;

    private UdpClient udp;

    async void Start()
    {
        statusText.text = isServer ? "Server Mode" : "Client Mode";
        connectButton.interactable = false;

        discovering = true;

        if (isServer)
            await StartServerBroadcast();
        else
            await StartClientListener();
    }

    private async Task StartServerBroadcast()
    {
        udp = new UdpClient();
        udp.EnableBroadcast = true;

        while (discovering)
        {
            try
            {
                string msg = $"SERVER:{gamePort}";
                byte[] data = Encoding.UTF8.GetBytes(msg);

                await udp.SendAsync(
                    data,
                    data.Length,
                    new IPEndPoint(IPAddress.Broadcast, broadcastPort)
                );

                Debug.Log("Broadcasting...");
            }
            catch (Exception ex)
            {
                Debug.LogError("LAN broadcast error: " + ex);
            }

            await Task.Delay((int)(broadcastInterval * 1000));
        }
    }

    private async Task StartClientListener()
    {
        udp = new UdpClient();
        udp.EnableBroadcast = true;

        // Allows multiple listeners or mixed interface binding
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        // Bind to ANY IP, not a specific NIC
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, broadcastPort));

        while (discovering)
        {
            try
            {
                UdpReceiveResult res = await udp.ReceiveAsync();
                string msg = Encoding.UTF8.GetString(res.Buffer);

                if (msg.StartsWith("SERVER:"))
                {
                    string port = msg.Split(':')[1];

                    foundIP = res.RemoteEndPoint.Address.ToString();
                    statusText.text = $"Found server: {foundIP}:{port}";

                    connectButton.interactable = true;
                }
            }
            catch (ObjectDisposedException)
            {
                // socket closed intentionally
                break;
            }
            catch (Exception ex)
            {
                Debug.LogError("LAN listen error: " + ex);
            }
        }
    }
    private void OnDestroy()
    {
        discovering = false;

        try { udp?.Close(); } catch { }
    }
    public void OnJoinPressed()
    {
        if (string.IsNullOrEmpty(foundIP))
        {
            statusText.text = "No server found.";
            return;
        }

        tugboat.SetClientAddress(foundIP);
        statusText.text = "Connecting to " + foundIP + "...";

        networkManager.ClientManager.StartConnection();
    }
}
