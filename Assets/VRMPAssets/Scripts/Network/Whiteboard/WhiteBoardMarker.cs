using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class WhiteboardMarker : NetworkBehaviour
{
    [SerializeField] private Transform _tip;
    [SerializeField] private int _penSize = 5;

    private Renderer _renderer;
    private Color[] _colors;
    private float _tipHeight;

    private RaycastHit _touch;
    private WhiteBoard _whiteboard;
    private Vector2 _touchPos, _lastTouchPos;
    private bool _touchedLastFrame;
    private Quaternion _lastTouchRot;

    void Start()
    {
        _renderer = _tip.GetComponent<Renderer>();
        _colors = Enumerable.Repeat(_renderer.material.color, _penSize * _penSize).ToArray();
        _tipHeight = _tip.localScale.y;
    }

    void Update()
    {
        Draw();
    }

    private void Draw()
    {
        if (!IsOwner) return;

        if (Physics.Raycast(_tip.position, transform.up, out _touch, _tipHeight))
        {
            if (_touch.transform.CompareTag("WhiteBoard"))
            {
                if (_whiteboard == null)
                    _whiteboard = _touch.transform.GetComponent<WhiteBoard>();

                //Debug.Log("Raycast hit WhiteBoard at UV: " + _touch.textureCoord);

                _touchPos = new Vector2(_touch.textureCoord.x, _touch.textureCoord.y);

                var x = (int)(_touchPos.x * _whiteboard.textureSize.x - (_penSize / 2));
                var y = (int)(_touchPos.y * _whiteboard.textureSize.y - (_penSize / 2));

                if (y < 0 || y > _whiteboard.textureSize.y || x < 0 || x > _whiteboard.textureSize.x)
                    return;

                if (_touchedLastFrame)
                {
                    // Send to server: current and previous (in pixel space)
                    DrawServerRpc(x, y, (int)_lastTouchPos.x, (int)_lastTouchPos.y, transform.rotation.eulerAngles);
                }

                _lastTouchPos = new Vector2(x, y);
                _lastTouchRot = transform.rotation;
                _touchedLastFrame = true;
                return;
            }
        }

        _whiteboard = null;
        _touchedLastFrame = false;
    }

    [ServerRpc(RequireOwnership = false)]
    void DrawServerRpc(int x, int y, int lastX, int lastY, Vector3 rotationEuler)
    {
        DrawClientRpc(x, y, lastX, lastY, rotationEuler);
    }

    [ClientRpc]
    void DrawClientRpc(int x, int y, int lastX, int lastY, Vector3 rotationEuler)
    {
        if (_whiteboard == null)
            _whiteboard = FindFirstObjectByType<WhiteBoard>();
        if (_whiteboard == null) return;

        // Draw current position
        _whiteboard.texture.SetPixels(x, y, _penSize, _penSize, _colors);

        // Interpolate between last and current position
        for (float f = 0.01f; f < 1.00f; f += 0.01f)
        {
            int lerpX = (int)Mathf.Lerp(lastX, x, f);
            int lerpY = (int)Mathf.Lerp(lastY, y, f);
            _whiteboard.texture.SetPixels(lerpX, lerpY, _penSize, _penSize, _colors);
        }

        _whiteboard.texture.Apply();

        // Apply rotation to keep orientation consistent (if needed)
        transform.rotation = Quaternion.Euler(rotationEuler);
    }
}
