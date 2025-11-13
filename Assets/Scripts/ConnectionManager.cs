using FishNet.Managing;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    [SerializeField] NetworkManager networkManager;

    // A host is simply a server and a client, so start them both.
    public void StartHost()
    {
        StartServer();
        StartClient();
    }

    // The server can be started directly from the ServerManager or Transport
    public void StartServer()
    {
        networkManager.ServerManager.StartConnection();
    }

    // The client can be started directly from the ClientManager or Transport
    public void StartClient()
    {
        networkManager.ClientManager.StartConnection();
    }

    // This is set on the Transport to indicate where the client should connect.
    public void SetIPAddress(string text)
    {
        networkManager.TransportManager.Transport.SetClientAddress(text);
    }
}
