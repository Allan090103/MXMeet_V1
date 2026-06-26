using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MXMeet.VRMeetUp
{
    /// <summary>
    /// Presents selected VRMeetUp prefabs inside the MXMeet MR view.
    /// In a Quest build these references should be assigned in the inspector.
    /// In editor play mode, the presenter can load the same prefabs by asset path.
    /// </summary>
    public class VRMeetUpAppPresenter : MonoBehaviour
    {
        [Header("VRMeetUp App Prefabs")]
        public GameObject roomInfoPrefab;
        public GameObject playerListPrefab;
        public GameObject playerMenuPrefab;
        public GameObject messageBoardPrefab;
        public GameObject objectDispenserPrefab;
        public GameObject musicPlayerPrefab;
        public GameObject staticPersistentUiPrefab;
        public GameObject tablePrefab;

        private GameObject _root;
        private readonly List<GameObject> _instances = new();

        public void Show(Vector3 anchorPosition, Quaternion anchorRotation)
        {
            if (_root == null)
            {
                _root = new GameObject("VRMeetUp App Surface");
                DontDestroyOnLoad(_root);
            }

            _root.SetActive(true);
            _root.transform.SetPositionAndRotation(anchorPosition, anchorRotation);

            if (_instances.Count > 0) return;

            SpawnPersistentUi();
            SpawnAppPrefab(ResolvePrefab(ref tablePrefab, "Assets/VRMPAssets/Prefabs/Environment/Table.prefab"), new Vector3(0.0f, -0.25f, 0.75f), Quaternion.identity, Vector3.one * 0.22f);
            SpawnAppPrefab(ResolvePrefab(ref roomInfoPrefab, "Assets/VRMPAssets/Prefabs/UIPrefabs/MenuUI/Room Info UI.prefab"), new Vector3(0.0f, 0.65f, 1.05f), Quaternion.identity, Vector3.one);
            SpawnAppPrefab(ResolvePrefab(ref playerListPrefab, "Assets/VRMPAssets/Prefabs/UIPrefabs/MenuUI/PlayerListUI/Player_List_UI.prefab"), new Vector3(-0.85f, 0.25f, 1.05f), Quaternion.Euler(0.0f, 18.0f, 0.0f), Vector3.one);
            SpawnAppPrefab(ResolvePrefab(ref messageBoardPrefab, "Assets/VRMPAssets/Prefabs/UIPrefabs/MenuUI/MessageBoard/Message Board UI.prefab"), new Vector3(0.85f, 0.25f, 1.05f), Quaternion.Euler(0.0f, -18.0f, 0.0f), Vector3.one);
            SpawnAppPrefab(ResolvePrefab(ref playerMenuPrefab, "Assets/VRMPAssets/Prefabs/UIPrefabs/MenuUI/Player_Menu_UI.prefab"), new Vector3(-0.55f, -0.1f, 0.95f), Quaternion.Euler(0.0f, 14.0f, 0.0f), Vector3.one);
            SpawnAppPrefab(ResolvePrefab(ref objectDispenserPrefab, "Assets/VRMPAssets/Prefabs/UIPrefabs/ObjectDispenser/Object Dispenser UI.prefab"), new Vector3(0.55f, -0.1f, 0.95f), Quaternion.Euler(0.0f, -14.0f, 0.0f), Vector3.one);
            SpawnAppPrefab(ResolvePrefab(ref musicPlayerPrefab, "Assets/VRMPAssets/Prefabs/UIPrefabs/MenuUI/Music Player UI.prefab"), new Vector3(0.0f, -0.45f, 0.9f), Quaternion.identity, Vector3.one);
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void SpawnPersistentUi()
        {
            GameObject prefab = ResolvePrefab(ref staticPersistentUiPrefab, "Assets/VRMPAssets/Prefabs/UIPrefabs/Static Persistent UI Objects.prefab");
            if (prefab == null || GameObject.Find(prefab.name) != null) return;

            GameObject instance = Instantiate(prefab);
            instance.name = prefab.name;
            DontDestroyOnLoad(instance);
            _instances.Add(instance);
        }

        private void SpawnAppPrefab(GameObject prefab, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            if (prefab == null) return;

            GameObject instance = Instantiate(prefab, _root.transform);
            instance.name = prefab.name;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, localScale);
            _instances.Add(instance);
        }

        private GameObject ResolvePrefab(ref GameObject prefab, string editorAssetPath)
        {
            if (prefab != null) return prefab;

#if UNITY_EDITOR
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(editorAssetPath);
            if (prefab == null)
                Debug.LogWarning($"[VRMeetUpAppPresenter] Missing prefab: {editorAssetPath}");
#endif
            return prefab;
        }
    }
}
