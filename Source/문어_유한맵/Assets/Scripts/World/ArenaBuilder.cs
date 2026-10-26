using UnityEngine;

namespace OctopusSurvivors
{
    public static class ArenaBuilder
    {
        public static readonly Vector3 BeachArtworkPosition = new Vector3(2.1f, 1.6f, 0f);
        public static readonly Vector3 BeachArtworkScale = new Vector3(1.357988f, 1.417603f, 1f);
        public static readonly Vector3 BossBeachArtworkPosition = new Vector3(.4f, 3.7f, 0f);
        public static readonly Vector3 BossBeachArtworkScale = new Vector3(1.357988f, 1.357988f, 1f);
        static GameObject root;
        static Sprite sprite;
        static Sprite bossSprite;
        static bool usingBossArtwork;
        public static SpriteRenderer BeachRenderer { get; private set; }
        static readonly System.Collections.Generic.List<Object> backdropAssets = new System.Collections.Generic.List<Object>();
        public static void Build()
        {
            Destroy();
            root = new GameObject("FiniteBeachArena");
            var texture = RuntimeArt.BeachTexture();
            float artworkWidth = OctopusBossPattern.OverviewSize * 2f * FiniteBeach.Aspect;
            BuildBackdrop(texture, artworkWidth);
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
            var beach = new GameObject("Original beach artwork");
            beach.transform.SetParent(root.transform, false);
            BeachRenderer = beach.AddComponent<SpriteRenderer>();
            BeachRenderer.sprite = sprite;
            BeachRenderer.sortingOrder = -20;
            beach.transform.localPosition = BeachArtworkPosition;
            beach.transform.localScale = BeachArtworkScale;
        }
        public static void UseBossArtwork(bool enabled)
        {
            if (BeachRenderer == null) return;
            // LateUpdate calls this continuously. Reapply the authored transform before
            // the state-change shortcut so play-mode reloads and Inspector edits cannot
            // leave the live boss map at an old position.
            BeachRenderer.transform.localPosition = enabled ? BossBeachArtworkPosition : BeachArtworkPosition;
            BeachRenderer.transform.localScale = enabled ? BossBeachArtworkScale : BeachArtworkScale;
            if (usingBossArtwork == enabled) return;
            if (enabled && bossSprite == null)
            {
                var texture = RuntimeArt.BossBeachTexture();
                if (texture == null || texture.width != 7373 || texture.height != 5100)
                {
                    Debug.LogError("[Stage5Map] BossBeach must be the 7373x5100 bottom-only ocean extension.");
                    return;
                }
                // The original 4147 rows stay at the top. Anchor their center at world zero:
                // extending the canvas by 953 rows moves only its lower edge, not the shore.
                float pivotY = (953f + 4147f * .5f) / 5100f;
                bossSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(.5f, pivotY), 100f, 0, SpriteMeshType.FullRect);
            }
            BeachRenderer.sprite = enabled ? bossSprite : sprite;
            usingBossArtwork = enabled;
            foreach (string name in new[] { "Continuous outer ocean", "Upper beach continuation" })
            {
                var backdrop = root.transform.Find(name);
                if (backdrop != null) backdrop.gameObject.SetActive(!enabled);
            }
            if (enabled)
                Debug.Log($"[Stage5Map] 7373x4147 original anchored at world zero; bottom-only 953px extension; " +
                    $"scale={BeachRenderer.transform.localScale}; bounds={BeachRenderer.bounds}; actor coordinates unchanged.");
        }
        public static void Destroy()
        {
            if (root != null) Object.Destroy(root);
            if (sprite != null) Object.Destroy(sprite);
            if (bossSprite != null) Object.Destroy(bossSprite);
            foreach (var asset in backdropAssets) if (asset != null) Object.Destroy(asset);
            backdropAssets.Clear();
            root = null; sprite = null;
            bossSprite = null; usingBossArtwork = false;
            BeachRenderer = null;
        }
        static Material Tile(Texture2D source, Rect rect)
        {
            var t = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat };
            t.SetPixels(source.GetPixels((int)rect.x, (int)rect.y, t.width, t.height)); t.Apply();
            var material = new Material(Shader.Find("Sprites/Default")) { mainTexture = t };
            backdropAssets.Add(t); backdropAssets.Add(material); return material;
        }
        static void BuildBackdrop(Texture2D source, float artworkWidth)
        {
            var water = Tile(source, new Rect(source.width * .40f, 0, 1024, 80));
            var sand = Tile(source, new Rect(source.width * .40f, source.height * .60f, 1024, 512));
            float pixelsPerWorld = source.width / artworkWidth;
            for (int layer = 0; layer < 2; layer++)
            {
                var go = new GameObject(layer == 0 ? "Continuous outer ocean" : "Upper beach continuation");
                go.transform.SetParent(root.transform, false);
                var mesh = new Mesh(); backdropAssets.Add(mesh); go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = layer == 0 ? water : sand;
                renderer.sortingOrder = layer == 0 ? -40 : -30;
                const int rows = 24; var v = new Vector3[(rows + 1) * 2]; var uv = new Vector2[v.Length];
                var indices = new int[rows * 6];
                for (int y = 0; y <= rows; y++)
                {
                    float t = y / (float)rows;
                    float height = layer == 0 ? Mathf.Lerp(-FiniteBeach.Height * 2, FiniteBeach.Height * 2, t) :
                        Mathf.Lerp(FiniteBeach.Height * .5f - .04f, FiniteBeach.Height * 1.6f, t);
                    float width = layer == 0 ? FiniteBeach.Width * 1.8f : FiniteBeach.Width * .5f +
                        Mathf.SmoothStep(0, 1, Mathf.Clamp01(t * 4)) * FiniteBeach.Width * .48f;
                    for (int side = 0; side < 2; side++)
                    {
                        int n = y * 2 + side;
                        v[n] = new Vector3((side == 0 ? -1 : 1) * width, height, 0);
                        uv[n] = new Vector2(v[n].x * pixelsPerWorld / 1024,
                            v[n].y * pixelsPerWorld / (layer == 0 ? 80 : 512));
                    }
                    if (y == rows) continue;
                    int i = y * 6, a = y * 2;
                    indices[i] = a; indices[i + 1] = a + 2; indices[i + 2] = a + 1;
                    indices[i + 3] = a + 1; indices[i + 4] = a + 2; indices[i + 5] = a + 3;
                }
                mesh.vertices = v; mesh.uv = uv; mesh.triangles = indices; mesh.RecalculateBounds();
            }
        }
    }

    // Shared world dimensions keep host/client terrain identical at every window size.
    // Collision follows BeachGeometry's outlines in the rendered artwork coordinates.
    public static class FiniteBeach
    {
        public const float Aspect = 16f / 9f;
        public const float Height = GameConfig.CameraSize * 2 * 2;
        public const float ArtworkAspect = 5760f / 3240f;
        public const float Width = Height * ArtworkAspect;
        public static bool Walkable(Vector2 p, float radius)
        {
            // Use the rendered artwork's shore in every stage. The legacy map's
            // smaller polygon blocked large portions of the current sandy beach.
            return BeachGeometry.Walkable(p, radius);
        }
        public static Vector2 Clamp(Vector2 p, float radius)
        {
            if (Walkable(p,radius)) return p;
            p = new Vector2(Mathf.Clamp(p.x,-Width*.5f,Width*.5f),Mathf.Clamp(p.y,-Height*.5f,Height*.5f));
            // Deterministic nearest valid placement for spawns and displacement effects.
            for(float d=.25f;d<Width;d+=.25f)
                for(int i=0;i<32;i++)
                {
                    float a=i*Mathf.PI/16;
                    var q=p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*d;
                    if(Walkable(q,radius)) return q;
                }
            return Vector2.zero;
        }
        public static Vector2 Move(Vector2 from, Vector2 to, float radius)
        {
            var p=Clamp(from,radius);
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(from,to)/.12f));
            var step=(to-from)/steps;
            for(int i=0;i<steps;i++)
            {
                if(Walkable(p+step,radius)) p+=step;
                else {
                    var x=p+new Vector2(step.x,0); if(Walkable(x,radius)) p=x;
                    var y=p+new Vector2(0,step.y); if(Walkable(y,radius)) p=y;
                }
            }
            return p;
        }
        public static Vector3 ClampCamera(Vector3 p, Camera camera)
        {
            float y=camera.orthographicSize, x=y*camera.aspect;
            p.x=Mathf.Clamp(p.x,-Mathf.Max(0,Width*.5f-x),Mathf.Max(0,Width*.5f-x));
            // Leave extra room below the playable shore so the player can remain above
            // the bottom HUD while the camera follows to the lower edge of the field.
            float lowerLimit = -Height * .5f - GameConfig.CameraSize * .2f;
            p.y=Mathf.Clamp(p.y,lowerLimit,Mathf.Max(0,Height*.5f-y));
            return p;
        }
    }
}
