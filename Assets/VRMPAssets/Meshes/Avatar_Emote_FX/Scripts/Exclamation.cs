using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class Exclamation : NetworkBehaviour
{
    public GameObject exclamationFX;

    void Start()
    {
        exclamationFX.SetActive(false);
    }

    //void Update()
    //{
    //    if (!IsOwner) return;

    //    if (Input.GetKeyDown(KeyCode.Z))
    //    {
    //        TriggerExclamationServerRpc();
    //    }
    //}

    public void OnButtonClick()
    {
        Debug.Log("[EMOTE] OnButtonClick called. IsOwner = " + IsOwner + ", IsSpawned = " + IsSpawned);

        if (!IsOwner) return;

        TriggerExclamationServerRpc();
    }

    [ServerRpc]
    void TriggerExclamationServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        TriggerExclamationClientRpc(senderId);
    }

    [ClientRpc]
    void TriggerExclamationClientRpc(ulong sourceClientId)
    {
        if (OwnerClientId != sourceClientId) return;

        StartCoroutine(ExclamationOn());
    }

    IEnumerator ExclamationOn()
    {
        exclamationFX.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        exclamationFX.SetActive(false);
    }
}
