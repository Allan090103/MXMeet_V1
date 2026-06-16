using Unity.Netcode;
using UnityEngine;
using XRMultiplayer;

public class EmoteButtonHandler : MonoBehaviour
{
    public enum EmoteType
    {
        LightBulb,
        Exclamation,
        Question,
        Stunned
    }

    public EmoteType emoteToTrigger;

    public void TriggerEmote()
    {
        Debug.Log("[EMOTE] Button clicked! Trying to trigger emote.");

        if (!NetworkManager.Singleton.IsConnectedClient)
        {
            Debug.LogWarning("Not connected to network.");
            return;
        }

        var playerObj = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (playerObj == null)
        {
            Debug.LogWarning("Local player object not found.");
            return;
        }

        if (XRINetworkPlayer.LocalPlayer == null)
        {
            Debug.LogWarning("[EMOTE] Local XRINetworkPlayer not ready.");
            return;
        }

        Debug.Log($"[EMOTE] XRINetworkPlayer ref: {XRINetworkPlayer.LocalPlayer.gameObject.name}, IsOwner: {XRINetworkPlayer.LocalPlayer.IsOwner}, IsSpawned: {XRINetworkPlayer.LocalPlayer.IsSpawned}");

        // Find the correct emote script based on selected type
        switch (emoteToTrigger)
        {
            case EmoteType.LightBulb:
                XRINetworkPlayer.LocalPlayer.GetComponentInChildren<Light_Bulb>()?.OnButtonClick();
                break;

            case EmoteType.Exclamation:
                XRINetworkPlayer.LocalPlayer.GetComponentInChildren<Exclamation>()?.OnButtonClick();
                break;

            case EmoteType.Question:
                XRINetworkPlayer.LocalPlayer.GetComponentInChildren<Question>()?.OnButtonClick();
                break;

            case EmoteType.Stunned:
                XRINetworkPlayer.LocalPlayer.GetComponentInChildren<Stunned_Stars>()?.OnButtonClick();
                break;
        }
    }
}
