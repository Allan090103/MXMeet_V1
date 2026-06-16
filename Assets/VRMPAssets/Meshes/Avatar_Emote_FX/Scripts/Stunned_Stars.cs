using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class Stunned_Stars : NetworkBehaviour
{
    public GameObject stunnedStarsFX;

    void Start()
    {
        stunnedStarsFX.SetActive(false);
    }

    //void Update()
    //{
    //    // Let owner press their button
    //    if (!IsOwner) return;

    //    if (Input.GetKeyDown(KeyCode.C))
    //    {
    //        RequestEmoteServerRpc();
    //    }
    //}

    public void OnButtonClick()
    {
        Debug.Log("[EMOTE] OnButtonClick called on Stunned");

        if (!IsOwner) return;

        RequestEmoteServerRpc();
    }

    [ServerRpc]
    void RequestEmoteServerRpc(ServerRpcParams rpcParams = default)
    {
        // Broadcast to everyone: show emote on the owner
        ulong senderId = rpcParams.Receive.SenderClientId;
        ShowEmoteClientRpc(senderId);
    }

    [ClientRpc]
    void ShowEmoteClientRpc(ulong sourceClientId)
    {
        // This instance will only activate the emote if it's owned by the sender
        if (OwnerClientId != sourceClientId) return;

        StartCoroutine(Stunned());
    }

    IEnumerator Stunned()
    {
        stunnedStarsFX.SetActive(true);
        yield return new WaitForSeconds(6.0f);
        stunnedStarsFX.SetActive(false);
    }
}
