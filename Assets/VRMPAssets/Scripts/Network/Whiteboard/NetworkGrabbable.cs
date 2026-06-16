using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class NetworkedGrabbable : NetworkBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    new void OnDestroy()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Transfer ownership to whoever grabs it
        if (IsServer || NetworkManager.Singleton.IsHost)
        {
            // Host already owns everything
            return;
        }

        if (!IsOwner)
        {
            // Ask the server to transfer ownership to this client
            RequestOwnershipServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // Optional: give ownership back to server
        if (IsOwner)
        {
            ReturnOwnershipToServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestOwnershipServerRpc(ulong clientId)
    {
        GetComponent<NetworkObject>().ChangeOwnership(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    void ReturnOwnershipToServerRpc()
    {
        GetComponent<NetworkObject>().RemoveOwnership(); // Host/server takes over
    }
}
