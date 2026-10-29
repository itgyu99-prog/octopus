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
            if(Application.isBatchMode)EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
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
            ValidateCombatChanges(session,player);
            ValidateVisuals();
            Check(Enum.GetValues(typeof(SpecialId)).Length == 4 && !SpecialAbilities.IsSupported(SpecialId.DimensionRift), "Rift identity remains for the joint attack but cannot be individually selected");
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
            Check(Actors(session).FindAll(a => a.Kind == ActorKind.CosmicStar).Count == CosmosSequence.StarCount(1), "Cosmos is reachable through the player special action");
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
            foreach(var icon in hudRoot.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if(icon.name=="Full color icon")Check(icon.type==UnityEngine.UI.Image.Type.Filled &&
                    icon.fillMethod==UnityEngine.UI.Image.FillMethod.Vertical && icon.fillOrigin==(int)UnityEngine.UI.Image.OriginVertical.Bottom,
                    "every cooldown icon restores color linearly from bottom to top");
            float attrCycle=GameConfig.AttributeCycle*GameConfig.AttributeCdMul(player.UpAttrCd.Value);
            player.AttributeCooldownTotal.Value=new Vector4((attrCycle+UpgradeTuning.Data.fireOrbDuration)*.5f,attrCycle*.5f,attrCycle*.5f,attrCycle*.5f);
            var comboTuning=GameplayRules.Tuning(ComboId.Fog);
            player.ComboCooldownTotal.Value=new Vector3((comboTuning.duration+comboTuning.cooldown*GameConfig.ElementCdMul(player.UpElemCd.Value))*.5f,0,0);
            player.SpecialCd.Value=GameConfig.SpecialCooldown*GameConfig.ElementCdMul(player.UpElemCd.Value)*.5f;
            Call(hud,"Update");
            foreach(var icon in icons)Check(Mathf.Abs(icon.fillAmount-.5f)<.001f,"attribute HUD uses replicated real cycle including active fire duration");
            var comboIcons=(UnityEngine.UI.Image[])typeof(GameHud).GetField("combatElements",Hidden).GetValue(hud);
            var specialIcon=(UnityEngine.UI.Image)typeof(GameHud).GetField("combatSpecial",Hidden).GetValue(hud);
            Check(Mathf.Abs(comboIcons[0].fillAmount-.5f)<.001f && Mathf.Abs(specialIcon.fillAmount-.5f)<.001f,"element and special cooldowns also restore from bottom to top");
            Capture(Camera.main,"Logs/YinYangV2/cooldown-half.png",true);
            player.AttributeCooldownTotal.Value=Vector4.zero;
            player.ComboCooldownTotal.Value=Vector3.zero;player.SpecialCd.Value=0;
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
            var mist=Actors(session).Find(a=>a.Kind==ActorKind.YinYangMist);
            Vector2 fieldCenter=YinYangVisual.MistFieldBounds.center;
            Check(Vector2.Distance(mist.Pos,fieldCenter)<.001f,"mist is anchored to the whole field rather than the caster");
            player.transform.position = new Vector3(2, 1, 0);
            Call(session, "TickCelestial", orbit, .1f);
            Call(session,"TickCelestial",mist,.1f);
            Check(Vector2.Distance(mist.Pos,fieldCenter)<.001f,"moving the player does not move field-wide fog");
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
            bool sawMerge = false, sawBurst = false, capturedWhite=false,capturedBlack=false;
            for (int f = 0; f < 120; f++)
            {
                foreach (var actor in Actors(session).ToArray())
                    if (!actor.Dead && YinYangVisual.Supports(actor.Kind)) Call(session, "TickCelestial", actor, .04f);
                sawMerge |= Actors(session).Exists(a => !a.Dead && a.Kind == ActorKind.YinYangCollision);
                sawBurst |= Actors(session).Exists(a => !a.Dead && a.Kind == ActorKind.YinYangBurst);
                target.HitFlash = Mathf.Max(0, target.HitFlash - .04f);
                Call(session, "RenderLocal"); yield return null;
                Capture(camera, Path.Combine(output, "frame-" + f.ToString("D3") + ".png"));
                if(!capturedWhite && Actors(session).Exists(a=>!a.Dead && a.Kind==ActorKind.YinYangHitWhite))
                {Capture(camera,"Logs/YinYangV2/contact-white.png");capturedWhite=true;}
                if(!capturedBlack && Actors(session).Exists(a=>!a.Dead && a.Kind==ActorKind.YinYangHitBlack))
                {Capture(camera,"Logs/YinYangV2/contact-black.png");capturedBlack=true;}
                if (f == 67) Check(target.Hp < target.MaxHp, "orbiting commas deal contact damage before detonation");
            }
            Check(sawMerge && sawBurst, "authoritative orbit -> merge -> blast transitions reached");
            Check(target.MaxHp-target.Hp>345, "near target takes comma contact plus the stronger ten-hit burst");
            Check(Mathf.Abs(far.MaxHp-far.Hp-345)<.05f, "expanded burst reaches outer target and deals exactly 345 total damage");
            Check(Actors(session).Exists(a=>a.Kind==ActorKind.YinYangBurst && a.Mark==10),"burst completes exactly ten authoritative hits");
            Check(!Actors(session).Exists(a => !a.Dead && YinYangVisual.Supports(a.Kind)), "all effect actors including marks and mist expire");
            Check(UnityEngine.Object.FindObjectsByType<YinYangVisual>(FindObjectsSortMode.None).Length == 0, "all local effect children cleaned up after expiry");
            var cosmosChecks=ValidateCosmos(session,player,camera); while(cosmosChecks.MoveNext())yield return cosmosChecks.Current;
            Check(errors == 0, "no runtime errors during actual Unity gameplay capture");
            File.WriteAllText("Art/YinYangV2/capture.json", "{\"source\":\"Unity Play Mode / authoritative skill / URP camera\",\"frames\":120,\"width\":960,\"height\":540,\"fps\":25,\"seconds\":4.8}");
        }

        IEnumerator ValidateCosmos(GameSession session,OctopusPlayer player,Camera camera)
        {
            Actors(session).Clear();
            var target=(SimActor)Call(session,"Create",ActorKind.Enemy,new Vector2(3,0),.8f);
            target.Hp=target.MaxHp=20000;
            session.CastCelestial(Vector2.zero,player,SpecialId.Cosmos,1);
            var dusk=Actors(session).Find(a=>a.Kind==ActorKind.CosmicDusk);
            int count=CosmosSequence.StarCount(1),early=0,formed=0;bool launched=false,restored=false;
            Directory.CreateDirectory("Logs/Cosmos/GameplayFrames");
            for(int frame=0;frame<200;frame++)
            {
                foreach(var actor in Actors(session).ToArray())
                    if(!actor.Dead && CosmosVisual.Supports(actor.Kind))Call(session,"TickCelestial",actor,.04f);
                float elapsed=(frame+1)*.04f;
                bool meteor=Actors(session).Exists(a=>!a.Dead && a.Kind==ActorKind.CosmicMeteor);
                if(elapsed<CosmosSequence.ReleaseTime-.001f)Check(!meteor,"stars wait until formation completes: "+frame);
                if(frame==24)early=Actors(session).FindAll(a=>!a.Dead && a.Kind==ActorKind.CosmicStar && a.Age>=0).Count;
                if(frame==61)formed=Actors(session).FindAll(a=>!a.Dead && a.Kind==ActorKind.CosmicStar && a.Age>=.25f).Count;
                launched|=meteor;
                bool active=Actors(session).Exists(a=>!a.Dead && a.CelestialCast==dusk);
                if(active)Check(dusk.Mark==0 && !dusk.Dead,"field stays dark until the last projectile and impact finish");
                restored|=dusk.Mark==1;
                target.HitFlash=Mathf.Max(0,target.HitFlash-.04f);
                Call(session,"RenderLocal");yield return null;
                Capture(camera,"Logs/Cosmos/GameplayFrames/frame-"+frame.ToString("D3")+".png");
            }
            Check(early>0 && early<10 && formed==count,"a few stars appear first, then the complete star field forms before launch");
            Check(launched && restored && dusk.Dead,"homing comets finish and brightness returns");
            Check(Mathf.Abs(target.MaxHp-target.Hp-3510)<1,"all stars hit and preserve the original total damage budget");
            Check(!Actors(session).Exists(a=>!a.Dead && CosmosVisual.Supports(a.Kind)),"all cosmic actors expire without leaving a dark overlay");
            Actors(session).Clear();session.CastCelestial(Vector2.zero,player,SpecialId.Cosmos,1);
            dusk=Actors(session).Find(a=>a.Kind==ActorKind.CosmicDusk);player.Downed.Value=true;
            Call(session,"TickCelestial",dusk,.1f);player.Downed.Value=false;
            Check(dusk.Mark==1 && !Actors(session).Exists(a=>!a.Dead && a.CelestialCast==dusk),"interrupted casting cancels stars and restores the field");
            Call(session,"TickCelestial",dusk,1f);Actors(session).Clear();Call(session,"RenderLocal");yield return null;
            var follow=camera.GetComponent<PlayerCameraFollow>();follow.Target=player.transform;
            camera.orthographicSize=GameConfig.CameraSize;
            camera.transform.position=FiniteBeach.ClampCamera(player.transform.position+Vector3.down*2.1f,camera);
            var stable=camera.transform.position;
            for(int i=0;i<20;i++){Call(follow,"LateUpdate");Check(Vector3.Distance(stable,camera.transform.position)<.001f,"stationary camera remains stable across follow updates");yield return null;}
            Check(typeof(PlayerCameraFollow).GetMethod("StrongImpact")==null,"impact effects cannot request camera vibration");
        }

        void ValidateCombatChanges(GameSession session,OctopusPlayer player)
        {
            float reach=-1, spread=-1;
            for(int level=1;level<=6;level++)
            {
                Actors(session).Clear();session.CastBasicAbility(player,ElementId.Fire,level);
                var fire=Actors(session).FindAll(a=>a.Kind==ActorKind.FlameAura);
                Check(fire.Count==level+2,"fire upgrade adds a projectile at level "+level);
                if(reach<0)reach=fire[0].SkillTime;
                float span=Vector2.Angle(fire[0].SkillPoint,fire[fire.Count-1].SkillPoint);
                if(spread<0)spread=span;
                Check(Mathf.Abs(span-spread)<.01f,"fire upgrade preserves the original fan width");
                foreach(var orb in fire)Check(Mathf.Abs(orb.SkillTime-reach)<.001f,"fire reach stays fixed at every upgrade level");
                Check(FireOrbGeometry.Launch(fire.Count-1)+UpgradeTuning.Data.fireOrbThrowTime<FireOrbGeometry.ReturnStart(fire[0].Duration),
                    "all upgraded fire shots launch before recall");
            }
            foreach(bool boss in new[]{false,true})
            {
                var points=boss?new[]{new Vector2(.22f,.39f),new Vector2(.78f,.39f),new Vector2(.5f,.58f),new Vector2(.5f,.25f)}:
                    new[]{new Vector2(.22f,.62f),new Vector2(.78f,.62f),new Vector2(.5f,.80f),new Vector2(.5f,.45f)};
                foreach(var uv in points)
                {
                    Vector2 point=BeachGeometry.World(uv,boss);
                    Check(BeachGeometry.Walkable(point,GameConfig.PlayerRadius,boss),"sand reachable in artwork coordinates "+uv+" boss="+boss);
                    if(!boss)Check(Vector2.Distance(FiniteBeach.Move(Vector2.zero,point,GameConfig.PlayerRadius),point)<.15f,"normal beach movement reaches the rendered shore");
                }
            }
            Actors(session).Clear();
            float size=5.85f;
            var comma=(SimActor)Call(session,"Create",ActorKind.Enemy,YinYangVisual.OrbitPosition(.15f,size,0),.8f);
            comma.Hp=comma.MaxHp=10000;
            var opposite=(SimActor)Call(session,"Create",ActorKind.Enemy,YinYangVisual.OrbitPosition(.15f,size,1),.8f);
            opposite.Hp=opposite.MaxHp=10000;
            session.CastCelestial(Vector2.zero,player,SpecialId.YinYang,1);
            Check(Actors(session).Exists(a=>a.Kind==ActorKind.YinYangMist),"casting creates replicated soft field mist");
            Call(session,"TickCelestial",Actors(session).Find(a=>a.Kind==ActorKind.YinYangOrbit),.42f);
            Check(comma.Hp<comma.MaxHp && opposite.Hp<opposite.MaxHp,"both orbiting commas deal swept contact damage");
            Check(Actors(session).Exists(a=>a.Kind==ActorKind.YinYangHitBlack && a.Target==comma),"black comma creates a black impact on its own victim");
            Check(Actors(session).Exists(a=>a.Kind==ActorKind.YinYangHitWhite && a.Target==opposite),"white comma creates a white impact on its own victim");
            foreach(float dt in new[]{1f/60f,.25f,2f})
            {
                Actors(session).Clear();
                var target=(SimActor)Call(session,"Create",ActorKind.Enemy,new Vector2(8.5f,0),.8f);target.Hp=target.MaxHp=2000;
                var outside=(SimActor)Call(session,"Create",ActorKind.Enemy,new Vector2(14,0),.8f);outside.Hp=outside.MaxHp=2000;
                var collision=(SimActor)Call(session,"Effect",ActorKind.YinYangCollision,Vector2.zero,Vector2.zero,size,YinYangVisual.MergeDuration,345f,player,1);
                Call(session,"TickCelestial",collision,YinYangVisual.MergeDuration+.001f);
                var burst=Actors(session).Find(a=>a.Kind==ActorKind.YinYangBurst);
                while(!burst.Dead)Call(session,"TickCelestial",burst,dt);
                Check(burst.Mark==10 && Mathf.Abs(target.MaxHp-target.Hp-345)<.05f,"exact ten-hit total stays stable at timestep "+dt);
                Check(outside.Hp==outside.MaxHp,"burst excludes enemies outside its expanded radius");
                Check(target.Knockback==Vector2.zero,"multi-hit burst does not knock targets out of subsequent hits");
                Check(!Actors(session).Exists(a=>a.Kind==ActorKind.YinYangMark),"hits no longer create YinYang image marks on monsters");
            }
            ValidateGroundCommaDamage(session,player);
            Actors(session).Clear();
        }

        void ValidateGroundCommaDamage(GameSession session,OctopusPlayer player)
        {
            var tuning=SpecialSkillTuning.Data;
            float oldBurstRadius=tuning.smallCommaBurstRadius;
            tuning.smallCommaBurstRadius=1.15f;
            Actors(session).Clear();
            float birth=YinYangVisual.GroundBirth(0);
            Vector2 strike=YinYangVisual.GroundAttackPoint(0,.1f);
            var target=(SimActor)Call(session,"Create",ActorKind.Enemy,strike+Vector2.right*.7f,.8f);
            target.Hp=target.MaxHp=1000;
            var splash=(SimActor)Call(session,"Create",ActorKind.Enemy,strike+Vector2.right*1.35f,.8f);
            splash.Hp=splash.MaxHp=1000;
            var mist=(SimActor)Call(session,"Effect",ActorKind.YinYangMist,Vector2.zero,Vector2.zero,
                tuning.mistRadius,8f,28f,player,1);
            mist.Age=birth+.09f;
            Call(session,"TickCelestial",mist,.02f);
            Check(target.Hp==972,"ground comma swept hit includes monster body radius beyond its centerline");
            Check(splash.Hp==972,"ground comma burst damages a nearby monster within the expanded blast");
            Check(YinYangArt.Comma(false).rect.width==256,"small comma art size stays unchanged while damage reach expands");
            tuning.smallCommaBurstRadius=oldBurstRadius;
            Actors(session).Clear();
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
            Check(go.transform.childCount==8,"orbit contains only the two main commas and their echoes");
            var ground=new GameObject("World ground comma preview");ground.transform.position=YinYangVisual.MistFieldBounds.center;
            var groundSr=ground.AddComponent<SpriteRenderer>();
            int groundBase=1; // The full-field atlas occupies the parent child; each comma has three layers.
            CelestialArt.Show(groundSr,ActorKind.YinYangMist,YinYangVisual.GroundBirth(0)+.1f,3.26f,YinYangVisual.MistFieldBounds.size.x,0);
            var rising=ground.transform.GetChild(groundBase+1).GetComponent<SpriteRenderer>();
            Check(rising.enabled && rising.sprite==YinYangArt.Comma(true),"small white comma rises before its 0.2 second detonation");
            float expectedCommaScale=.7f*(.5f+.5f*Mathf.SmoothStep(0,1,.1f/.22f));
            Check(Mathf.Abs(rising.transform.localScale.x-expectedCommaScale)<.001f,"small comma visual scale stays fixed during damage update");
            CelestialArt.Show(groundSr,ActorKind.YinYangMist,YinYangVisual.GroundBirth(0)+.31f,3.26f,YinYangVisual.MistFieldBounds.size.x,0);
            Check(!rising.enabled && ground.transform.GetChild(groundBase+2).GetComponent<SpriteRenderer>().enabled,"world ground comma explodes after rising");
            ground.SetActive(false);Destroy(ground);
            CelestialArt.Show(sr,ActorKind.YinYangOrbit,1.2f,2.8f,5.85f,0);
            Vector3 pose = a.localPosition;
            CelestialArt.Show(sr, ActorKind.YinYangOrbit, 1.2f, 2.8f, 5.85f, 0);
            Check((a.localPosition - pose).sqrMagnitude < .0001f, "snapshot age sampling is deterministic");
            go.SetActive(false);
            var field=YinYangVisual.MistFieldBounds;
            var fog=new GameObject("Field fog coverage preview");fog.transform.position=field.center;
            CelestialArt.Show(fog.AddComponent<SpriteRenderer>(),ActorKind.YinYangMist,1,YinYangVisual.OrbitDuration+YinYangVisual.MergeDuration+.18f,field.size.x,0);
            var fogSprite=fog.transform.GetChild(0).GetComponent<SpriteRenderer>();
            Check(fog.transform.childCount==1+YinYangVisual.GroundCount*3,"one continuous field atlas plus three render layers per ground comma");
            Check(fogSprite.bounds.min.x<field.min.x && fogSprite.bounds.max.x>field.max.x &&
                fogSprite.bounds.min.y<field.min.y && fogSprite.bounds.max.y>field.max.y,"single fog bank covers the entire field through sea and cliff edges");
            Check(fogSprite.sortingOrder==80,"mist overlays players and monsters");
            var uiSource=File.ReadAllText("Assets/Scripts/UI/UiUtil.cs");
            Check(System.Text.RegularExpressions.Regex.IsMatch(uiSource,@"canvas\.renderMode\s*=\s*RenderMode.ScreenSpaceOverlay"),
                "overlay HUD, combine buttons and developer menu remain above the world fog");
            fog.SetActive(false);Destroy(fog);
            foreach(bool white in new[]{false,true})
            {
                var impact=new GameObject("Contact color preview");
                CelestialArt.Show(impact.AddComponent<SpriteRenderer>(),white?ActorKind.YinYangHitWhite:ActorKind.YinYangHitBlack,.02f,.18f,2f,0);
                var ink=impact.transform.GetChild(0).GetComponent<SpriteRenderer>();
                Check(ink.sprite==YinYangArt.Contact && ink.color==(white?Color.white:Color.black),"supplied impact silhouette has the correct comma color "+white);
                impact.SetActive(false);Destroy(impact);
            }
            var burstPreview=new GameObject("Player symbol scale preview");
            float diameter=5.85f*1.8f*2;
            CelestialArt.Show(burstPreview.AddComponent<SpriteRenderer>(),ActorKind.YinYangBurst,.1f,YinYangVisual.BurstDuration,diameter,0);
            float playerSymbol=burstPreview.transform.GetChild(burstPreview.transform.childCount-1).localScale.x;
            burstPreview.SetActive(false);Destroy(burstPreview);
            var markPreview=new GameObject("Monster symbol scale preview");
            CelestialArt.Show(markPreview.AddComponent<SpriteRenderer>(),ActorKind.YinYangMark,0,.22f,diameter*YinYangVisual.SymbolScale,0);
            Check(!markPreview.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled,"retired monster symbol remains invisible");
            markPreview.SetActive(false);Destroy(markPreview);
            Destroy(go);
        }

        static void Capture(Camera camera, string path, bool includeUi=false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var changed=new List<Canvas>();
            if(includeUi)foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if(canvas.renderMode==RenderMode.ScreenSpaceOverlay)
                {changed.Add(canvas);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1f;}
            Canvas.ForceUpdateCanvases();
            var previous = RenderTexture.active; var rt = RenderTexture.GetTemporary(960, 540, 24);
            var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
            try
            {
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
                RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally {
                foreach(var canvas in changed){canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}
                RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); Destroy(tex);
            }
        }
    }
}
