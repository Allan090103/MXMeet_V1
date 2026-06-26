using UnityEngine;

namespace MXMeet.VRMeetUp
{
    /// <summary>
    /// Editor/Standalone fallback that gives the MR scenes a visible room-like
    /// environment when real Quest passthrough is not available.
    /// </summary>
    public class MRPreviewEnvironment : MonoBehaviour
    {
        private GameObject _root;
        private GameObject _anchorMarker;
        private GameObject _previewPanel;

        public void Show()
        {
            if (_root != null)
            {
                _root.SetActive(true);
                return;
            }

            _root = new GameObject("MR Preview Environment");
            _root.transform.SetParent(transform, false);

            CreateFloor();
            CreateBackWall();
            CreateSideWalls();
            CreateGrid();
            CreateRoomProps();
            ConfigureCamera();
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        public GameObject PlaceAnchor(Vector3 position)
        {
            Show();

            if (_anchorMarker == null)
            {
                _anchorMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _anchorMarker.name = "MR Preview Anchor";
                _anchorMarker.transform.SetParent(_root.transform, false);
                _anchorMarker.transform.localScale = new Vector3(0.18f, 0.02f, 0.18f);
                SetMaterial(_anchorMarker, new Color(0.0f, 0.85f, 1.0f, 0.85f), "MR Preview Anchor Material");
            }

            _anchorMarker.transform.position = position;
            _anchorMarker.SetActive(true);
            CreatePreviewPanel(position);
            return _anchorMarker;
        }

        private void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "MR Preview Floor";
            floor.transform.SetParent(_root.transform, false);
            floor.transform.localScale = new Vector3(3.2f, 1.0f, 3.2f);
            SetMaterial(floor, new Color(0.18f, 0.19f, 0.18f, 1.0f), "MR Preview Floor Material");
        }

        private void CreateBackWall()
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "MR Preview Back Wall";
            wall.transform.SetParent(_root.transform, false);
            wall.transform.position = new Vector3(0.0f, 1.5f, 4.0f);
            wall.transform.localScale = new Vector3(6.5f, 3.0f, 0.05f);
            SetMaterial(wall, new Color(0.34f, 0.36f, 0.37f, 1.0f), "MR Preview Wall Material");
        }

        private void CreateSideWalls()
        {
            CreateSideWall("MR Preview Left Wall", -3.2f);
            CreateSideWall("MR Preview Right Wall", 3.2f);
        }

        private void CreateSideWall(string name, float x)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(_root.transform, false);
            wall.transform.position = new Vector3(x, 1.5f, 1.0f);
            wall.transform.localScale = new Vector3(0.05f, 3.0f, 6.0f);
            SetMaterial(wall, new Color(0.28f, 0.30f, 0.31f, 1.0f), name + " Material");
        }

        private void CreateGrid()
        {
            Material lineMaterial = CreatePreviewMaterial(new Color(0.0f, 0.85f, 1.0f, 0.28f), "MR Preview Grid Material", true);

            for (int i = -6; i <= 6; i++)
            {
                CreateLine("MR Preview Grid X", new Vector3(i * 0.5f, 0.01f, -2.5f), new Vector3(i * 0.5f, 0.01f, 4.0f), lineMaterial);
                CreateLine("MR Preview Grid Z", new Vector3(-3.0f, 0.012f, i * 0.5f), new Vector3(3.0f, 0.012f, i * 0.5f), lineMaterial);
            }
        }

        private void CreateRoomProps()
        {
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "MR Preview Table";
            table.transform.SetParent(_root.transform, false);
            table.transform.position = new Vector3(-1.2f, 0.42f, 2.0f);
            table.transform.localScale = new Vector3(1.2f, 0.08f, 0.75f);
            SetMaterial(table, new Color(0.16f, 0.23f, 0.24f, 1.0f), "MR Preview Table Material");

            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "MR Preview Wall Screen";
            screen.transform.SetParent(_root.transform, false);
            screen.transform.position = new Vector3(1.15f, 1.55f, 3.96f);
            screen.transform.localScale = new Vector3(1.5f, 0.85f, 0.03f);
            SetMaterial(screen, new Color(0.03f, 0.06f, 0.08f, 1.0f), "MR Preview Screen Material");
        }

        private void CreatePreviewPanel(Vector3 anchorPosition)
        {
            if (_previewPanel == null)
            {
                _previewPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _previewPanel.name = "VRMeetUp MR Preview Panel";
                _previewPanel.transform.SetParent(_root.transform, false);
                SetMaterial(_previewPanel, new Color(0.02f, 0.10f, 0.14f, 0.96f), "MR Preview Panel Material");
            }

            _previewPanel.transform.position = anchorPosition + new Vector3(0.0f, 0.55f, 0.0f);
            _previewPanel.transform.localScale = new Vector3(1.25f, 0.7f, 0.04f);
            _previewPanel.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            _previewPanel.SetActive(true);
        }

        private void CreateLine(string name, Vector3 start, Vector3 end, Material material)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(_root.transform, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = 0.01f;
            line.endWidth = 0.01f;
            line.useWorldSpace = false;
            line.material = material;
        }

        private void ConfigureCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.08f, 1.0f);
            cam.transform.position = new Vector3(0.0f, 1.6f, -1.2f);
            cam.transform.rotation = Quaternion.Euler(8.0f, 0.0f, 0.0f);
        }

        private void SetMaterial(GameObject target, Color color, string materialName)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null) return;

            renderer.material = CreatePreviewMaterial(color, materialName, false);
        }

        private Material CreatePreviewMaterial(Color color, string materialName, bool preferUnlit)
        {
            Shader shader = FindPreviewShader(preferUnlit);
            Material material = new Material(shader);
            material.name = materialName;

            ApplyMaterialColor(material, color);

            if (color.a < 0.99f)
            {
                ConfigureTransparentMaterial(material);
            }

            return material;
        }

        private Shader FindPreviewShader(bool preferUnlit)
        {
            string[] shaderNames = preferUnlit
                ? new[]
                {
                    "Universal Render Pipeline/Unlit",
                    "Universal Render Pipeline/Lit",
                    "Sprites/Default",
                    "Unlit/Color",
                    "Standard"
                }
                : new[]
                {
                    "Universal Render Pipeline/Lit",
                    "Universal Render Pipeline/Unlit",
                    "Standard",
                    "Sprites/Default",
                    "Unlit/Color"
                };

            foreach (string shaderName in shaderNames)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader != null) return shader;
            }

            return Shader.Find("Hidden/InternalErrorShader");
        }

        private void ApplyMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            material.color = color;
        }

        private void ConfigureTransparentMaterial(Material material)
        {
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1.0f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0.0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
        }
    }
}
