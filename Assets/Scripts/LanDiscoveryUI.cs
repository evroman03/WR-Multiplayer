using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LanDiscoveryUi : MonoBehaviour
{
    [Header("UI References")]
    public Transform serverListParent;
    public GameObject serverEntryPrefab;

    public TMP_InputField directIPField;
    public Button directConnectButton;

    public TMP_Text modeText;

    void Start()
    {
        modeText.text = LanDiscoveryManager.Instance.isServer
            ? "Server Mode"
            : "Client Mode";

        LanDiscoveryManager.Instance.OnServerListUpdated += RefreshServerList;

        directConnectButton.onClick.AddListener(OnDirectConnectPressed);
    }

    private void RefreshServerList(List<LanDiscoveryManager.DiscoveredServer> servers)
    {
        foreach (Transform c in serverListParent)
            Destroy(c.gameObject);

        foreach (var s in servers)
        {
            GameObject entry = Instantiate(serverEntryPrefab, serverListParent);
            entry.transform.Find("ServerNameText").GetComponent<TMP_Text>().text = "LAN Server";
            entry.transform.Find("ServerIPText").GetComponent<TMP_Text>().text = $"{s.ip}:{s.port}";

            entry.transform.Find("ConnectButton").GetComponent<Button>()
                .onClick.AddListener(() =>
                {
                    LanDiscoveryManager.Instance.ConnectToServer(s.ip);
                });
        }
    }

    private void OnDirectConnectPressed()
    {
        string ip = directIPField.text.Trim();
        if (string.IsNullOrEmpty(ip))
            return;

        LanDiscoveryManager.Instance.ConnectToServer(ip);
    }
}
