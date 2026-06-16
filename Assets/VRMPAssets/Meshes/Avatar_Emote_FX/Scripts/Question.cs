using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class Question : NetworkBehaviour
{
    public GameObject questionFX;

    void Start()
    {
        questionFX.SetActive(false);
    }

    //void Update()
    //{
    //    if (!IsOwner) return;

    //    if (Input.GetKeyDown(KeyCode.B))
    //    {
    //        TriggerQuestionServerRpc();
    //    }
    //}

    public void OnButtonClick()
    {
        Debug.Log("[EMOTE] OnButtonClick called on Question");

        if (!IsOwner) return;

        TriggerQuestionServerRpc();
    }

    [ServerRpc]
    void TriggerQuestionServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        TriggerQuestionClientRpc(senderId);
    }

    [ClientRpc]
    void TriggerQuestionClientRpc(ulong sourceClientId)
    {
        if (OwnerClientId != sourceClientId) return;

        StartCoroutine(QuestionOn());
    }

    IEnumerator QuestionOn()
    {
        questionFX.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        questionFX.SetActive(false);
    }
}
