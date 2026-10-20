using System;
using System.Collections.Generic;
using UnityEngine;

namespace OctopusSurvivors
{
    // Whole supplied frames, including their full base, share one root pivot.
    // A small root-locked mesh follows the random target; it never cuts/reassembles the artwork.
    public sealed class OctopusBossVisual : MonoBehaviour
    {
        sealed class Frame
        {
            public Texture2D Texture;
            public Rect Rect;
            public Vector2 Pivot, Tip;
            public float Ppu, Height;
            public Material Material;
            public float[] Centerline;
            public bool Down;
        }
        static Frame[] idle, whip, burrow, slam;
        static Frame contact, head;
        static readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
        static readonly Dictionary<Texture2D, Color32[]> pixelCache = new Dictionary<Texture2D, Color32[]>();
        const int Columns = 32, Rows = 18;
        sealed class Arm
        {
            public GameObject Root;
            public Mesh Mesh;
            public MeshRenderer Renderer;
            public readonly Vector3[] Vertices = new Vector3[(Columns + 1) * (Rows + 1)];
            public readonly Vector2[] Uv = new Vector2[(Columns + 1) * (Rows + 1)];
        }
        readonly Arm[] arms = new Arm[6];
        readonly Arm[] trails = new Arm[6];
        readonly Mesh[] impactMeshes = new Mesh[6];
        readonly MeshRenderer[] impactRenderers = new MeshRenderer[6];
        readonly Mesh[] healthMeshes = new Mesh[6];
        readonly MeshRenderer[] healthRenderers = new MeshRenderer[6];
        readonly List<Vector3> vertices = new List<Vector3>(600);
        readonly List<Color> colors = new List<Color>(600);
        readonly List<int> triangles = new List<int>(900);
        Transform world;
        SpriteRenderer body;
        Mesh areaMesh;
        MeshRenderer areaRenderer;
        Material effects;
        OctopusBossSnapshot state, preview;
        bool isPreview, initialized;
        float observedTime, receivedAt;
        float nextBossHudLookup;
        RectTransform bossHud;
        bool loggedBossHudAdjustment;
        byte observedPattern;
        byte hitFlags;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAssets()
        {
            foreach (var o in assets) if (o != null) Destroy(o);
            assets.Clear(); pixelCache.Clear(); idle = whip = burrow = slam = null; contact = head = null;
        }
        static Texture2D Load(string name, bool whiteKey = false)
        {
            var bytes = Resources.Load<TextAsset>("OctopusBoss/" + name);
            if (bytes == null) throw new InvalidOperationException("Missing octopus boss asset: " + name);
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = "Octopus boss " + name,
                filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            ImageConversion.LoadImage(t, bytes.bytes);
            if (whiteKey || name == "Burrow")
            {
                var pixels = t.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].r > 235 && pixels[i].g > 235 && pixels[i].b > 235 &&
                        Mathf.Max(pixels[i].r, pixels[i].b) - pixels[i].g < 25 && whiteKey) pixels[i].a = 0;
                    var p = pixels[i];
                    if (p.a > 0 && p.r > p.g * 1.07f && p.g > p.b * 1.16f && p.r > 60)
                    {
                        float shade = Mathf.InverseLerp(.23f, 1f, p.r / 255f);
                        Color earth = Color.Lerp(new Color(.37f, .24f, .14f), new Color(.91f, .77f, .59f), shade);
                        earth.a = p.a / 255f; pixels[i] = earth;
                    }
                }
                t.SetPixels32(pixels); t.Apply();
            }
            assets.Add(t); return t;
        }
        static Frame Make(Texture2D t, Rect rect, Vector2 pivot, float ppu, bool down = false)
        {
            var material = new Material(Shader.Find("Sprites/Default")) { mainTexture = t };
            assets.Add(material);
            var frame = new Frame { Texture = t, Rect = rect, Pivot = pivot, Ppu = ppu, Height = rect.height / ppu, Material = material, Down = down };
            if (!pixelCache.TryGetValue(t, out var pixels)) pixelCache[t] = pixels = t.GetPixels32();
            float far = -1;
            // The farthest solid pixel gives the end of each independently drawn pose.
            for (int y = 0; y < rect.height; y++)
            for (int x = 0; x < rect.width; x++)
            {
                if (pixels[((int)rect.y + y) * t.width + (int)rect.x + x].a < 160) continue;
                var p = new Vector2(x, y) - pivot;
                if (p.sqrMagnitude > far) { far = p.sqrMagnitude; frame.Tip = p / ppu; }
            }
            frame.Centerline = new float[Columns + 1];
            for (int col = 0; col <= Columns; col++)
            {
                int sample = Mathf.Clamp(Mathf.RoundToInt((down ? rect.height : rect.width) * col / Columns),
                    0, (int)(down ? rect.height : rect.width) - 1);
                int lo = down ? (int)rect.width : (int)rect.height, hi = -1;
                for (int along = 0; along < (down ? rect.width : rect.height); along++)
                {
                    int x = down ? along : sample, y = down ? (int)rect.height - 1 - sample : along;
                    if (pixels[((int)rect.y + y) * t.width + (int)rect.x + x].a <= 120) continue;
                    lo = Mathf.Min(lo, along); hi = Mathf.Max(hi, along);
                }
                frame.Centerline[col] = hi >= lo ? ((lo + hi) * .5f - (down ? pivot.x : pivot.y)) / ppu : float.NaN;
            }
            for (int col = 0; col <= Columns; col++) if (float.IsNaN(frame.Centerline[col]))
            {
                int nearest = -1;
                for (int offset = 1; offset <= Columns && nearest < 0; offset++)
                {
                    if (col - offset >= 0 && !float.IsNaN(frame.Centerline[col - offset])) nearest = col - offset;
                    else if (col + offset <= Columns && !float.IsNaN(frame.Centerline[col + offset])) nearest = col + offset;
                }
                frame.Centerline[col] = nearest >= 0 ? frame.Centerline[nearest] : 0;
            }
            return frame;
        }
        static Frame[] Sheet(string name, int cols, int rows, int count, float baseHeight, bool whiteKey = false, bool fixedWhip = false)
        {
            var t = Load(name, whiteKey); var pixels = t.GetPixels32();
            int w = t.width / cols, h = t.height / rows;
            var result = new Frame[count];
            float ppu = 1;
            for (int i = 0; i < count; i++)
            {
                // The playback atlas has its first pose in the top-left cell,
                // four empty cells after it, then the remaining twenty poses
                // across the next four rows. Do not treat those empty cells as
                // animation frames or the last four authored poses get dropped.
                int sourceIndex = fixedWhip && i > 0 ? i + 4 : i;
                int ox = sourceIndex % cols * w, oy = (rows - 1 - sourceIndex / cols) * h;
                int bottom = h, top = 0, left = w, right = 0;
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                {
                    var p = pixels[(oy + y) * t.width + ox + x];
                    bool art = p.a > 100 && (name != "Slam" || Mathf.Max(p.r, Mathf.Max(p.g, p.b)) - Mathf.Min(p.r, Mathf.Min(p.g, p.b)) > 12);
                    if (art) { bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); left = Mathf.Min(left, x); right = Mathf.Max(right, x); }
                }
                if (i == 0) ppu = Mathf.Max(1, top - bottom + 1) / baseHeight;
                Vector2 pivot = new Vector2(w * .5f, bottom);
                if (fixedWhip) pivot = new Vector2(300, 39);
                else
                {
                    float total = 0; int n = 0;
                    for (int y = bottom; y <= Mathf.Min(h - 1, bottom + 7); y++) for (int x = 0; x < w; x++)
                        if (pixels[(oy + y) * t.width + ox + x].a > 120) { total += x; n++; }
                    if (n > 0) pivot.x = total / n;
                }
                bool down = name == "Slam" && i >= 6;
                if (down)
                {
                    float total = 0; int n = 0;
                    for (int y = Mathf.Max(0, top - 16); y <= top; y++) for (int x = 0; x < w; x++)
                        if (pixels[(oy + y) * t.width + ox + x].a > 120) { total += x; n++; }
                    pivot = new Vector2(n > 0 ? total / n : w * .5f, top - 10);
                }
                var rect = new Rect(ox, oy, w, h);
                if (name == "Slam")
                {
                    int l = Mathf.Max(0, left - 8), b = Mathf.Max(0, bottom - 8);
                    int r = Mathf.Min(w, right + 9), u = Mathf.Min(h, top + 9);
                    rect = new Rect(ox + l, oy + b, r - l, u - b); pivot -= new Vector2(l, b);
                }
                result[i] = Make(t, rect, pivot, ppu, down);
                result[i].Height = Mathf.Max(1, top - bottom + 1) / ppu;
            }
            return result;
        }
        static void EnsureAssets()
        {
            if (idle != null) return;
            float h = OctopusBossPattern.IdleHeight;
            idle = Sheet("Idle", 4, 1, 4, h, true);
            whip = Sheet("Whip", 5, 5, 21, h, false, true);
            burrow = Sheet("Burrow", 4, 3, 12, h);
            slam = Sheet("Slam", 3, 3, 9, h);
            var texture = Load("Contact");
            contact = Make(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(35, 4), 11.2f);
            texture = Load("Body");
            head = Make(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(texture.width * .5f, texture.height * .5f), 100);
        }
        void Initialize()
        {
            if (initialized) return;
            EnsureAssets(); initialized = true;
            world = new GameObject("Whole-frame octopus boss").transform; world.SetParent(transform, false);
            body = new GameObject("Supplied boss character").AddComponent<SpriteRenderer>();
            body.transform.SetParent(world, false); body.sortingOrder = 12;
            body.sprite = Sprite.Create(head.Texture, head.Rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            body.transform.localScale = Vector3.one * (FiniteBeach.Width * .235f / body.sprite.bounds.size.x);
            effects = new Material(Shader.Find("Sprites/Default"));
            for (int a = 0; a < 6; a++)
            {
                var arm = new Arm { Root = new GameObject("Whole tentacle " + (a + 1)), Mesh = new Mesh() };
                arms[a] = arm; arm.Mesh.MarkDynamic(); arm.Root.transform.SetParent(world, false);
                arm.Root.AddComponent<MeshFilter>().sharedMesh = arm.Mesh;
                arm.Renderer = arm.Root.AddComponent<MeshRenderer>();
                arm.Renderer.sortingOrder = 10 + (2 - a % 3) * 2;
                var indices = new int[Columns * Rows * 6]; int n = 0;
                for (int y = 0; y < Rows; y++) for (int x = 0; x < Columns; x++)
                {
                    int i = y * (Columns + 1) + x;
                    indices[n++] = i; indices[n++] = i + Columns + 1; indices[n++] = i + 1;
                    indices[n++] = i + 1; indices[n++] = i + Columns + 1; indices[n++] = i + Columns + 2;
                }
                arm.Mesh.vertices = arm.Vertices; arm.Mesh.triangles = indices;
                var trail = new Arm { Root = new GameObject("Short whole-pose trail " + a), Mesh = new Mesh() };
                trails[a] = trail; trail.Root.transform.SetParent(world, false);
                trail.Root.AddComponent<MeshFilter>().sharedMesh = trail.Mesh;
                trail.Renderer = trail.Root.AddComponent<MeshRenderer>(); trail.Renderer.sortingOrder = arm.Renderer.sortingOrder - 1;
                trail.Mesh.MarkDynamic(); trail.Mesh.vertices = trail.Vertices; trail.Mesh.triangles = indices;
                var tint = new Color[trail.Vertices.Length];
                for (int v = 0; v < tint.Length; v++) tint[v] = new Color(1, .8f, .85f, .14f);
                trail.Mesh.colors = tint;
                var effect = new GameObject("Contact, sand and short trail " + a); effect.transform.SetParent(world, false);
                impactMeshes[a] = new Mesh(); impactMeshes[a].MarkDynamic();
                effect.AddComponent<MeshFilter>().sharedMesh = impactMeshes[a];
                impactRenderers[a] = effect.AddComponent<MeshRenderer>();
                impactRenderers[a].sharedMaterial = effects; impactRenderers[a].sortingOrder = 21;
                var health = new GameObject("Tentacle HP " + (a + 1)); health.transform.SetParent(world, false);
                healthMeshes[a] = new Mesh(); health.AddComponent<MeshFilter>().sharedMesh = healthMeshes[a];
                healthRenderers[a] = health.AddComponent<MeshRenderer>(); healthRenderers[a].sharedMaterial = effects; healthRenderers[a].sortingOrder = 28;
            }
            var area = new GameObject("Half-area anticipation"); area.transform.SetParent(world, false);
            areaMesh = new Mesh(); area.AddComponent<MeshFilter>().sharedMesh = areaMesh;
            areaRenderer = area.AddComponent<MeshRenderer>(); areaRenderer.sharedMaterial = effects; areaRenderer.sortingOrder = -5;
        }
        public void Configure(byte flags)
        {
            hitFlags = flags;
            Initialize(); body.color = (flags & (byte)EnemyFlag.Hit) != 0 ? new Color(1, .65f, .65f) : Color.white;
        }
        public void Preview(OctopusBossSnapshot value)
        { Initialize(); isPreview = true; preview = value; Draw(value); }
        void LateUpdate()
        {
            if (!initialized || isPreview) return;
            var session = GameSession.Instance;
            if (session == null) return;
            state = session.OctopusBoss.Value;
            if (state.Time != observedTime || state.Pattern != observedPattern)
            { observedTime = state.Time; observedPattern = state.Pattern; receivedAt = Time.time; }
            if (session.CombatActive) state.Time += Mathf.Min(.08f, Time.time - receivedAt);
            Draw(state);
        }
        void Draw(OctopusBossSnapshot s)
        {
            // Cancel WorldView's generic actor scale: these are measured arena/world sizes.
            var scale = transform.lossyScale;
            world.localScale = new Vector3(1 / Mathf.Max(.001f, scale.x), 1 / Mathf.Max(.001f, scale.y), 1);
            world.position = Vector3.zero;
            // Keep the boss's simulation anchor and attacks unchanged while placing its
            // large portrait below the fixed top-center boss HUD.
            body.transform.position = OctopusBossPattern.Head + Vector2.down * (FiniteBeach.Height * .14f);
            body.color = (hitFlags & (byte)EnemyFlag.Hit) != 0 ? new Color(1, .65f, .65f) : Color.white;
            if (s.Vulnerable > 0)
            { body.transform.position += Vector3.down * .8f; body.color = new Color(.78f, .84f, .88f); }
            DrawArea(s);
            for (int a = 0; a < 6; a++)
            {
                impactRenderers[a].enabled = false;
                trails[a].Root.SetActive(false);
                var root = OctopusBossPattern.RestRoot(a);
                healthRenderers[a].enabled = OctopusBossPattern.ArmsVisible(s);
                if (healthRenderers[a].enabled) DrawHealth(a, OctopusBossPattern.ArmRoot(s, a), s.ArmHealth(a),
                    s.Pattern == 4 && s.Time >= 1.65f && s.Time < 4.65f);
                if (s.Vulnerable > 0 || s.ArmHealth(a) <= 0) { arms[a].Root.SetActive(false); continue; }
                arms[a].Root.SetActive(true);
                if (s.Pattern == 4) DrawHalfArm(s, a, root);
                else DrawRelayArm(s, a, root);
            }
            KeepHeadClearOfBossHud();
        }

        void KeepHeadClearOfBossHud()
        {
            if (body == null || !body.enabled) return;
            if (bossHud == null && Time.unscaledTime >= nextBossHudLookup)
            {
                var hud = GameObject.Find("BossBar");
                if (hud != null) bossHud = hud.GetComponent<RectTransform>();
                nextBossHudLookup = Time.unscaledTime + 1f;
            }
            var camera = Camera.main;
            if (bossHud == null || camera == null || !camera.orthographic || !bossHud.gameObject.activeInHierarchy || camera.pixelHeight <= 0)
                return;

            var canvas = bossHud.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var corners = new Vector3[4];
            bossHud.GetWorldCorners(corners);
            float hudBottom = float.PositiveInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                var point = RectTransformUtility.WorldToScreenPoint(
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, corners[i]);
                hudBottom = Mathf.Min(hudBottom, point.y);
            }

            var bounds = body.bounds;
            var top = camera.WorldToScreenPoint(new Vector3(bounds.center.x, bounds.max.y, body.transform.position.z));
            const float gapPixels = 12f;
            float overlapPixels = top.y - (hudBottom - gapPixels);
            if (overlapPixels <= 0f) return;

            float worldPerViewportPixel = 2f * camera.orthographicSize / camera.pixelHeight;
            float shift = overlapPixels * worldPerViewportPixel;
            body.transform.position += Vector3.down * shift;
            if (!loggedBossHudAdjustment)
            {
                Debug.Log($"[Stage5BossHud] moved only the rendered boss body down by {shift:F2} world units " +
                    $"to keep it {gapPixels:F0}px below the top HUD; simulation head and attacks are unchanged.");
                loggedBossHudAdjustment = true;
            }
        }
        void DrawRelayArm(OctopusBossSnapshot s, int arm, Vector2 root)
        {
            int strike = -1; float age = 0;
            for (int n = 0; n < 30; n++)
            {
                if (OctopusBossPattern.RelayArm(n) != arm) continue;
                float t = s.Time - (OctopusBossPattern.RelayContact(n) - OctopusBossPattern.ContactOffset);
                if (t >= 0 && t < OctopusBossPattern.SwingDuration) { strike = n; age = t; break; }
            }
            if (strike < 0)
            { DrawFrame(arm, idle[Mathf.Abs((int)(s.Time * 8) + arm) % 4], root, Vector2.zero, 0, arm >= 3); return; }
            var target = OctopusBossPattern.RelayTarget(s.Seed, strike);
            float reach = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.13f, OctopusBossPattern.ContactOffset, age));
            if (age > .45f) reach *= 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.45f, .76f, age));
            Frame f;
            if (age >= .28f && age < .55f) f = contact;
            else if (age < .32f) f = whip[Mathf.Clamp((int)(age / .32f * 12), 0, 11)];
            else f = whip[Mathf.Clamp(13 + (int)((age - .55f) / .23f * 8), 13, 20)];
            DrawFrame(arm, f, root, target - root, reach, arm >= 3);
            if (reach > .55f)
            {
                trails[arm].Root.SetActive(true);
                DrawMesh(trails[arm], f, root, target - root, reach * .88f, arm >= 3);
            }
            float since = age - OctopusBossPattern.ContactOffset;
            if (since >= 0 && since < .36f)
                DrawImpact(arm, target, new Vector2(2.6f, 1.9f), since, s.Seed + (uint)strike, null);
            else if (since < 0 && since > -.27f)
                DrawImpact(arm, target, new Vector2(2.6f, 1.9f), since, s.Seed + (uint)strike, null);
        }
        void DrawHalfArm(OctopusBossSnapshot s, int arm, Vector2 rest)
        {
            float t = s.Time; var raised = OctopusBossPattern.HalfRoot(s.Half, arm);
            if (t < .55f) { DrawFrame(arm, burrow[Mathf.Clamp(8 + (int)(t / .55f * 4), 8, 11)], rest, Vector2.zero, 0, arm >= 3); return; }
            if (t < 1.1f || (t >= 5.2f && t < 5.65f)) { arms[arm].Root.SetActive(false); return; }
            if (t < 1.65f) { DrawFrame(arm, burrow[Mathf.Clamp(4 + (int)((t - 1.1f) / .55f * 4), 4, 7)], raised, Vector2.zero, 0, arm >= 3); return; }
            if (t >= 4.65f && t < 5.2f) { DrawFrame(arm, burrow[Mathf.Clamp(8 + (int)((t - 4.65f) / .55f * 4), 8, 11)], raised, Vector2.zero, 0, arm >= 3); return; }
            if (t >= 5.65f && t < 6.2f) { DrawFrame(arm, burrow[Mathf.Clamp(4 + (int)((t - 5.65f) / .55f * 4), 4, 7)], rest, Vector2.zero, 0, arm >= 3); return; }
            if (t >= 6.2f) { DrawFrame(arm, idle[(int)(t * 8 + arm) % 4], rest, Vector2.zero, 0, arm >= 3); return; }
            if (s.Half >= 2)
            {
                DrawSideHalfArm(s, arm, raised);
                return;
            }
            float first = OctopusBossPattern.HalfContact(s.Half, arm, 0);
            int cycle = Mathf.Max(0, Mathf.FloorToInt((t - first) / OctopusBossPattern.HalfInterval(s.Half)));
            float contactTime = OctopusBossPattern.HalfContact(s.Half, arm, cycle);
            float since = t - contactTime;
            var target = OctopusBossPattern.HalfTarget(s.Seed, s.Half, arm, cycle);
            float progress = Mathf.Repeat(t - first + .09f, OctopusBossPattern.SlamInterval) / OctopusBossPattern.SlamInterval;
            float reach = progress < .375f ? Mathf.SmoothStep(0, 1, progress / .375f) :
                1 - Mathf.SmoothStep(0, 1, (progress - .375f) / .625f);
            Frame frame = progress < .22f ? slam[2] : progress < .38f ? slam[6] :
                progress < .62f ? slam[7] : progress < .78f ? slam[8] : slam[4];
            DrawFrame(arm, frame, raised, target - raised, reach, arm >= 3);
            if (reach > .4f)
            {
                trails[arm].Root.SetActive(true);
                DrawMesh(trails[arm], frame, raised, target - raised, reach * .78f, arm >= 3);
            }
            if (since >= 0) DrawImpact(arm, target, OctopusBossPattern.HalfRadii(s.Half), since,
                s.Seed + (uint)(cycle * 6 + arm), OctopusBossPattern.HalfRect(s.Half));
        }
        void DrawSideHalfArm(OctopusBossSnapshot s, int arm, Vector2 root)
        {
            float t = s.Time, interval = OctopusBossPattern.SideSlamInterval;
            float firstCycle = OctopusBossPattern.HalfAttackStart + arm * .035f;
            int cycle = Mathf.Max(0, Mathf.FloorToInt((t - firstCycle) / interval));
            float sequenceStart = firstCycle + cycle * interval;
            float progress = Mathf.Clamp01((t - sequenceStart) / interval);
            float age = progress * interval;
            int frame = age < .08f ? Mathf.RoundToInt(Mathf.Lerp(0, 3, age / .08f)) :
                age < .20f ? Mathf.RoundToInt(Mathf.Lerp(4, 7, (age - .08f) / .12f)) :
                age < .28f ? Mathf.RoundToInt(Mathf.Lerp(8, 11, (age - .20f) / .08f)) :
                age < .36f ? 12 :
                age < .44f ? Mathf.RoundToInt(Mathf.Lerp(13, 16, (age - .36f) / .08f)) :
                Mathf.RoundToInt(Mathf.Lerp(17, 20, (age - .44f) / .04f));
            // The supplied long-contact pose is the impact keyframe for side slams.
            // These are complete authored poses with their own sand base and pivots.
            // Draw them as-is; bending the whole frame moves the soil and tears the silhouette.
            var sideFrame = frame == 12 ? contact : whip[Mathf.Clamp(frame, 0, 20)];
            DrawFrame(arm, sideFrame, root, Vector2.zero, 0, arm >= 3);
        }
        void DrawFrame(int index, Frame f, Vector2 root, Vector2 target, float reach, bool mirror)
        { DrawMesh(arms[index], f, root, target, reach, mirror); }
        void DrawMesh(Arm arm, Frame f, Vector2 root, Vector2 target, float reach, bool mirror, float thicknessScale = 1f)
        {
            arm.Renderer.sharedMaterial = f.Material;
            arm.Root.transform.position = root;
            if (f.Down)
            {
                // The three downward poses already contain their full authored
                // reach. Keep their size and root pixel exact instead of scaling
                // each frame by its target distance (which made them shrink).
                int downIndex = 0;
                for (int y = 0; y <= Rows; y++) for (int x = 0; x <= Columns; x++)
                {
                    float px = f.Rect.width * x / Columns, py = f.Rect.height * y / Rows;
                    Vector2 p = (new Vector2(px, py) - f.Pivot) / f.Ppu;
                    if (mirror) p.x = -p.x;
                    arm.Vertices[downIndex] = p;
                    arm.Uv[downIndex++] = new Vector2((f.Rect.x + .5f + (f.Rect.width - 1) * x / Columns) / f.Texture.width,
                        (f.Rect.y + .5f + (f.Rect.height - 1) * y / Rows) / f.Texture.height);
                }
                arm.Mesh.vertices = arm.Vertices; arm.Mesh.uv = arm.Uv; arm.Mesh.RecalculateBounds();
                return;
            }
            Vector2 tip = f.Tip;
            if (mirror) target.x = -target.x;
            float keep = Mathf.Min(3.4f, OctopusBossPattern.IdleHeight * .34f);
            float length = Mathf.Max(.01f, tip.x - keep);
            float startColumn = Mathf.Clamp((f.Down ? (f.Rect.height - f.Pivot.y + keep * f.Ppu) / f.Rect.height :
                (f.Pivot.x + keep * f.Ppu) / f.Rect.width) * Columns, 0, Columns);
            int startIndex = Mathf.Min(Columns - 1, (int)startColumn);
            float startY = Mathf.Lerp(f.Centerline[startIndex], f.Centerline[startIndex + 1], startColumn - startIndex);
            Vector2 p0 = new Vector2(keep, startY);
            float span = target.x - keep;
            Vector2 p1 = p0 + new Vector2(span * .34f, Mathf.Clamp(target.magnitude * .10f, .6f, 3.6f));
            Vector2 p2 = target + new Vector2(-span * .24f, Mathf.Clamp(target.magnitude * .06f, .5f, 2.4f));
            int n = 0;
            for (int y = 0; y <= Rows; y++) for (int x = 0; x <= Columns; x++)
            {
                float px = f.Rect.width * x / Columns, py = f.Rect.height * y / Rows;
                Vector2 p = (new Vector2(px, py) - f.Pivot) / f.Ppu;
                float column = x;
                int ci = Mathf.Clamp((int)column, 0, Columns - 1);
                float cross = Mathf.Lerp(f.Centerline[ci], f.Centerline[ci + 1], column - ci);
                p.y = cross + (p.y - cross) * thicknessScale;
                if (reach > 0 && tip.x > keep + .5f && p.x > keep)
                {
                    float t = Mathf.Clamp01((p.x - keep) / length), u = 1 - t;
                    Vector2 center = u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * target;
                    Vector2 tangent = (3 * u * u * (p1 - p0) + 6 * u * t * (p2 - p1) + 3 * t * t * (target - p2)).normalized;
                    if (tangent.sqrMagnitude < .1f) tangent = Vector2.right;
                    var normal = new Vector2(-tangent.y, tangent.x);
                    Vector2 bent = center + normal * (p.y - cross);
                    p = Vector2.Lerp(p, bent, reach);
                }
                if (mirror) p.x = -p.x;
                arm.Vertices[n] = p;
                arm.Uv[n++] = new Vector2((f.Rect.x + px) / f.Texture.width, (f.Rect.y + py) / f.Texture.height);
            }
            arm.Mesh.vertices = arm.Vertices; arm.Mesh.uv = arm.Uv; arm.Mesh.RecalculateBounds();
        }
        void BeginGeometry() { vertices.Clear(); colors.Clear(); triangles.Clear(); }
        void DrawHealth(int index, Vector2 root, float ratio, bool slamming)
        {
            BeginGeometry();
            float x = root.x - 2.5f, y = root.y + (slamming ? .75f : -.85f), w = 5, h = .35f;
            var bg = new Color(.12f, .08f, .10f, .95f); var fill = new Color(.93f, .26f, .44f);
            Triangle(new Vector3(x, y, 0), new Vector3(x + w, y, 0), new Vector3(x + w, y + h, 0), bg, bg, bg);
            Triangle(new Vector3(x, y, 0), new Vector3(x + w, y + h, 0), new Vector3(x, y + h, 0), bg, bg, bg);
            w = (w - .10f) * ratio; x += .05f; y += .05f; h -= .10f;
            Triangle(new Vector3(x, y, 0), new Vector3(x + w, y, 0), new Vector3(x + w, y + h, 0), fill, fill, fill);
            Triangle(new Vector3(x, y, 0), new Vector3(x + w, y + h, 0), new Vector3(x, y + h, 0), fill, fill, fill);
            ApplyGeometry(healthMeshes[index]);
        }
        Vector3 Clip(Vector2 p, Rect? clip)
        {
            if (clip.HasValue) { var r = clip.Value; p.x = Mathf.Clamp(p.x, r.xMin, r.xMax); p.y = Mathf.Clamp(p.y, r.yMin, r.yMax); }
            return p;
        }
        void Triangle(Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
        {
            int n = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
            colors.Add(ca); colors.Add(cb); colors.Add(cc); triangles.Add(n); triangles.Add(n + 1); triangles.Add(n + 2);
        }
        void ApplyGeometry(Mesh mesh)
        { mesh.Clear(); mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds(); }
        void DrawImpact(int arm, Vector2 at, Vector2 radius, float age, uint seed, Rect? clip)
        {
            BeginGeometry(); impactRenderers[arm].enabled = true;
            if (age < 0)
            {
                float pulse = .6f + .15f * Mathf.Sin(age * 22);
                for (int j = 0; j < 40; j++)
                {
                    float a = j * Mathf.PI * 2 / 40, b = (j + 1) * Mathf.PI * 2 / 40;
                    Vector2 p = Vector2.Scale(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), radius),
                        q = Vector2.Scale(new Vector2(Mathf.Cos(b), Mathf.Sin(b)), radius);
                    var c = new Color(.85f, .22f, .38f, pulse);
                    Triangle(Clip(at + p, clip), Clip(at + q, clip), Clip(at + p * .91f, clip), c, c, c);
                    Triangle(Clip(at + q, clip), Clip(at + q * .91f, clip), Clip(at + p * .91f, clip), c, c, c);
                }
                ApplyGeometry(impactMeshes[arm]); return;
            }
            float fade = Mathf.Clamp01(1 - age / .38f), expand = 1 + age * 1.8f;
            if (clip.HasValue && age < .12f)
            {
                float pressFade = 1 - age / .12f;
                var pressed = new Vector2(radius.x * .76f, radius.y * .38f);
                for (int j = 0; j < 32; j++)
                {
                    float a = j * Mathf.PI * 2 / 32, b = (j + 1) * Mathf.PI * 2 / 32;
                    var p = at + Vector2.Scale(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), pressed);
                    var q = at + Vector2.Scale(new Vector2(Mathf.Cos(b), Mathf.Sin(b)), pressed);
                    Triangle(Clip(at, clip), Clip(p, clip), Clip(q, clip),
                        new Color(1, .78f, .81f, pressFade * .85f),
                        new Color(.94f, .37f, .54f, pressFade * .65f), new Color(.94f, .37f, .54f, pressFade * .65f));
                }
            }
            for (int j = 0; j < 36; j++)
            {
                float a = j * Mathf.PI * 2 / 36, b = (j + 1) * Mathf.PI * 2 / 36;
                Vector2 p = Vector2.Scale(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), radius),
                    q = Vector2.Scale(new Vector2(Mathf.Cos(b), Mathf.Sin(b)), radius);
                var edge = new Color(.72f, .39f, .19f, .04f * fade);
                var center = new Color(1, .84f, .5f, (age < .06f ? .65f : .22f) * fade);
                Triangle(Clip(at, clip), Clip(at + p, clip), Clip(at + q, clip), center, edge, edge);
                var ring = new Color(.95f, .72f, .38f, fade * .8f);
                Triangle(Clip(at + p * expand, clip), Clip(at + q * expand, clip), Clip(at + p * (expand - .055f), clip), ring, ring, ring);
                Triangle(Clip(at + q * expand, clip), Clip(at + q * (expand - .055f), clip), Clip(at + p * (expand - .055f), clip), ring, ring, ring);
            }
            for (int j = 0; j < 22; j++)
            {
                float a = OctopusBossPattern.Sample(seed, j * 2) * Mathf.PI * 2;
                float speed = 3 + OctopusBossPattern.Sample(seed, j * 2 + 1) * 8;
                Vector2 p = at + new Vector2(Mathf.Cos(a) * age * speed, Mathf.Sin(a) * age * speed * .5f + Mathf.Sin(age / .38f * Mathf.PI) * 1.5f);
                float r = (.08f + (j % 4) * .045f) * fade;
                var c = new Color(.82f + (j % 3) * .07f, .58f + (j % 3) * .08f, .29f, fade);
                Triangle(Clip(p + new Vector2(-r, -r), clip), Clip(p + new Vector2(r, -r), clip), Clip(p + Vector2.up * r * 1.8f, clip), c, c, c);
            }
            ApplyGeometry(impactMeshes[arm]);
        }
        void DrawArea(OctopusBossSnapshot s)
        {
            areaRenderer.enabled = s.Pattern == 4 && s.Time < 4.65f;
            if (!areaRenderer.enabled) return;
            var r = OctopusBossPattern.HalfRect(s.Half);
            float alpha = s.Time < 1.65f ? .10f + .035f * Mathf.Sin(s.Time * 18) : .035f;
            var c = new Color(1, .20f, .20f, alpha);
            BeginGeometry();
            Triangle(new Vector3(r.xMin, r.yMin, 0), new Vector3(r.xMax, r.yMin, 0), new Vector3(r.xMax, r.yMax, 0), c, c, c);
            Triangle(new Vector3(r.xMin, r.yMin, 0), new Vector3(r.xMax, r.yMax, 0), new Vector3(r.xMin, r.yMax, 0), c, c, c);
            ApplyGeometry(areaMesh);
        }
        void OnDestroy()
        {
            foreach (var a in arms) if (a != null && a.Mesh != null) Destroy(a.Mesh);
            foreach (var a in trails) if (a != null && a.Mesh != null) Destroy(a.Mesh);
            foreach (var mesh in impactMeshes) if (mesh != null) Destroy(mesh);
            foreach (var mesh in healthMeshes) if (mesh != null) Destroy(mesh);
            if (areaMesh != null) Destroy(areaMesh);
            if (effects != null) Destroy(effects);
            if (body != null && body.sprite != null) Destroy(body.sprite);
        }
    }
}
