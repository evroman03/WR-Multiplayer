using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerEntryUI : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text detailText;
    public Button connectButton;

    private LanDiscoveryManager.DiscoveredServer _info;
    private System.Action<LanDiscoveryManager.DiscoveredServer> _onConnect;

    public void Initialize(
        LanDiscoveryManager.DiscoveredServer info,
        System.Action<LanDiscoveryManager.DiscoveredServer> onConnect)
    {
        _info = info;
        _onConnect = onConnect;
        SetInfo(info);
        connectButton.onClick.AddListener(OnConnectPressed);
    }

    public void SetInfo(LanDiscoveryManager.DiscoveredServer info)
    {
        _info = info;

        titleText.text = $"LAN Server";   // Or info.displayName if you add naming later

        detailText.text =
            $"IP: {info.ip}   Port: {info.port}   Last Seen: {info.lastSeen:F1}s ago";
    }

    private void OnConnectPressed()
    {
        _onConnect?.Invoke(_info);
    }

    private void OnDestroy()
    {
        connectButton.onClick.RemoveListener(OnConnectPressed);
    }
}
