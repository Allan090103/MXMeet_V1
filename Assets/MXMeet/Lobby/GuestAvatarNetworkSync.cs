using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace MXMeet.Lobby
{
    /// <summary>
    /// Attach to the GuestAvatar prefab.
    /// Synchronises avatar position and rotation across the NGO network
    /// so all lobby participants see each other's avatars in the correct position.
    ///
    /// Requires:
    ///   - NetworkObject component on the same GameObject
    ///   - NetworkTransform component on the same GameObject
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    public class GuestAvatarNetworkSync : NetworkBehaviour
    {
        [Header("Smoothing")]
        public float positionSmoothing = 10f;
        public float rotationSmoothing = 10f;

        // Networked variables
        private NetworkVariable<Vector3>    _networkPosition = new(Vector3.zero,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> _networkRotation = new(Quaternion.identity,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<int>        _skinIndex       = new(0,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        [Header("Avatar Reference")]
        public Transform headTransform; // assign the avatar head bone

        private void Update()
        {
            if (IsOwner)
            {
                // Owner: send current camera (head) position to network
                Transform cam = Camera.main?.transform;
                if (cam != null)
                {
                    _networkPosition.Value = cam.position;
                    _networkRotation.Value = cam.rotation;
                }
            }
            else
            {
                // Non-owner: smoothly move to networked position
                transform.position = Vector3.Lerp(transform.position, _networkPosition.Value,
                                                  Time.deltaTime * positionSmoothing);
                transform.rotation = Quaternion.Slerp(transform.rotation, _networkRotation.Value,
                                                       Time.deltaTime * rotationSmoothing);
            }
        }

        /// <summary>Call on owner after avatar skin is chosen to sync skin to others.</summary>
        public void SetSkinIndex(int index)
        {
            if (IsOwner) _skinIndex.Value = index;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _skinIndex.OnValueChanged += OnSkinChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _skinIndex.OnValueChanged -= OnSkinChanged;
        }

        private void OnSkinChanged(int previous, int current)
        {
            // Update avatar skin visual based on index
            // Map index to skin names — extend as needed
            string[] skins = { "default", "robot", "astronaut" };
            if (current >= 0 && current < skins.Length)
            {
                foreach (Transform child in transform)
                    child.gameObject.SetActive(child.name == skins[current]);
            }
        }
    }
}
