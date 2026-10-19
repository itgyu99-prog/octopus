using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OctopusSurvivors.EditorTools
{
    [InitializeOnLoad]
    public static class OctopusBossChecks
    {
        const string Key = "Octopus.Stage5BossCheck";
        static OctopusBossChecks()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
                    new GameObject("Stage 5 octopus boss checks").AddComponent<OctopusBossCapture>();
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key, false))
                {
                    SessionState.SetBool(Key, false);
                    if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt(Key + ".Exit", 2));
                }
            };
        }
        [MenuItem("문어/5스테이지 문어 보스 검증")]
        public static void Run()
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/OctopusBossChecks.status", "RUNNING");
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            ShaderUtil.allowAsyncCompilation = false;
            SessionState.SetBool(Key, true); EditorApplication.EnterPlaymode();
        }
        internal static void Finish(bool pass)
        {
            File.WriteAllText("Logs/OctopusBossChecks.status", pass ? "PASS" : "FAIL");
            SessionState.SetInt(Key + ".Exit", pass ? 0 : 2); EditorApplication.isPlaying = false;
        }
    }
    public sealed class OctopusBossCapture : MonoBehaviour
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        IEnumerator routine;
        float deadline;
        bool finished;
        static object Call(object o, string name, params object[] args) => o.GetType().GetMethod(name, Hidden).Invoke(o, args);
        static void Check(bool condition, string label)
        { if (!condition) throw new Exception(label); Debug.Log("OCTOPUS BOSS PASS: " + label); }
        void Start() { routine = Run(); deadline = Time.realtimeSinceStartup + 240; }
        void Update()
        {
            if (finished) return;
            try
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Octopus boss check timeout");
                if (routine.MoveNext()) return;
                finished = true; OctopusBossChecks.Finish(true);
            }
            catch (Exception e)
            { Debug.LogError("OCTOPUS BOSS FAIL: " + e); finished = true; OctopusBossChecks.Finish(false); }
        }
        IEnumerator Run()
        {
            CheckGeometry();
            yield return null;
            ushort port;
            using (var socket = new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0)))
                port = (ushort)((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
            typeof(GameBootstrap).GetMethod("StartHost", BindingFlags.Static | BindingFlags.NonPublic, null,
                new[] { typeof(ushort) }, null).Invoke(null, new object[] { port });
            while (GameSession.Instance == null || !GameSession.Instance.IsSpawned) yield return null;
            var s = GameSession.Instance; s.SelectStartingElement(ElementId.Water);
            while (s.HostElement.Value == 0) yield return null;
            s.ToggleLocalReady(); while (!s.CanStart()) yield return null; s.RequestStart();
            while (!s.IsPlaying || OctopusPlayer.Local == null) yield return null;
            var p = OctopusPlayer.Local; p.enabled = s.enabled = false;
            p.Invincible.Value = true; p.transform.position = Vector3.zero; p.NetPos.Value = Vector2.zero;
            Call(s, "ClearFieldForNextStage");
            s.Stage.Value = 5; Call(s, "SpawnBoss", 5);
            var actors = (List<SimActor>)typeof(GameSession).GetField("_actors", Hidden).GetValue(s);
            var boss = actors.Find(a => !a.Dead && a.Kind == ActorKind.Boss);
            Check(boss != null && boss.Duration == OctopusBossPattern.VisualMarker && boss.Pos == OctopusBossPattern.Head,
                "stage 5 spawns the stationary octopus, not the former charging boss");
            foreach (float dt in new[] { 1f / 120, 1f / 30, .27f })
            {
                s.OctopusBoss.Value = new OctopusBossSnapshot { Pattern = 2, Seed = 12345 };
                int safety = 0;
                while (s.OctopusBoss.Value.Time < 5.6f && safety++ < 1000) Call(s, "TickBossPattern", boss, dt, 0f);
                Check(s.OctopusBoss.Value.Contacts == 30, "30 actual contacts, including crossed contacts at dt=" + dt);
            }
            s.OctopusBoss.Value = new OctopusBossSnapshot { Pattern = 2, Seed = 17, Time = .74f };
            Call(s, "TickBossPattern", boss, .009f, 0f);
            Check(s.OctopusBoss.Value.Contacts == 0, "counter does not increment during windup");
            Call(s, "TickBossPattern", boss, .002f, 0f);
            Check(s.OctopusBoss.Value.Contacts == 1, "counter increments at the first physical contact");
            s.OctopusBoss.Value = new OctopusBossSnapshot { Pattern = 2, Time = 6.64f, Contacts = 30, Seed = 9 };
            Call(s, "TickBossPattern", boss, .02f, 0f);
            Check(s.OctopusBoss.Value.Pattern == 4, "relay transitions into half-area burrow pattern");
            s.OctopusBoss.Value = new OctopusBossSnapshot { Pattern = 4, Time = 7.29f, Seed = 9 };
            Call(s, "TickBossPattern", boss, .02f, 0f);
            Check(s.OctopusBoss.Value.Pattern == 2 && s.OctopusBoss.Value.Contacts == 0,
                "return completes before the next relay resets its counter");
            var wire = new OctopusBossSnapshot { Pattern = 4, Half = 3, Contacts = 30, Seed = 0x1234abcd, Time = 2.345f };
            using (var writer = new FastBufferWriter(64, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(wire);
                using (var reader = new FastBufferReader(writer, Allocator.Temp))
                { reader.ReadNetworkSerializable(out OctopusBossSnapshot copy); Check(copy.Equals(wire), "host/client snapshot round trip"); }
            }
            var camera = Camera.main; var follow = camera.GetComponent<PlayerCameraFollow>(); if (follow != null) follow.enabled = false;
            camera.orthographicSize = OctopusBossPattern.OverviewSize; camera.aspect = 16f / 9;
            camera.transform.position = new Vector3(0, OctopusBossPattern.OverviewCenter.y, -10);
            Directory.CreateDirectory("Logs/OctopusBossFrames");
            for (int shot = 0; shot < 8; shot++)
            {
                var snap = new OctopusBossSnapshot { Pattern = (byte)(shot < 2 ? 2 : 4), Seed = 12345,
                    Half = (byte)Mathf.Clamp(shot - 2, 0, 3), Time = shot == 0 ? 0 : shot == 1 ? 2.04f : shot < 6 ? 2.4f : shot == 6 ? .8f : 6.5f };
                if (shot == 1) snap.Contacts = 9;
                s.OctopusBoss.Value = snap; Call(s, "RenderLocal"); yield return null;
                var visual = FindFirstObjectByType<OctopusBossVisual>();
                Check(visual != null, "supplied sprite frames are connected to the actual boss actor");
                visual.Preview(snap);
                var armViews = (Array)typeof(OctopusBossVisual).GetField("arms", Hidden).GetValue(visual);
                int visible = 0;
                for (int arm = 0; arm < 6; arm++)
                {
                    var entry = armViews.GetValue(arm);
                    var go = (GameObject)entry.GetType().GetField("Root").GetValue(entry);
                    if (!go.activeSelf) continue;
                    visible++;
                    Vector2 expected = shot >= 2 && shot < 6 ? OctopusBossPattern.HalfRoot(snap.Half, arm) : OctopusBossPattern.RestRoot(arm);
                    Check(Vector2.Distance(go.transform.position, expected) < .001f, "fixed root / selected-half emergence / original-root return");
                }
                Check(visible == (shot == 6 ? 0 : 6), "all six burrow, emerge and return together");
                Capture(camera, "Logs/OctopusBossFrames/shot-" + shot + ".png");
            }
            // Continuous captures expose stepping/teleporting that isolated poses can hide.
            for (int f = 0; f < 75; f++)
            {
                var snap = new OctopusBossSnapshot { Pattern = 4, Half = 2, Seed = 12345, Time = 1.65f + f / 25f };
                s.OctopusBoss.Value = snap; Call(s, "RenderLocal");
                FindFirstObjectByType<OctopusBossVisual>().Preview(snap);
                Capture(camera, "Logs/OctopusBossFrames/half-" + f.ToString("D3") + ".png");
                if (f % 10 == 0) yield return null;
            }
            Call(s, "KillEnemy", boss);
            Check(!s.BossAlive.Value && s.SpecialUnlocked.Value && s.OctopusBoss.Value.Pattern == 0,
                "defeat cleans up the pattern and preserves the stage 5 special unlock");
            int rewards = 0;
            foreach (var actor in actors) if (!actor.Dead && ElementRules.IsSpecialPickup(actor.Kind))
            { rewards++; Check(FiniteBeach.Walkable(actor.Pos, .4f), "special reward remains reachable on the beach"); }
            Check(rewards == 2, "two original stage 5 special rewards retained");
            File.WriteAllText("Logs/OctopusBossChecks.summary.txt",
                "PASS: stage5 replacement; 30 hits at 120fps/30fps/hitch; counter at contact; 4 half rectangles; 3-second/all-six slam; safe-half exclusion; snapshot roundtrip; full-frame assets; defeat/unlock; eight screenshots and 75 continuous half-area frames.");
        }
        static void CheckGeometry()
        {
            int[] perArm = new int[6];
            for (int n = 0; n < 30; n++)
            {
                perArm[OctopusBossPattern.RelayArm(n)]++;
                Check(OctopusBossPattern.Arena.Contains(OctopusBossPattern.RelayTarget(12345, n)), "relay target inside arena " + n);
                if (n > 0) Check(Mathf.Abs(OctopusBossPattern.RelayContact(n) - OctopusBossPattern.RelayContact(n - 1) - .16f) < .00001f, "0.16s contact interval");
            }
            for (int a = 0; a < 6; a++) Check(perArm[a] == 5, "five contacts for arm " + a);
            for (int half = 0; half < 4; half++)
            {
                var r = OctopusBossPattern.HalfRect(half);
                Check(Mathf.Abs(r.width * r.height * 2 - OctopusBossPattern.Arena.width * OctopusBossPattern.Arena.height) < .01f, "exact half area " + half);
                for (int a = 0; a < 6; a++)
                {
                    Check(r.Contains(OctopusBossPattern.HalfRoot(half, a)), "all six emerge inside selected half");
                    int contacts = 0;
                    for (int cycle = 0; cycle < 13; cycle++)
                    {
                        float t = OctopusBossPattern.HalfContact(a, cycle);
                        if (t >= 4.65f) continue;
                        Check(t >= 1.65f, "no damage before the 3-second active window"); contacts++;
                        var target = OctopusBossPattern.HalfTarget(12345, half, a, cycle);
                        Check(r.Contains(target) && OctopusBossPattern.HalfCanHit(target, half, target), "broad physical footprint in half");
                        var safe = OctopusBossPattern.Arena.center * 2 - target;
                        Check(!OctopusBossPattern.HalfCanHit(safe, half, target), "safe half cannot receive slam damage");
                    }
                    Check(contacts >= 12, "every arm slams throughout the 3-second window");
                }
            }
        }
        static void Capture(Camera camera, string path)
        {
            var prior = RenderTexture.active; var rt = RenderTexture.GetTemporary(1280, 720, 24);
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
                RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally { RenderTexture.active = prior; RenderTexture.ReleaseTemporary(rt); Destroy(tex); }
        }
    }
}
