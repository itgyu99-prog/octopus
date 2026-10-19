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
            var wire = new OctopusBossSnapshot { Pattern = 4, Half = 3, Contacts = 30, Seed = 0x1234abcd, Time = 2.345f,
                Vulnerable = 7.25f, ArmMaxHp = 200, Hp0 = 255, Hp1 = 182, Hp2 = 117, Hp3 = 80, Hp4 = 15, Hp5 = 0 };
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
                if (shot == 0) VerifyCompleteDownFrames(visual);
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
            VerifyLegGate(s, boss, actors);
            Call(s, "KillEnemy", boss);
            Check(!s.BossAlive.Value && s.SpecialUnlocked.Value && s.OctopusBoss.Value.Pattern == 0,
                "defeat cleans up the pattern and preserves the stage 5 special unlock");
            int rewards = 0;
            foreach (var actor in actors) if (!actor.Dead && ElementRules.IsSpecialPickup(actor.Kind))
            { rewards++; Check(FiniteBeach.Walkable(actor.Pos, .4f), "special reward remains reachable on the beach"); }
            Check(rewards == 2, "two original stage 5 special rewards retained");
            File.WriteAllText("Logs/OctopusBossChecks.summary.txt",
                "PASS: stage5 replacement; 30 contacts/4 half areas; six independent limb HP actors; protected core/target filtering; projectile and area limb damage; all-six break opens 10-second stun; core damage only during window; HP restore/relock/attack restart; snapshot HP+stun roundtrip; complete downward poses; continued ocean/sand backdrop; defeat/unlock/reachable rewards; eight screenshots and75 continuous frames.");
        }
        static void VerifyCompleteDownFrames(OctopusBossVisual visual)
        {
            var frames = (Array)typeof(OctopusBossVisual).GetField("slam", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            var arms = (Array)typeof(OctopusBossVisual).GetField("arms", Hidden).GetValue(visual);
            var arm = arms.GetValue(0);
            var mesh = (Mesh)arm.GetType().GetField("Mesh").GetValue(arm);
            for (int frame = 6; frame < 9; frame++)
            for (int mirror = 0; mirror < 2; mirror++)
            {
                var source = frames.GetValue(frame);
                var rect = (Rect)source.GetType().GetField("Rect").GetValue(source);
                Call(visual, "DrawMesh", arm, source, Vector2.zero, new Vector2(1, -5), 1f, mirror != 0);
                Check(Mathf.Abs(mesh.bounds.size.x / mesh.bounds.size.y - rect.width / rect.height) < .0001f,
                    "downward frame keeps the complete source aspect and base " + frame + "/" + mirror);
                var vertices = mesh.vertices; var indices = mesh.triangles; float sign = 0;
                for (int t = 0; t < indices.Length; t += 3)
                {
                    var a = vertices[indices[t + 1]] - vertices[indices[t]];
                    var b = vertices[indices[t + 2]] - vertices[indices[t]];
                    float cross = a.x * b.y - a.y * b.x;
                    if (t == 0) sign = Mathf.Sign(cross);
                    if (cross * sign <= .000001f) throw new Exception("downward frame has a folded/collapsed triangle");
                }
                Check(true, "downward mesh has no folded or collapsed triangles " + frame + "/" + mirror);
            }
            var arena = GameObject.Find("FiniteBeachArena");
            var cap = arena.transform.Find("Upper beach continuation").GetComponent<MeshRenderer>();
            Check(cap.bounds.min.y <= FiniteBeach.Height * .5f,
                "background continuation meets the original upper beach without a gap");
        }
        static void VerifyLegGate(GameSession s, SimActor boss, List<SimActor> actors)
        {
            Call(s, "StartOctopusBoss", boss);
            float hp = boss.Hp;
            Call(s, "ApplyDamage", boss, 100f, StatusMask.Burn, 5f, 20f);
            Check(boss.Hp == hp && boss.BurnTime <= 0, "protected core rejects direct damage and damage-over-time status");
            Check(!(bool)Call(s, "CanTargetActor", boss), "automatic attacks exclude the protected core");
            var limbs = (SimActor[])typeof(GameSession).GetField("octopusArms", Hidden).GetValue(s);
            Check(limbs.Length == 6 && limbs[0].Id != limbs[1].Id, "six independent damageable limb actors");
            float before = limbs[0].Hp;
            s.DamageCircle(limbs[0].Pos, .2f, 10f, StatusMask.None, 0, 0);
            Check(Mathf.Abs(limbs[0].Hp - before + 10) < .01f && limbs[1].Hp == limbs[1].MaxHp,
                "area damage reduces only the contacted limb HP");
            var projectile = (SimActor)Call(s, "Create", ActorKind.Droplet, limbs[0].Pos + Vector2.right * 4, .3f);
            projectile.Damage = 7;
            bool hit = (bool)Call(s, "HitProjectileSegment", projectile, limbs[0].Pos - Vector2.right * 4);
            Check(hit && Mathf.Abs(limbs[0].Hp - before + 17) < .01f, "projectiles hit the visible limb collider");
            projectile.Dead = true;
            Call(s, "ApplyDamage", limbs[0], 100000f, StatusMask.None, 0f, 0f);
            Check(s.OctopusBoss.Value.LivingArms == 5 && s.OctopusBoss.Value.Vulnerable == 0, "one broken limb does not expose the core");
            Call(s, "ApplyDamage", limbs[1], limbs[1].MaxHp * .55f, StatusMask.None, 0f, 0f);
            Call(s, "RenderLocal"); FindFirstObjectByType<OctopusBossVisual>().Preview(s.OctopusBoss.Value);
            Capture(Camera.main, "Logs/OctopusBossFrames/limb-hp.png");
            for (int i = 1; i < 6; i++) Call(s, "ApplyDamage", limbs[i], 100000f, StatusMask.None, 0f, 0f);
            Check(s.OctopusBoss.Value.LivingArms == 0 && s.OctopusBoss.Value.Pattern == 5 &&
                Mathf.Abs(s.OctopusBoss.Value.Vulnerable - 10) < .001f, "last limb immediately interrupts attacks for exactly ten seconds");
            Call(s, "TickBossPattern", boss, 3f, 0f);
            Check(Mathf.Abs(s.OctopusBoss.Value.Vulnerable - 7) < .001f, "stun timer counts combat seconds");
            FindFirstObjectByType<OctopusBossVisual>().Preview(s.OctopusBoss.Value);
            Capture(Camera.main, "Logs/OctopusBossFrames/core-open.png");
            Call(s, "ApplyDamage", boss, 13f, StatusMask.None, 0f, 0f);
            Check(Mathf.Abs(boss.Hp - hp + 13) < .01f, "core becomes damageable during stun");
            Call(s, "TickBossPattern", boss, 6.99f, 0f);
            Check(s.OctopusBoss.Value.Vulnerable > 0, "core remains open just before ten seconds");
            Call(s, "TickBossPattern", boss, .02f, 0f);
            Check(s.OctopusBoss.Value.Vulnerable == 0 && s.OctopusBoss.Value.LivingArms == 6 &&
                s.OctopusBoss.Value.Pattern == 2, "ten-second expiry restores all limbs and restarts attacks");
            for (int i = 0; i < 6; i++) Check(s.OctopusBoss.Value.ArmHealth(i) == 1, "restored limb HP is full");
            float after = boss.Hp; Call(s, "ApplyDamage", boss, 100f, StatusMask.None, 0f, 0f);
            Check(boss.Hp == after, "restored limbs protect the core again without healing core HP");
            Check(boss.Hp < hp, "core damage persists between vulnerability cycles");
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
