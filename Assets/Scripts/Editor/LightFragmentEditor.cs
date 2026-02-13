using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool để tạo và đặt Light Fragment trong Scene.
/// Menu: GameObject > No Way Out > Light Fragment
/// Hoặc right-click trong Hierarchy > No Way Out > Light Fragment
/// </summary>
public class LightFragmentEditor
{
    private static int fragmentCounter = 0;
    
    [MenuItem("GameObject/No Way Out/Light Fragment", false, 10)]
    public static void CreateLightFragment()
    {
        fragmentCounter++;
        
        // Tạo GameObject
        GameObject fragment = new GameObject($"LightFragment_{fragmentCounter}");
        
        // Đặt ở vị trí Scene View camera đang nhìn (hoặc 0,0,0)
        if (SceneView.lastActiveSceneView != null)
        {
            Camera cam = SceneView.lastActiveSceneView.camera;
            fragment.transform.position = cam.transform.position + cam.transform.forward * 5f;
            fragment.transform.position = new Vector3(
                Mathf.Round(fragment.transform.position.x),
                Mathf.Round(fragment.transform.position.y),
                0f
            );
        }
        
        // Add LightFragment component (auto-setup collider, light, sprite trong Awake)
        fragment.AddComponent<LightFragment>();
        
        // Tạo icon sprite để dễ nhìn trong Editor
        var sr = fragment.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = fragment.AddComponent<SpriteRenderer>();
        }
        sr.color = new Color(1f, 0.95f, 0.6f);
        sr.sortingOrder = 5;
        
        // Tạo diamond sprite
        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color32[] pixels = new Color32[16 * 16];
        for (int px = 0; px < 16; px++)
        {
            for (int py = 0; py < 16; py++)
            {
                int dx = Mathf.Abs(px - 8);
                int dy = Mathf.Abs(py - 8);
                if (dx + dy <= 7)
                    pixels[py * 16 + px] = new Color32(255, 240, 150, 255);
                else
                    pixels[py * 16 + px] = new Color32(0, 0, 0, 0);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        
        // Nếu có object đang select → đặt làm child (ví dụ đặt trong room)
        if (Selection.activeGameObject != null)
        {
            fragment.transform.SetParent(Selection.activeGameObject.transform);
        }
        
        // Select fragment mới tạo
        Selection.activeGameObject = fragment;
        Undo.RegisterCreatedObjectUndo(fragment, "Create Light Fragment");
        
        Debug.Log($"[LightFragmentEditor] Created Light Fragment #{fragmentCounter} at {fragment.transform.position}. Di chuyển nó đến vị trí mong muốn!");
    }
    
    [MenuItem("GameObject/No Way Out/Light Fragment (x3 - Tutorial Set)", false, 11)]
    public static void CreateTutorialFragmentSet()
    {
        // Tạo parent container
        GameObject container = new GameObject("LightFragments_Tutorial");
        container.transform.position = Vector3.zero;
        
        // Nếu có object đang select → đặt làm child
        if (Selection.activeGameObject != null)
        {
            container.transform.SetParent(Selection.activeGameObject.transform);
        }
        
        // Tạo 3 fragments với offset để dễ kéo
        Vector3 basePos = Vector3.zero;
        if (SceneView.lastActiveSceneView != null)
        {
            Camera cam = SceneView.lastActiveSceneView.camera;
            basePos = cam.transform.position + cam.transform.forward * 5f;
            basePos.z = 0f;
        }
        
        string[] names = { "Fragment_Easy", "Fragment_Medium", "Fragment_Hidden" };
        Vector3[] offsets = { new Vector3(-5, 0, 0), new Vector3(0, 0, 0), new Vector3(5, 0, 0) };
        
        for (int i = 0; i < 3; i++)
        {
            fragmentCounter++;
            GameObject frag = new GameObject(names[i]);
            frag.transform.SetParent(container.transform);
            frag.transform.position = basePos + offsets[i];
            frag.transform.position = new Vector3(
                Mathf.Round(frag.transform.position.x),
                Mathf.Round(frag.transform.position.y),
                0f
            );
            
            frag.AddComponent<LightFragment>();
            
            // Diamond sprite
            var sr = frag.GetComponent<SpriteRenderer>();
            if (sr == null) sr = frag.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.95f, 0.6f);
            sr.sortingOrder = 5;
            
            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color32[] pixels = new Color32[16 * 16];
            for (int px = 0; px < 16; px++)
            {
                for (int py = 0; py < 16; py++)
                {
                    int dx = Mathf.Abs(px - 8);
                    int dy = Mathf.Abs(py - 8);
                    if (dx + dy <= 7)
                        pixels[py * 16 + px] = new Color32(255, 240, 150, 255);
                    else
                        pixels[py * 16 + px] = new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        }
        
        Selection.activeGameObject = container;
        Undo.RegisterCreatedObjectUndo(container, "Create Tutorial Fragment Set");
        
        Debug.Log("[LightFragmentEditor] Created 3 Light Fragments for tutorial! Kéo chúng đến vị trí mong muốn trên map.");
    }
}
