//using System.Linq;
//using Unity.Netcode;
//using UnityEngine;

//public class WhiteboardNetwork : NetworkBehaviour
//{
//    [Tooltip("Reference to the local WhiteBoard component in scene (assign in inspector)")]
//    public WhiteBoard whiteboard;

//    // Clients call this to request a stroke be drawn.
//    // RequireOwnership = false allows any client to call this RPC for the server object.
//    [ServerRpc(RequireOwnership = false)]
//    public void SendStrokeServerRpc(Vector2 uv, int penSize, Vector4 color)
//    {
//        // Apply on server authoritative whiteboard
//        ApplyStrokeLocal(uv, penSize, new Color(color.x, color.y, color.z, color.w));

//        // Tell all clients to apply the stroke
//        BroadcastStrokeClientRpc(uv, penSize, color);
//    }

//    // Broadcast to clients
//    [ClientRpc]
//    void BroadcastStrokeClientRpc(Vector2 uv, int penSize, Vector4 color)
//    {
//        // Server will already have applied above; it's okay if the server applies again.
//        ApplyStrokeLocal(uv, penSize, new Color(color.x, color.y, color.z, color.w));
//    }

//    void ApplyStrokeLocal(Vector2 uv, int penSize, Color col)
//    {
//        if (whiteboard == null || whiteboard.GetWritableTexture() == null) return;

//        Texture2D tex = whiteboard.GetWritableTexture();
//        Vector2Int size = new Vector2Int(tex.width, tex.height);

//        int px = Mathf.RoundToInt(uv.x * (size.x - 1));
//        int py = Mathf.RoundToInt(uv.y * (size.y - 1));

//        int half = penSize / 2;
//        int sx = Mathf.Clamp(px - half, 0, size.x - 1);
//        int sy = Mathf.Clamp(py - half, 0, size.y - 1);
//        int w = Mathf.Clamp(px + half, 0, size.x - 1) - sx + 1;
//        int h = Mathf.Clamp(py + half, 0, size.y - 1) - sy + 1;

//        Color[] cols = Enumerable.Repeat(col, w * h).ToArray();
//        tex.SetPixels(sx, sy, w, h, cols);
//        tex.Apply();
//    }
//}
