#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace AshenOath
{
    // Opt-in standalone checks only; absent from the normal Windows build.
    public sealed class OathChecks : MonoBehaviour
    {
        OathGame game;
        IEnumerator steps;
        float deadline;
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("OATH_CHECK_PASS: "+message);}
        public void Run(OathGame value){game=value;deadline=Time.realtimeSinceStartup+75;steps=Checks();}
        void Update()
        {
            try{if(Time.realtimeSinceStartup>deadline)throw new Exception("Check timeout");if(steps!=null&&!steps.MoveNext()){Debug.Log("OATH_ALL_CHECKS_PASS");Application.Quit(0);enabled=false;}}
            catch(Exception e){Debug.LogError("OATH_CHECK_FAILED: "+e);Application.Quit(2);enabled=false;}
        }
        IEnumerator Wait(float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end)yield return null;}
        IEnumerator Checks()
        {
            var wait=Wait(1);while(wait.MoveNext())yield return null;
            Check(game.State==OathState.Title,"title boot");Capture("title");
            game.StartRun();Check(game.State==OathState.Playing&&game.Health==100,"new run");
            // Exercise physical pickup path, not only the direct grant API.
            game.SpawnPickup(game.Position,0);yield return null;yield return null;
            Check(game.Spirits[0].rank==1,"touching an elemental rune summons its spirit");
            for(int i=1;i<4;i++)game.AcquireSpirit(i);
            wait=Wait(.6f);while(wait.MoveNext())yield return null;
            for(int i=0;i<4;i++)Check(game.Spirits[i].view.enabled&&Vector2.Distance(game.Spirits[i].pos,game.Position)<3,"spirit follows knight: "+i);
            game.Pause();float paused=game.Elapsed;wait=Wait(.2f);while(wait.MoveNext())yield return null;
            Check(game.State==OathState.Paused&&game.Elapsed==paused,"pause freezes simulation");game.Resume();
            Check(game.TryDash(Vector2.right),"dash starts");Check(!game.TryDash(Vector2.right),"dash cooldown blocks repeat");
            float hp=game.Health;game.Hurt(20);Check(game.Health==hp,"dash grants invulnerability");
            wait=Wait(.5f);while(wait.MoveNext())yield return null;
            var target=game.SpawnEnemy(game.Position+Vector2.up*4,0);target.hp=target.maxHp=1000;
            foreach(var spirit in game.Spirits)spirit.cooldown=0;
            yield return null;yield return null;
            for(int i=0;i<4;i++)Check(game.Spirits[i].cooldown>2.5f,"independent three-second spirit cooldown: "+i);
            Check(target.hp<1000,"spirits damage foes");
            wait=Wait(.25f);while(wait.MoveNext())yield return null;Capture("contracts");
            for(int i=0;i<8;i++)game.SpawnPickup(game.Position,4);
            yield return null;yield return null;
            Check(game.State==OathState.Blessing,"experience opens blessing choice");Capture("blessing");
            game.Bless(1);Check(game.State==OathState.Playing&&game.MaxHealth==120,"blessing applies and resumes");
            for(int i=0;i<20;i++){var enemy=game.SpawnEnemy(game.Position+Vector2.right*7,0);game.Damage(enemy,10000);}
            Check(game.OathCharge==100,"kills charge oath");Check(game.UnleashOath()&&game.OathCharge==0,"oath consumes charge");
            typeof(OathGame).GetField("<Elapsed>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,135f);
            yield return null;yield return null;
            Check(game.Boss!=null,"final wave spawns forgotten king");
            game.Damage(game.Boss,10000);Check(game.State==OathState.Victory,"boss defeat wins run");Capture("victory");
            game.StartRun();game.Hurt(200);Check(game.State==OathState.Defeat,"zero health loses run");
            game.StartRun();Check(game.Enemies.Count==0&&game.Kills==0&&game.Spirits[0].rank==0,"restart clears prior run");
            game.ReturnToTitle();Check(game.State==OathState.Title,"return to title");
            yield return null;
        }
        void Capture(string name)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            string folder=Path.Combine(Application.dataPath,"..","Screenshots");Directory.CreateDirectory(folder);
            var render=RenderTexture.GetTemporary(1600,900,24);var previous=RenderTexture.active;var camera=game.ViewCamera;
            var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            try
            {
                foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;}
                Canvas.ForceUpdateCanvases();camera.targetTexture=render;camera.Render();RenderTexture.active=render;
                var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();
                File.WriteAllBytes(Path.Combine(folder,name+".png"),texture.EncodeToPNG());Destroy(texture);
            }
            finally{camera.targetTexture=null;foreach(var c in canvases)c.renderMode=RenderMode.ScreenSpaceOverlay;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(render);}
        }
    }
}
#endif
