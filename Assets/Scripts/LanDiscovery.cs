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
using FishNet.Example;

public class LanDiscoveryUI : MonoBehaviour
{
    [Header("Settings")]
    public bool isServer;
    public int broadcastPort = 47777;
    public int gamePort = 7770;
    public float broadcastInterval = 2f;

    [Header("UI")]
    public TMP_Text statusText;
    public Button connectButton;   // button in inspector

    [Header("References")]
    public Tugboat tugboat;        // drag your Tugboat transport here
    public NetworkManager networkManager;
    public NetworkHudCanvases networkHudCanvas;
    private bool discovering;
    private string foundIP = null;

    void Start()
    {
        statusText.text = isServer ? "Server Mode" : "Client Mode";

        if (isServer)
            _ = ServerBroadcastLoop();
        else
            _ = ClientListenLoop();

        // Disable the button until a server is found
        connectButton.interactable = false;
    }

    // =====================================================
    // SERVER BROADCAST
    // =====================================================
    private async Task ServerBroadcastLoop()
    {
        UdpClient sender = new UdpClient();
        sender.EnableBroadcast = true;

        discovering = true;

        while (discovering)
        {
            try
            {
                string msg = "SERVER:" + gamePort;
                byte[] data = Encoding.UTF8.GetBytes(msg);

                await sender.SendAsync(data, data.Length,
                    new IPEndPoint(IPAddress.Broadcast, broadcastPort));

                statusText.text = "Broadcasting...";
                Debug.Log("Broadcasting " + msg);
            }
            catch (Exception ex)
            {
                statusText.text = "Broadcast error: " + ex.Message;
                Debug.Log("Broadcast error: " + ex.Message);
            }

            await Task.Delay((int)(broadcastInterval * 1000));
        }

        sender.Close();
    }

    // =====================================================
    // CLIENT LISTEN
    // =====================================================
    private async Task ClientListenLoop()
    {
        UdpClient listener = new UdpClient(broadcastPort);
        discovering = true;

        while (discovering)
        {
            try
            {
                UdpReceiveResult result = await listener.ReceiveAsync();
                string msg = Encoding.UTF8.GetString(result.Buffer);

                if (msg.StartsWith("SERVER:"))
                {
                    string portStr = msg.Split(':')[1];
                    foundIP = result.RemoteEndPoint.Address.ToString();

                    statusText.text = $"Found server: {foundIP}:{portStr}";
                    connectButton.interactable = true;
                }
            }
            catch
            {
                // socket closed
                break;
            }
        }

        listener.Close();
    }

    // =====================================================
    // BUTTON: TRY CONNECT
    // =====================================================
    public void OnJoinPressed()
    {
        if (foundIP == null)
        {
            statusText.text = "No server found yet.";
            return;
        }

        statusText.text = "Connecting to " + foundIP + "...";
        tugboat.SetClientAddress(foundIP);
        networkHudCanvas.OnClick_Client();

    }

    void OnDestroy()
    {
        discovering = false;
    }
}
