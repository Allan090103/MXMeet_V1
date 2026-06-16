using Unity.Netcode;
using UnityEngine;

public class WhiteBoard : NetworkBehaviour
{
    public Texture2D texture;
    public Vector2 textureSize = new Vector2(2048, 2048);

    private Renderer _renderer;

    void Start()
    {
        _renderer = GetComponent<Renderer>();

        // Always create texture on every client
        if (texture == null)
        {
            texture = new Texture2D((int)textureSize.x, (int)textureSize.y, TextureFormat.RGBA32, false);
        }

        _renderer.material.mainTexture = texture;
    }

    public Color SampleBackgroundColor()
    {
        if (texture != null)
        {
            return texture.GetPixel(0, 0);
        }
        return Color.white;
    }

    public void ClearBoard()
    {
        Color[] clearColor = new Color[texture.width * texture.height];
        Color backgroundColor = SampleBackgroundColor();

        for (int i = 0; i < clearColor.Length; i++)
        {
            clearColor[i] = backgroundColor;
        }

        texture.SetPixels(clearColor);
        texture.Apply();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ClearBoardServerRpc()
    {
        ClearBoardClientRpc();
    }

    [ClientRpc]
    private void ClearBoardClientRpc()
    {
        ClearBoard();
    }
}
