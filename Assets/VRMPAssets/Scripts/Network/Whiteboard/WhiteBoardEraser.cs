using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class WhiteboardEraser : NetworkBehaviour
{
    [SerializeField] private Transform _tip;
    [SerializeField] private int _eraserSize = 30;

    private Color[] _eraseColor;
    private float _tipHeight;

    private RaycastHit _touch;
    private WhiteBoard _whiteboard;
    private Vector2 _touchPos, _lastTouchPos;
    private bool _touchedLastFrame;
    private Quaternion _lastTouchRot;

    void Start()
    {
        _whiteboard = FindFirstObjectByType<WhiteBoard>();

        if (_whiteboard != null)
        {
            _eraseColor = Enumerable.Repeat(SampleBackgroundColor(), _eraserSize * _eraserSize).ToArray();
        }
        else
        {
            Debug.LogError("Whiteboard not found in the scene!");
        }

        _tipHeight = _tip.localScale.y;
    }

    void Update()
    {
        Erase();
    }

    private void Erase()
    {
        if (!IsOwner) return;

        if (Physics.Raycast(_tip.position, transform.up, out _touch, _tipHeight))
        {
            if (_touch.transform.CompareTag("WhiteBoard"))
            {
                if (_whiteboard == null)
                {
                    _whiteboard = _touch.transform.GetComponent<WhiteBoard>();
                }

                _touchPos = new Vector2(_touch.textureCoord.x, _touch.textureCoord.y);

                var x = (int)(_touchPos.x * _whiteboard.textureSize.x - (_eraserSize / 2));
                var y = (int)(_touchPos.y * _whiteboard.textureSize.y - (_eraserSize / 2));

                if (y < 0 || y > _whiteboard.textureSize.y || x < 0 || x > _whiteboard.textureSize.x)
                    return;

                if (_touchedLastFrame)
                {
                    EraseServerRpc(x, y, (int)_lastTouchPos.x, (int)_lastTouchPos.y, transform.rotation.eulerAngles);
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
    void EraseServerRpc(int x, int y, int lastX, int lastY, Vector3 rotationEuler)
    {
        EraseClientRpc(x, y, lastX, lastY, rotationEuler);
    }

    [ClientRpc]
    void EraseClientRpc(int x, int y, int lastX, int lastY, Vector3 rotationEuler)
    {
        if (_whiteboard == null)
            _whiteboard = FindFirstObjectByType<WhiteBoard>();
        if (_whiteboard == null)
        {
            Debug.LogWarning("Whiteboard not found on client.");
            return;
        }

        // If needed, (re)sample erase color
        if (_eraseColor == null)
        {
            _eraseColor = Enumerable.Repeat(SampleBackgroundColor(), _eraserSize * _eraserSize).ToArray();
        }

        // Maintain orientation
        transform.rotation = Quaternion.Euler(rotationEuler);

        _whiteboard.texture.SetPixels(x, y, _eraserSize, _eraserSize, _eraseColor);

        for (float f = 0.01f; f < 1.00f; f += 0.01f)
        {
            int lerpX = (int)Mathf.Lerp(lastX, x, f);
            int lerpY = (int)Mathf.Lerp(lastY, y, f);
            _whiteboard.texture.SetPixels(lerpX, lerpY, _eraserSize, _eraserSize, _eraseColor);
        }

        _whiteboard.texture.Apply();
    }

    private Color SampleBackgroundColor()
    {
        if (_whiteboard != null && _whiteboard.texture != null)
        {
            return _whiteboard.texture.GetPixel(0, 0);
        }

        return Color.white;
    }
}
