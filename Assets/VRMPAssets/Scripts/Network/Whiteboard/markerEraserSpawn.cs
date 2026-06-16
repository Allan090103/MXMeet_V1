using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Server-side spawner for marker and eraser prefabs.
/// Place this on a GameObject in the scene (e.g., NetworkManager object). Assign prefabs.
/// Prefabs MUST be NetworkObject prefabs and registered in NetworkManager's Prefabs list.
/// </summary>
public class markerEraserSpawn : MonoBehaviour
{
    [Tooltip("The marker prefab (must contain NetworkObject).")]
    public GameObject markerPrefab;

    [Tooltip("The eraser prefab (must contain NetworkObject).")]
    public GameObject eraserPrefab;

    [Tooltip("Optional: spawn location for marker/eraser. If null, spawns at Vector3.zero.")]
    public Transform spawnRoot;

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("[markerEraserSpawn] NetworkManager not found in scene.");
            return;
        }

        if (NetworkManager.Singleton.IsServer)
        {
            // Spawn initial shared marker/eraser for lobby
            SpawnSharedItems();
            // Listen for future clients if you want to spawn per-client items
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        // Option: spawn an extra marker/eraser whenever a client connects
        // If you want per-client ownership: use SpawnWithOwnership(clientId)
        SpawnSharedItems();
    }

    public void SpawnSharedItems()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.Log("[markerEraserSpawn] Not server - won't spawn items.");
            return;
        }

        if (markerPrefab != null)
        {
            Vector3 pos = spawnRoot != null ? spawnRoot.position : Vector3.zero;
            var m = Instantiate(markerPrefab, pos, spawnRoot != null ? spawnRoot.rotation : Quaternion.identity);
            NetworkObject no = m.GetComponent<NetworkObject>();
            if (no == null)
            {
                Debug.LogError("[markerEraserSpawn] markerPrefab missing NetworkObject component.");
            }
            else
            {
                no.Spawn(); // visible to all clients
            }
        }

        if (eraserPrefab != null)
        {
            Vector3 pos = spawnRoot != null ? spawnRoot.position + Vector3.right * 0.2f : Vector3.zero + Vector3.right * 0.2f;
            var e = Instantiate(eraserPrefab, pos, spawnRoot != null ? spawnRoot.rotation : Quaternion.identity);
            NetworkObject noE = e.GetComponent<NetworkObject>();
            if (noE == null)
            {
                Debug.LogError("[markerEraserSpawn] eraserPrefab missing NetworkObject component.");
            }
            else
            {
                noE.Spawn();
            }
        }
    }
}
