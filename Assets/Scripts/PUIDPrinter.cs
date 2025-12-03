using UnityEngine;
using PlayEveryWare.EpicOnlineServices;
using System.Collections; 

public class PUIDPrinter : MonoBehaviour
{
    void Start()
    {

        StartCoroutine(PUIDPrinterFunc());
    }

    IEnumerator PUIDPrinterFunc()
    {
        while (true)
        {
            if (EOSManager.Instance.GetLocalUserId() != null)
            {
                Debug.Log("FULL PUID = " + EOSManager.Instance.GetLocalUserId().ToString());
                break;
            }
            else
            {
                Debug.Log("PUIDPrinter: User not logged in yet.");
            }
            yield return new WaitForSeconds(3);
        }
    }
}
