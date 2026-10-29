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
    public static class DimensionRiftChecks
    {
        const string Key = "Octopus.DimensionRiftChecks";
        static DimensionRiftChecks()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Key, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode)
                    new GameObject("Dimension Rift checks").AddComponent<DimensionRiftCapture>();
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(Key, false);
                    if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt(Key + ".Exit", 2));
                }
            };
        }
        [MenuItem("문어/차원 균열 실제 플레이 검증")]
        public static void Run()
        {
            Directory.CreateDirectory("Logs/DimensionRift");
            File.WriteAllText("Logs/DimensionRift/status.txt", "RUNNING");
            SystemDataChecks.Run();
            ShaderUtil.allowAsyncCompilation = false;
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool(Key, true); EditorApplication.EnterPlaymode();
        }
        internal static void Finish(bool pass)
        {
            File.WriteAllText("Logs/DimensionRift/status.txt", pass ? "PASS" : "FAIL");
            SessionState.SetInt(Key + ".Exit", pass ? 0 : 2); EditorApplication.isPlaying = false;
        }
    }
    public sealed partial class DimensionRiftCapture : MonoBehaviour
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        readonly List<string> results = new List<string>();
        IEnumerator routine; bool finished; int errors; float deadline;
        static object Call(object o, string name, params object[] args) => o.GetType().GetMethod(name, Hidden).Invoke(o, args);
        static List<SimActor> Actors(GameSession s) => (List<SimActor>)typeof(GameSession).GetField("_actors", Hidden).GetValue(s);
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); results.Add("PASS: " + message); }
        void Start() { deadline = Time.realtimeSinceStartup + 240; Application.logMessageReceived += Log; routine = Run(); }
        void Log(string m, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) errors++; }
        void OnDestroy() => Application.logMessageReceived -= Log;
        void Update()
        {
            if (finished) return;
            try
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Rift validation timed out");
                if (routine.MoveNext()) return;
                Finish(true);
            }
            catch (Exception ex) { results.Add(ex.ToString()); Debug.LogException(ex); Finish(false); }
        }
        void Finish(bool pass)
        {
            finished = true; Time.captureDeltaTime = 0;
            File.WriteAllLines("Logs/DimensionRift/checks.txt", results);
            DimensionRiftChecks.Finish(pass);
        }
        SimActor Enemy(GameSession s, Vector2 pos, float hp, float max = 1000, ActorKind kind = ActorKind.Enemy)
        {
            var enemy = (SimActor)Call(s, "Create", kind, pos, .8f);
            enemy.Hp = hp; enemy.MaxHp = max; enemy.Element = ElementId.Grass;
            return enemy;
        }
        IEnumerator Run()
        {
            yield return null;
            ushort port;
            using (var socket = new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0)))
                port = (ushort)((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
            typeof(GameBootstrap).GetMethod("StartHost", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(ushort) }, null).Invoke(null, new object[] { port });
            while (GameSession.Instance == null || !GameSession.Instance.IsSpawned) yield return null;
            var s = GameSession.Instance; s.SelectStartingElement(ElementId.Water);
            while (s.HostElement.Value == 0) yield return null;
            s.ToggleLocalReady(); while (!s.CanStart()) yield return null; s.RequestStart();
            while (!s.IsPlaying || OctopusPlayer.Local == null) yield return null;
            var p = OctopusPlayer.Local; p.enabled = s.enabled = false;
            p.Invincible.Value = true; p.transform.position = Vector3.zero; p.NetPos.Value = Vector2.zero;
            p.Slot0.Value = p.Slot1.Value = p.Slot2.Value = p.Slot3.Value = 0;
            Actors(s).Clear();
            Check(DimensionRiftArt.Tear(31) != null && DimensionRiftArt.Wave(.5f) != null && DimensionRiftArt.Smoke(5) != null, "runtime artwork loads from Resources");
            Check(ElementRules.SpecialFromPickup(ElementRules.PickupKind(SpecialId.DimensionRift)) == SpecialId.DimensionRift, "pickup identity round trip");
            Check(!SpecialAbilities.IsSupported(SpecialId.DimensionRift), "Rift is no longer an individually selectable special");
            s.CastSpecialConvergence(p.Pos);
            var rift = Actors(s).Find(a => a.Kind == ActorKind.DimensionRift);
            Check(rift != null, "joint skill creates the original damaging Rift");
            Vector2 anchored = rift.Pos;
            p.transform.position = Vector2.right * 3;
            var far = Enemy(s, new Vector2(100, 100), 1000);
            var edge = Enemy(s, new Vector2(2, 1), 150);
            var boss = Enemy(s, new Vector2(3, 2), 149, 1000, ActorKind.Boss);
            Call(s, "TickDimensionRift", rift, 2.49f);
            Check(far.Hp == 1000 && !edge.RiftExecuting, "no damage during crack formation");
            Call(s, "TickDimensionRift", rift, .01f);
            Check(far.Hp == 950 && far.RiftSource == rift, "opening pulse damages and marks targets outside camera range");
            Check(Mathf.Abs(edge.Hp - 100) < .001f && !edge.RiftExecuting, $"exactly ten percent is not executed (hp={edge.Hp:R}, max={edge.MaxHp:R}, threshold={DimensionRiftSettings.Tuning.executionThreshold:R}, executing={edge.RiftExecuting})");
            Check(boss.Hp == 99 && boss.RiftSource == null && !boss.RiftExecuting, "boss takes wave damage but default execution is disabled");
            Check(rift.Pos == anchored, "rift remains at cast position after owner movement");
            Call(s, "ApplyDamage", edge, 1f, StatusMask.None, 0f, 0f);
            Check(edge.RiftExecuting, "other attacks crossing below threshold trigger marked execution");
            float size = edge.Size;
            Call(s, "TickDimensionRift", edge, .35f);
            Check(edge.Size < size && Vector2.Distance(edge.Pos, anchored + Vector2.up * DimensionRiftSettings.Lift) < Vector2.Distance(edge.RiftStart, anchored + Vector2.up * DimensionRiftSettings.Lift), "executed enemy shrinks and travels into airborne fissure");
            Check(!(bool)Call(s, "CanTargetActor", edge), "executing enemies cannot be targeted or receive duplicate damage");
            Call(s, "TickDimensionRift", edge, .36f); Check(edge.Dead, "absorption completes through normal kill/reward path");
            var tuning = DimensionRiftSettings.Tuning;
            tuning.executeBosses = true;
            Check(DimensionRiftSettings.CanExecute(boss), "boss execution can be enabled independently");
            tuning.executeNormalMonsters = false;
            Check(!DimensionRiftSettings.CanExecute(far) && DimensionRiftSettings.CanExecute(boss), "normal and boss toggles are independent");
            tuning.executeNormalMonsters = true; tuning.executeBosses = false;
            boss.Hp = boss.MaxHp = 10000; // Keep the test boss alive during catch-up pulses.
            Call(s, "TickDimensionRift", rift, 1.2f);
            Check(far.Hp == 900, "next pulse occurs after configured 1.2 seconds");
            Call(s, "TickDimensionRift", rift, 3.6f);
            Check(far.Hp == 750, "lag catch-up applies each of the remaining three scheduled pulses exactly once");
            float configuredDuration=SpecialSkillTuning.Data.riftDuration;
            SpecialSkillTuning.Data.riftDuration=4;
            Call(s, "TickDimensionRift", rift, 2.7f);
            SpecialSkillTuning.Data.riftDuration=configuredDuration;
            Check(rift.Dead && Mathf.Abs(rift.Age - 10) < .001f, "full skill lifetime is capped at ten seconds");
            Check(rift.Life<=0,"editing Inspector duration does not strand an already active rift");
            far.Hp = 50;
            Check(!(bool)Call(s, "TryBeginRiftExecution", far), "expired source cannot execute a previously marked enemy");
            Actors(s).Clear();
            Call(s, "DropBossSpecial", Vector2.zero);
            Check(Actors(s).Count == 2 && !Actors(s).Exists(a => a.Kind == ActorKind.PickupDimensionRift), "boss rewards contain only selectable specials");
            Actors(s).Clear(); p.transform.position = Vector3.zero; p.NetPos.Value = Vector2.zero;
            var target = Enemy(s, new Vector2(5, 1), 1000);
            var execute = Enemy(s, new Vector2(-5, 0), 140);
            s.CastDimensionRift(Vector2.zero, p, 1);
            var camera = Camera.main; var follow = camera.GetComponent<PlayerCameraFollow>();
            if (follow != null) follow.enabled = false;
            camera.orthographicSize = 11.8f; camera.aspect = 16f / 9; camera.rect = new Rect(0, 0, 1, 1);
            camera.transform.position = new Vector3(0, 1.8f, -10);
            string output = "Art/DimensionRift/GameplayFrames"; Directory.CreateDirectory(output);
            Time.captureDeltaTime = 1f / 15;
            for (int f = 0; f < 153; f++)
            {
                foreach (var actor in Actors(s).ToArray()) if (!actor.Dead) Call(s, "TickDimensionRift", actor, 1f / 15);
                Call(s, "RenderLocal"); yield return null;
                Capture(camera, Path.Combine(output, "frame-" + f.ToString("D3") + ".png"));
            }
            Check(execute.Dead && target.Hp == 750, "real capture exercises execution and all five field pulses");
            Check(!Actors(s).Exists(a => !a.Dead && DimensionRiftVisual.Supports(a.Kind)), "all rift and wave actors expire");
            Check(UnityEngine.Object.FindObjectsByType<DimensionRiftVisual>(FindObjectsSortMode.None).Length == 0, "render objects and their smoke children clean up after expiry");
            Check(errors == 0, "no runtime errors during Unity gameplay capture");
            var enhancements=ValidateEnhancements(s,p);
            while(enhancements.MoveNext())yield return enhancements.Current;
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
