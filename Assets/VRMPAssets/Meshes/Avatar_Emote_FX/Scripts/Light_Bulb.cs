using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class Light_Bulb : NetworkBehaviour
{
    public GameObject bulbFX;

    void Start()
    {
        bulbFX.SetActive(false);
    }

    //void Update()
    //{
    //    if (!IsOwner) return;

    //    if (Input.GetKeyDown(KeyCode.X))
    //    {
    //        TriggerBulbServerRpc();
    //    }
    //}

    public void OnButtonClick()
    {
        Debug.Log("[EMOTE] OnButtonClick called on Light_Bulb");

        if (!IsOwner) return;

        TriggerBulbServerRpc();
    }

    [ServerRpc]
    void TriggerBulbServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        TriggerBulbClientRpc(senderId);
    }

    [ClientRpc]
    void TriggerBulbClientRpc(ulong sourceClientId)
    {
        if (OwnerClientId != sourceClientId) return;

        StartCoroutine(BulbOn());
    }

    IEnumerator BulbOn()
    {
        bulbFX.SetActive(true);
        yield return new WaitForSeconds(3.0f);
        bulbFX.SetActive(false);
    }
}
