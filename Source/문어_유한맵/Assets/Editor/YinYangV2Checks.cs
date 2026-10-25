using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OctopusSurvivors.EditorTools
{
    [InitializeOnLoad]
    public static class YinYangV2Checks
    {
        const string Key = "Octopus.YinYangV2Checks";
        static YinYangV2Checks()
        {
            EditorApplication.update += () =>
            {
                const string request = "Logs/YinYangV2.request";
                if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
                File.Delete(request); Run();
            };
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
                    new GameObject("YinYang V2 gameplay capture").AddComponent<YinYangV2Capture>();
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key, false))
                {
                    SessionState.SetBool(Key, false);
                    if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt(Key + ".Exit", 2));
                }
            };
        }
        [MenuItem("문어/음양 V2 실제 플레이 검증 및 GIF 프레임")]
        public static void Run()
        {
            Directory.CreateDirectory("Logs/YinYangV2");
            File.WriteAllText("Logs/YinYangV2/status.txt", "RUNNING");
            ShaderUtil.allowAsyncCompilation = false;
            // Run in the existing scene; Unity restores its edit-mode state after Play Mode.
            SessionState.SetBool(Key, true); EditorApplication.EnterPlaymode();
        }
        internal static void Finish(bool pass)
        {
            File.WriteAllText("Logs/YinYangV2/status.txt", pass ? "PASS" : "FAIL");
            SessionState.SetInt(Key + ".Exit", pass ? 0 : 2); EditorApplication.isPlaying = false;
        }
    }

    public sealed class YinYangV2Capture : MonoBehaviour
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        readonly List<string> checks = new List<string>();
        IEnumerator routine; float deadline; bool finished; int errors;
        static object Call(object o, string name, params object[] args) => o.GetType().GetMethod(name, Hidden).Invoke(o, args);
        static List<SimActor> Actors(GameSession s) => (List<SimActor>)typeof(GameSession).GetField("_actors", Hidden).GetValue(s);
        void Check(bool ok, string description) { if (!ok) throw new Exception(description); checks.Add("PASS: " + description); }
        void Start() { Application.logMessageReceived += OnLog; deadline = Time.realtimeSinceStartup + 240; routine = Run(); }
        void OnLog(string m, string stack, LogType t) { if (t == LogType.Error || t == LogType.Exception) errors++; }
        void OnDestroy() { Application.logMessageReceived -= OnLog; }
        void Update()
        {
            if (finished || routine == null) return;
            try
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("YinYang V2 capture timed out");
                if (routine.MoveNext()) return;
                Finish(true);
            }
            catch (Exception ex) { checks.Add(ex.ToString()); Debug.LogException(ex); Finish(false); }
        }
        void Finish(bool pass)
        {
            finished = true; Time.captureDeltaTime = 0;
            File.WriteAllLines("Logs/YinYangV2/checks.txt", checks); YinYangV2Checks.Finish(pass);
        }
        IEnumerator Run()
        {
            yield return null;
            ushort port;
            using (var socket = new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0)))
                port = (ushort)((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
            typeof(GameBootstrap).GetMethod("StartHost", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(ushort) }, null).Invoke(null, new object[] { port });
            while (GameSession.Instance == null || !GameSession.Instance.IsSpawned) yield return null;
            var session = GameSession.Instance; session.SelectStartingElement(ElementId.Water);
            while (session.HostElement.Value == 0) yield return null;
            session.ToggleLocalReady(); while (!session.CanStart()) yield return null; session.RequestStart();
            while (!session.IsPlaying || OctopusPlayer.Local == null) yield return null;
            var player = OctopusPlayer.Local; player.enabled = session.enabled = false;
            player.Invincible.Value = true; player.transform.position = Vector3.zero; player.NetPos.Value = Vector2.zero;
            player.Slot0.Value = player.Slot1.Value = player.Slot2.Value = player.Slot3.Value = 0;
            Call(session, "ClearFieldForNextStage"); Actors(session).Clear();
            ValidateVisuals();
            Check(Enum.GetValues(typeof(SpecialId)).Length == 3, "only None, Cosmos and YinYang remain");
            Check(ActorKind.PickupCosmos != ActorKind.Droplet, "Cosmos pickup has its own non-projectile network ID");
            for (byte retired = 1; retired <= 4; retired++)
            {
                player.DevSetSpecial(retired);
                Check(player.Special.Value == 0, "retired special rejected: " + retired);
            }
            player.DevSetSpecial((byte)SpecialId.Cosmos);
            Check(player.Special.Value == (byte)SpecialId.Cosmos, "developer menu accepts Cosmos");
            player.Transformed.Value = true; player.SpecialCd.Value = 0;
            Call(player, "CastSpecial");
            Check(Actors(session).FindAll(a => a.Kind == ActorKind.CosmicStar).Count == 54, "Cosmos is reachable through the player special action");
            Check(player.SpecialCd.Value > 0, "Cosmos cast starts the authoritative cooldown");
            Actors(session).Clear(); player.Transformed.Value = false;
            player.DevSetSpecial((byte)SpecialId.YinYang);
            Check(player.Special.Value == (byte)SpecialId.YinYang, "developer menu accepts YinYang");
            Call(session, "DropBossSpecial", Vector2.zero);
            Check(Actors(session).Count == 2 && Actors(session).Exists(a => a.Kind == ActorKind.PickupCosmos) && Actors(session).Exists(a => a.Kind == ActorKind.PickupYinYang), "boss rewards contain exactly Cosmos and YinYang");
            Actors(session).Clear();
            player.Slot0.Value = (byte)ElementId.Fire; player.Slot1.Value = (byte)ElementId.Water;
            player.Slot2.Value = (byte)ElementId.Grass; player.Slot3.Value = (byte)ElementId.Electric;
            player.Combo.Value = (byte)ComboId.Fog; player.Combo1.Value = player.Combo2.Value = 0;
            player.ExtraDash.Value = 1; player.DashCharges.Value = 2; player.DashRechargeLeft.Value = 0;
            var hud = GameHud.Instance; Call(hud, "Update");
            var hudRoot = (RectTransform)typeof(GameHud).GetField("combatRoot", Hidden).GetValue(hud);
            Check(hudRoot.anchorMin == new Vector2(.5f, 0) && hudRoot.anchorMax == new Vector2(.5f, 0), "HUD is anchored at bottom center");
            Check(hudRoot.anchoredPosition.y >= 0, "HUD sits inside the visible bottom edge");
            Check(CombatHudArt.Frame.rect.width == 1855 && CombatHudArt.Frame.rect.height == 365, "HUD uses the exact second supplied artwork");
            var dash = (UnityEngine.UI.Image[])typeof(GameHud).GetField("combatDash", Hidden).GetValue(hud);
            Check(dash[0].fillAmount == 1 && dash[1].fillAmount == 1, "two available dashes show two full charge segments");
            var icons = (UnityEngine.UI.Image[])typeof(GameHud).GetField("combatAttributes", Hidden).GetValue(hud);
            foreach (var icon in icons) Check(icon.color == Color.white && icon.sprite != null, "attribute artwork retains its original bright colors");
            var recipeButtons = (UnityEngine.UI.Button[])typeof(GameHud).GetField("quickCombineButtons", Hidden).GetValue(hud);
            int available = 0;
            foreach (var button in recipeButtons) if (button.gameObject.activeSelf)
            {
                available++;
                Check(button.transform.Find("Recipe artwork").GetComponent<UnityEngine.UI.Image>().sprite != null, "available recipe button has supplied artwork");
                Check(button.GetComponent<CircularButtonHitArea>() != null, "recipe hit area follows its circular silhouette");
            }
            Check(available > 0, "clickable recipe icons are visible on the left");
            var firstButtonRect = recipeButtons[0].GetComponent<RectTransform>();
            var hitArea = recipeButtons[0].GetComponent<CircularButtonHitArea>();
            var center = RectTransformUtility.WorldToScreenPoint(null, firstButtonRect.TransformPoint(firstButtonRect.rect.center));
            var corner = RectTransformUtility.WorldToScreenPoint(null, firstButtonRect.TransformPoint(firstButtonRect.rect.center + new Vector2(firstButtonRect.rect.width * .49f, firstButtonRect.rect.height * .49f)));
            Check(hitArea.IsRaycastLocationValid(center, null) && !hitArea.IsRaycastLocationValid(corner, null), "circular buttons accept center clicks and reject rectangle corners");
            Directory.CreateDirectory("Art/Review");
            ScreenCapture.CaptureScreenshot("Art/Review/bottom-hud-full.png");
            for (int wait = 0; wait < 4; wait++) yield return null;
            player.DevSetSpecial((byte)SpecialId.Cosmos); Call(hud, "Update");
            var devMenu = UnityEngine.Object.FindAnyObjectByType<DevMenu>();
            var devPanel = (GameObject)typeof(DevMenu).GetField("_panel", Hidden).GetValue(devMenu);
            devPanel.SetActive(true);
            ScreenCapture.CaptureScreenshot("Art/Review/cosmos-yinyang-menu.png");
            for (int wait = 0; wait < 4; wait++) yield return null;
            devPanel.SetActive(false); player.DevSetSpecial((byte)SpecialId.YinYang);
            player.DashCharges.Value = 1; player.DashRechargeLeft.Value = GameConfig.DashRecharge * .5f;
            Call(hud, "Update");
            Check(dash[0].fillAmount == 1 && Mathf.Abs(dash[1].fillAmount - .5f) < .001f, "second dash recharges independently after first charge is ready");
            ScreenCapture.CaptureScreenshot("Art/Review/bottom-hud-second-dash.png");
            for (int wait = 0; wait < 4; wait++) yield return null;
            int beforeCombo = player.ComboCount;
            foreach (var button in recipeButtons) if (button.gameObject.activeSelf) { button.onClick.Invoke(); break; }
            for (int wait = 0; wait < 3; wait++) yield return null;
            Check(player.ComboCount == beforeCombo + 1, "clicking a recipe icon performs a real authoritative combination");
            player.Slot0.Value = player.Slot1.Value = player.Slot2.Value = player.Slot3.Value = 0;
            player.Combo.Value = player.Combo1.Value = player.Combo2.Value = 0;

            // Owner tracking and cancellation are exercised through authoritative simulation.
            session.CastCelestial(Vector2.zero, player, SpecialId.YinYang, 1);
            var orbit = Actors(session).Find(a => a.Kind == ActorKind.YinYangOrbit);
            player.transform.position = new Vector3(2, 1, 0);
            Call(session, "TickCelestial", orbit, .1f);
            Check(Vector2.Distance(orbit.Pos, new Vector2(2, 1)) < .001f, "orbit follows the moving owner");
            player.Downed.Value = true; Call(session, "TickCelestial", orbit, .1f);
            Check(orbit.Dead && !Actors(session).Exists(a => a.Kind == ActorKind.YinYangCollision), "downed owner cancels pending impact");
            player.Downed.Value = false; player.transform.position = Vector3.zero; player.NetPos.Value = Vector2.zero;
            Actors(session).Clear();
            var target = (SimActor)Call(session, "Create", ActorKind.Enemy, new Vector2(3.2f, 1.5f), .8f);
            target.Hp = target.MaxHp = 2000; target.Element = ElementId.Grass;
            var far = (SimActor)Call(session, "Create", ActorKind.Enemy, new Vector2(10, 2), .8f);
            far.Hp = far.MaxHp = 2000; far.Element = ElementId.Fire;

            var camera = Camera.main;
            var follow = camera.GetComponent<PlayerCameraFollow>(); if (follow != null) follow.enabled = false;
            camera.orthographicSize = 11.8f; camera.aspect = 16f / 9; camera.rect = new Rect(0, 0, 1, 1);
            camera.transform.position = new Vector3(0, .8f, -10);
            Time.captureDeltaTime = .04f;
            string output = "Art/YinYangV2/GameplayFrames"; Directory.CreateDirectory(output);
            Call(session, "RenderLocal"); yield return null;
            Capture(camera, "Art/YinYangV2/clean_background.png");
            session.CastCelestial(Vector2.zero, player, SpecialId.YinYang, 1);
            bool sawMerge = false, sawBurst = false;
            for (int f = 0; f < 120; f++)
            {
                foreach (var actor in Actors(session).ToArray())
                    if (!actor.Dead && YinYangVisual.Supports(actor.Kind)) Call(session, "TickCelestial", actor, .04f);
                sawMerge |= Actors(session).Exists(a => !a.Dead && a.Kind == ActorKind.YinYangCollision);
                sawBurst |= Actors(session).Exists(a => !a.Dead && a.Kind == ActorKind.YinYangBurst);
                target.HitFlash = Mathf.Max(0, target.HitFlash - .04f);
                Call(session, "RenderLocal"); yield return null;
                Capture(camera, Path.Combine(output, "frame-" + f.ToString("D3") + ".png"));
                if (f == 67) Check(target.Hp == target.MaxHp, "no damage before taiji lock and detonation");
            }
            Check(sawMerge && sawBurst, "authoritative orbit -> merge -> blast transitions reached");
            Check(Mathf.Abs(target.MaxHp - target.Hp - 230) < .01f, "impact damages nearby target exactly once (230)");
            Check(far.Hp == far.MaxHp, "cosmetic debris and wave do not enlarge the damage radius");
            Check(!Actors(session).Exists(a => !a.Dead && YinYangVisual.Supports(a.Kind)), "all four effect actor kinds expire");
            Check(UnityEngine.Object.FindObjectsByType<YinYangVisual>(FindObjectsSortMode.None).Length == 0, "all local effect children cleaned up after expiry");
            Check(errors == 0, "no runtime errors during actual Unity gameplay capture");
            File.WriteAllText("Art/YinYangV2/capture.json", "{\"source\":\"Unity Play Mode / authoritative skill / URP camera\",\"frames\":120,\"width\":960,\"height\":540,\"fps\":25,\"seconds\":4.8}");
        }

        void ValidateVisuals()
        {
            Check(YinYangArt.Comma(false).rect.width == 256 && YinYangArt.Comma(true).rect.width == 256, "both commas use equal 256px smooth sprites");
            Check(YinYangArt.Comma(false).texture.filterMode == FilterMode.Bilinear, "smooth bilinear sampling, no nearest-neighbor enlargement");
            for (int i = 0; i < YinYangArt.BurstFrames; i++)
                Check(YinYangArt.Burst(i) != null && YinYangArt.Burst(i).rect.width == 384, "384px burst frame " + i);
            var go = new GameObject("YinYang snapshot check"); var sr = go.AddComponent<SpriteRenderer>();
            CelestialArt.Show(sr, ActorKind.YinYangOrbit, 1.2f, 2.8f, 5.85f, 0);
            var visual = go.GetComponent<YinYangVisual>();
            var a = go.transform.GetChild(0); var b = go.transform.GetChild(4);
            Check(!sr.enabled && go.GetComponentsInChildren<LineRenderer>().Length == 0, "orbit renders no parent ring and no line renderer");
            Check((a.localPosition + b.localPosition).magnitude < .0001f, "opposite positions around owner");
            Check(Mathf.Abs(Mathf.DeltaAngle(a.localEulerAngles.z, b.localEulerAngles.z)) > 179.99f, "headings differ by exactly 180 degrees");
            Check((a.localScale - b.localScale).sqrMagnitude < .0001f, "both main commas have equal size");
            Check(Vector2.Dot(a.TransformDirection(new Vector3(-.7071068f, .7071068f, 0)), new Vector2(-a.localPosition.y, a.localPosition.x).normalized) > .999f, "rounded head follows orbital tangent");
            Check(YinYangVisual.OrbitPosition(1, 5.85f, 0).sqrMagnitude < .0001f, "orbit reaches exact common center for seamless taiji join");
            Vector3 pose = a.localPosition;
            CelestialArt.Show(sr, ActorKind.YinYangOrbit, 1.2f, 2.8f, 5.85f, 0);
            Check((a.localPosition - pose).sqrMagnitude < .0001f, "snapshot age sampling is deterministic");
            Destroy(go);
        }

        static void Capture(Camera camera, string path)
        {
            var previous = RenderTexture.active; var rt = RenderTexture.GetTemporary(960, 540, 24);
            var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
            try
            {
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
                RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); Destroy(tex); }
        }
    }
}
