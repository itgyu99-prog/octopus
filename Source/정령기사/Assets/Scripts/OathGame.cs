using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshenOath
{
    public enum OathState { Title, Playing, Paused, Blessing, Victory, Defeat }
    public sealed class OathGame : MonoBehaviour
    {
        public sealed class Spirit
        {
            public int rank;
            public float cooldown;
            public SpriteRenderer view, glow, emblem;
            public Vector2 pos;
        }
        public sealed class Foe
        {
            public Vector2 pos;
            public float hp, maxHp, attack, slow, flash;
            public int type;
            public SpriteRenderer view, shadow;
        }
        sealed class Shot
        {
            public Vector2 pos, velocity;
            public float life, damage;
            public int element;
            public bool hostile;
            public SpriteRenderer view;
        }
        sealed class Pickup
        {
            public Vector2 pos;
            public int type; // 0..3 spirits; 4 experience; 5 health
            public SpriteRenderer view, glow;
        }
        sealed class Field
        {
            public Vector2 pos;
            public float life=2.5f, pulse, damage;
            public SpriteRenderer ring, glow;
        }
        sealed class Fleck
        {
            public Vector2 velocity;
            public float life, maxLife;
            public SpriteRenderer view;
        }
        public OathState State { get; private set; } = OathState.Title;
        public readonly Spirit[] Spirits = {new Spirit(),new Spirit(),new Spirit(),new Spirit()};
        public readonly List<Foe> Enemies = new List<Foe>();
        readonly List<Shot> shots=new List<Shot>();
        readonly List<Pickup> pickups=new List<Pickup>();
        readonly List<Field> fields=new List<Field>();
        readonly List<Fleck> flecks=new List<Fleck>();
        public Vector2 Position { get; private set; }
        public float Health { get; private set; }=100;
        public float MaxHealth { get; private set; }=100;
        public float Elapsed { get; private set; }
        public float OathCharge { get; private set; }
        public float DashCooldown { get; private set; }
        public int Level { get; private set; }=1;
        public int Experience { get; private set; }
        public int ExperienceNeeded => 5+Level*3;
        public int Kills { get; private set; }
        public int Wave => Mathf.Min(4,1+(int)(Elapsed/45f));
        public float AttackMultiplier { get; private set; }=1;
        public float SpiritInterval { get; private set; }=3;
        public int Best => PlayerPrefs.GetInt("AshenOath.v1.Best",0);
        public bool Muted { get; private set; }
        public Foe Boss { get; private set; }
        public OathHud Hud { get; private set; }
        public Camera ViewCamera { get; private set; }
        SpriteRenderer knight, shadow, halo;
        Transform actors, terrain;
        Vector2 aim=Vector2.right, dashDirection;
        float swordCd, dashLeft, invulnerable, spawnLeft, auraTime, shake;
        int previousWave=1;
        bool bossSpawned;
        bool selfCheck;
        AudioSource audioFx, ambient;
        AudioClip swordSound, pickupSound, hurtSound, dashSound;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if(FindFirstObjectByType<OathGame>()==null)new GameObject("Ashen Oath").AddComponent<OathGame>(); }
        void Awake()
        {
            Application.targetFrameRate=60;
            selfCheck=Array.IndexOf(Environment.GetCommandLineArgs(),"-oath-selftest")>=0;
            UnityEngine.Random.InitState(Environment.TickCount);
            var camGo=new GameObject("Sanctum Camera");ViewCamera=camGo.AddComponent<Camera>();camGo.tag="MainCamera";
            ViewCamera.orthographic=true;ViewCamera.orthographicSize=9.2f;ViewCamera.backgroundColor=OathArt.Ink;
            ViewCamera.transform.position=new Vector3(0,0,-10);camGo.AddComponent<AudioListener>();
            terrain=new GameObject("The Hollow Sanctum").transform;OathArt.BuildSanctum(terrain);
            actors=new GameObject("Actors").transform;
            shadow=OathArt.Node("glow",Vector2.zero,new Vector2(1.8f,.55f),new Color(0,0,0,.8f),0,actors);
            halo=OathArt.Node("glow",Vector2.zero,Vector2.one*4,new Color(.38f,.57f,.62f,.23f),1,actors);
            knight=OathArt.Node("knight",Vector2.zero,Vector2.one*1.65f,Color.white,12,actors);
            for(int i=0;i<4;i++)
            {
                Spirits[i].view=OathArt.Node("spirit",Vector2.zero,Vector2.one*.7f,OathArt.Elements[i],13,actors);
                Spirits[i].glow=OathArt.Node("glow",Vector2.zero,Vector2.one*2,OathArt.Elements[i]*new Color(1,1,1,.25f),4,actors);
                Spirits[i].emblem=OathArt.Node("rune"+i,Vector2.zero,Vector2.one*.25f,OathArt.Elements[i],14,actors);
            }
            SetupAudio();Hud=gameObject.AddComponent<OathHud>();Hud.Initialize(this);ReturnToTitle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-oath-selftest")>=0)gameObject.AddComponent<OathChecks>().Run(this);
#endif
        }
        public void StartRun()
        {
            ClearRun();State=OathState.Playing;Position=new Vector2(0,-3);
            Health=MaxHealth=100;Elapsed=0;Level=1;Experience=Kills=0;OathCharge=0;DashCooldown=0;
            AttackMultiplier=1;SpiritInterval=3;swordCd=dashLeft=invulnerable=0;spawnLeft=2;bossSpawned=false;Boss=null;previousWave=1;
            for(int i=0;i<4;i++){Spirits[i].rank=0;Spirits[i].cooldown=0;}
            SpawnPickup(Position+new Vector2(2.1f,.3f),0);
            SpawnPickup(new Vector2(-10,5),1);SpawnPickup(new Vector2(10,6),2);SpawnPickup(new Vector2(8,-8),3);
            ViewCamera.transform.position=new Vector3(Position.x,Position.y,-10);
            Hud.ShowPlay();Hud.Toast("첫 번째 서약", "주황빛 룬에 다가가 잿불 정령과 계약하세요.");
        }
        public void ReturnToTitle()
        {
            ClearRun();State=OathState.Title;Position=new Vector2(6,0);ViewCamera.transform.position=new Vector3(0,0,-10);
            Hud.ShowTitle();
        }
        void ClearRun()
        {
            foreach(var e in Enemies){Destroy(e.view.gameObject);Destroy(e.shadow.gameObject);}Enemies.Clear();
            foreach(var s in shots)Destroy(s.view.gameObject);shots.Clear();
            foreach(var p in pickups){Destroy(p.view.gameObject);Destroy(p.glow.gameObject);}pickups.Clear();
            foreach(var f in fields){Destroy(f.ring.gameObject);Destroy(f.glow.gameObject);}fields.Clear();
            foreach(var f in flecks)Destroy(f.view.gameObject);flecks.Clear();
            Boss=null;
        }
        void Update()
        {
            float dt=Mathf.Min(Time.deltaTime,.05f);
            if(Input.GetKeyDown(KeyCode.M))ToggleSound();
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(State==OathState.Playing)Pause();else if(State==OathState.Paused)Resume();
            }
            if(State==OathState.Title&&Input.GetKeyDown(KeyCode.Return))StartRun();
            if(State==OathState.Playing)
            {
                Elapsed+=dt;swordCd-=dt;invulnerable-=dt;DashCooldown=Mathf.Max(0,DashCooldown-dt);
                Vector2 movement=new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),
                    (Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0)).normalized;
                Vector2 mouse=(Vector2)ViewCamera.ScreenToWorldPoint(Input.mousePosition)-Position;
                if(mouse.sqrMagnitude>.05f)aim=mouse.normalized;
                if(Input.GetKeyDown(KeyCode.Space)||Input.GetMouseButtonDown(1))TryDash(movement.sqrMagnitude>.1f?movement:aim);
                if(dashLeft>0){dashLeft-=dt;Position+=dashDirection*18*dt;Particle(Position,OathArt.Elements[1],.32f,.3f,Vector2.zero);}
                else Position+=movement*5.9f*dt;
                Position=Clamp(Position);
                if((Input.GetMouseButton(0)||Input.GetKey(KeyCode.J))&&swordCd<=0)SwordAttack();
                if(Input.GetKeyDown(KeyCode.E))UnleashOath();
                TickSpirits(dt);
                if(State==OathState.Playing)TickEnemies(dt);
                if(State==OathState.Playing)TickShots(dt);
                if(State==OathState.Playing)TickFields(dt);
                if(State==OathState.Playing)TickPickups(dt);
                if(State==OathState.Playing)TickWaves(dt);
            }
            if(State==OathState.Playing||State==OathState.Title)TickParticles(dt);
            UpdateVisuals(dt);Hud.Tick(dt);
        }
        public static Vector2 Clamp(Vector2 p) => new Vector2(Mathf.Clamp(p.x,-20.8f,20.8f),Mathf.Clamp(p.y,-12,12));
        public void Pause(){if(State!=OathState.Playing)return;State=OathState.Paused;Hud.ShowPause();}
        public void Resume(){if(State!=OathState.Paused)return;State=OathState.Playing;Hud.ShowPlay();}
        public bool TryDash(Vector2 direction)
        {
            if(State!=OathState.Playing||DashCooldown>0)return false;
            dashDirection=direction.sqrMagnitude>.01f?direction.normalized:Vector2.right;
            dashLeft=.22f;DashCooldown=1.6f;invulnerable=.3f;Play(dashSound,.22f);return true;
        }
        void SwordAttack()
        {
            swordCd=.43f;Play(swordSound,.18f);shake=.055f;
            Vector2[] points=new Vector2[18];float angle=Mathf.Atan2(aim.y,aim.x);
            for(int i=0;i<points.Length;i++)points[i]=Position+OathArt.Polar(angle-.95f+i*1.9f/17,1.85f);
            var slash=OathArt.Line(actors,new Color(.9f,.88f,.76f,.85f),.09f,25,points);Destroy(slash.gameObject,.14f);
            foreach(var e in Enemies)
                if(e.hp>0&&Vector2.Distance(e.pos,Position)<2.5f&&Vector2.Dot((e.pos-Position).normalized,aim)>.1f)
                {Damage(e,27*AttackMultiplier);e.pos=Clamp(e.pos+aim*.3f);}
        }
        public void AcquireSpirit(int index)
        {
            if(index<0||index>3)return;
            var s=Spirits[index];bool first=s.rank==0;s.rank=Mathf.Min(3,s.rank+1);s.cooldown=0;
            if(!first)Health=Mathf.Min(MaxHealth,Health+12);
            Burst(Position,OathArt.Elements[index],24);Play(pickupSound,.4f);
            Hud.Toast(first?OathArt.Names[index]+"의 계약":OathArt.Names[index]+"의 공명",first?"정령이 곁에서 함께 싸웁니다.":"정령의 힘이 성장했습니다.  +12 생명력");
        }
        void TickSpirits(float dt)
        {
            for(int i=0;i<4;i++)
            {
                var s=Spirits[i];if(s.rank==0)continue;s.cooldown-=dt;
                if(s.cooldown<=0&&Nearest(Position,12)!=null){CastSpirit(i);s.cooldown=SpiritInterval;}
            }
        }
        public void CastSpirit(int i)
        {
            var s=Spirits[i];if(s.rank==0)return;var e=Nearest(Position,12);if(e==null)return;
            float damage=(20+8*s.rank)*AttackMultiplier;Vector2 from=s.pos;
            Burst(from,OathArt.Elements[i],5);
            switch(i)
            {
                case 0:
                    var f=new Field{pos=e.pos,damage=damage*.42f};
                    f.ring=OathArt.Node("ring",f.pos,Vector2.one*3.7f,OathArt.Elements[0]*new Color(1,1,1,.8f),3,actors);
                    f.glow=OathArt.Node("glow",f.pos,Vector2.one*4.2f,OathArt.Elements[0]*new Color(1,1,1,.7f),2,actors);fields.Add(f);break;
                case 1:
                    Vector2 dir=(e.pos-from).normalized;float angle=Mathf.Atan2(dir.y,dir.x);
                    for(int n=-1;n<=1;n++)SpawnShot(from,OathArt.Polar(angle+n*.15f)*11,damage,1,false);break;
                case 2:
                    var nearest=new List<Foe>();foreach(var foe in Enemies)if(foe.hp>0&&Vector2.Distance(foe.pos,Position)<10)nearest.Add(foe);
                    nearest.Sort((a,b)=>(a.pos-Position).sqrMagnitude.CompareTo((b.pos-Position).sqrMagnitude));
                    for(int n=0;n<Mathf.Min(3+s.rank-1,nearest.Count);n++)
                    {
                        var target=nearest[n];Damage(target,damage);target.slow=1.8f;
                        var vine=OathArt.Line(actors,OathArt.Elements[2],.055f,8,from,Vector2.Lerp(from,target.pos,.4f)+Vector2.up*.4f,target.pos);Destroy(vine.gameObject,.32f);
                        for(int k=0;k<5;k++)Particle(target.pos+OathArt.Polar(k*1.3f,.5f),OathArt.Elements[2],.25f,.45f,Vector2.up*.7f,"diamond");
                    }break;
                case 3:
                    var hit=new HashSet<Foe>();Vector2 chain=from;
                    for(int jump=0;jump<3+s.rank;jump++)
                    {
                        Foe target=null;float best=jump==0?14:6;
                        foreach(var foe in Enemies){float d=Vector2.Distance(foe.pos,chain);if(foe.hp>0&&!hit.Contains(foe)&&d<best){best=d;target=foe;}}
                        if(target==null)break;hit.Add(target);
                        var arc=OathArt.Line(actors,OathArt.Elements[3],.075f,21,chain,Vector2.Lerp(chain,target.pos,.33f)+Vector2.up*.3f,
                            Vector2.Lerp(chain,target.pos,.65f)+Vector2.down*.3f,target.pos);Destroy(arc.gameObject,.2f);Damage(target,damage*.85f);chain=target.pos;
                    }break;
            }
        }
        public bool UnleashOath()
        {
            if(State!=OathState.Playing||OathCharge<100)return false;
            OathCharge=0;invulnerable=1;shake=.3f;Play(pickupSound,.65f);
            foreach(var e in Enemies)if(e.hp>0&&Vector2.Distance(e.pos,Position)<9)Damage(e,85*AttackMultiplier);
            for(int i=0;i<4;i++)if(Spirits[i].rank>0){CastSpirit(i);Spirits[i].cooldown=0;}
            for(int i=0;i<60;i++)Particle(Position,OathArt.Gold,.25f,.8f,OathArt.Polar(i*Mathf.PI/30,10));
            Hud.Toast("서약 해방", "계약한 정령들의 힘이 성소를 가릅니다.");return true;
        }
        public Foe SpawnEnemy(Vector2 p,int type)
        {
            var e=new Foe{pos=Clamp(p),type=type};e.hp=e.maxHp=type==2?1900:type==1?55+Wave*8:32+Wave*8;e.attack=type==2?2.3f:UnityEngine.Random.Range(.5f,2.5f);
            e.view=OathArt.Node(type==2?"boss":type==1?"wraith":"shade",e.pos,Vector2.one*(type==2?3:1.4f),Color.white,10,actors);
            e.shadow=OathArt.Node("glow",e.pos+Vector2.down*.5f,new Vector2(type==2?3:1.7f,.65f),new Color(0,0,0,.6f),0,actors);
            Enemies.Add(e);if(type==2)Boss=e;return e;
        }
        void TickWaves(float dt)
        {
            if(Wave!=previousWave)
            {
                previousWave=Wave;Hud.Toast(Wave==4?"망각의 왕": "제 "+Wave+" 파동",Wave==4?"왕을 쓰러뜨리고 서약을 완성하세요.":"더 깊은 어둠이 성소로 밀려옵니다.");
                if(Wave<4){SpawnPickup(Clamp(Position+Vector2.up*3),5);SpawnPickup(Clamp(Position+Vector2.right*4),UnityEngine.Random.Range(0,4));}
            }
            if(Wave==4&&!bossSpawned){bossSpawned=true;SpawnEnemy(new Vector2(0,9),2);}
            spawnLeft-=dt;if(spawnLeft>0||Enemies.Count>65)return;
            spawnLeft=Wave==4?1.65f:Mathf.Max(.45f,1.5f-Elapsed*.007f);
            int count=Wave>=3?2:1;
            for(int i=0;i<count;i++)
            {
                Vector2 p=Clamp(Position+OathArt.Polar(UnityEngine.Random.Range(0f,Mathf.PI*2),UnityEngine.Random.Range(11f,15f)));
                if(Vector2.Distance(p,Position)<7)p=Clamp(-Position+OathArt.Polar(UnityEngine.Random.value*6.28f,8));
                SpawnEnemy(p,Elapsed>35&&UnityEngine.Random.value<.28f?1:0);
            }
        }
        void TickEnemies(float dt)
        {
            for(int i=Enemies.Count-1;i>=0;i--)
            {
                var e=Enemies[i];if(e.hp<=0){Destroy(e.view.gameObject);Destroy(e.shadow.gameObject);Enemies.RemoveAt(i);continue;}
                e.attack-=dt;e.slow=Mathf.Max(0,e.slow-dt);e.flash=Mathf.Max(0,e.flash-dt);
                Vector2 dir=Position-e.pos;float distance=dir.magnitude;
                float speed=e.type==2?1.15f:e.type==1?1.3f:1.75f+Wave*.15f;
                if(e.slow>0)speed*=.38f;
                if(e.type!=1||distance>6)e.pos+=dir.normalized*speed*dt;
                else if(distance<4)e.pos-=dir.normalized*speed*.5f*dt;
                if(distance<(e.type==2?1.5f:.9f))Hurt(e.type==2?24:9);
                if(e.attack<=0)
                {
                    if(e.type==1){e.attack=2.8f;SpawnShot(e.pos,dir.normalized*4.5f,12,3,true);}
                    if(e.type==2)
                    {
                        e.attack=e.hp<e.maxHp*.5f?1.7f:2.4f;
                        for(int k=0;k<14;k++)SpawnShot(e.pos,OathArt.Polar(k*Mathf.PI/7+Elapsed*.3f)*3.6f,16,0,true);
                        Burst(e.pos,OathArt.Elements[0],18);
                    }
                }
                e.pos=Clamp(e.pos);e.view.transform.position=(Vector3)e.pos+Vector3.up*(Mathf.Sin(Time.time*4+i)*.045f);
                e.shadow.transform.position=e.pos+Vector2.down*.5f;e.view.flipX=dir.x<0;
                e.view.color=e.flash>0?new Color(1,.55f,.5f):Color.white;e.view.sortingOrder=10-Mathf.RoundToInt(e.pos.y*2);
            }
        }
        public void Damage(Foe e,float damage)
        {
            if(e.hp<=0)return;e.hp-=damage;e.flash=.12f;Burst(e.pos,e.type==2?OathArt.Gold:OathArt.Hex("AB727F"),3);
            if(e.hp>0)return;Kills++;OathCharge=Mathf.Min(100,OathCharge+5);SpawnPickup(e.pos,4);
            if(UnityEngine.Random.value<.05f)SpawnPickup(e.pos+Vector2.right*.5f,5);
            if(e.type==2)Finish(true);
        }
        public void Hurt(float damage)
        {
            if(State!=OathState.Playing||invulnerable>0)return;
            Health=Mathf.Max(0,Health-damage);invulnerable=.65f;shake=.2f;Play(hurtSound,.3f);Burst(Position,OathArt.Hex("CB7072"),8);
            if(Health<=0)Finish(false);
        }
        void Finish(bool won)
        {
            State=won?OathState.Victory:OathState.Defeat;
            if(!selfCheck){PlayerPrefs.SetInt("AshenOath.v1.Best",Mathf.Max(Best,Kills));PlayerPrefs.Save();}
            Hud.ShowEnd(won);
        }
        Foe Nearest(Vector2 pos,float range)
        {
            Foe result=null;foreach(var e in Enemies){float d=Vector2.Distance(e.pos,pos);if(e.hp>0&&d<range){range=d;result=e;}}return result;
        }
        void SpawnShot(Vector2 pos,Vector2 velocity,float damage,int element,bool hostile)
        {
            var s=new Shot{pos=pos,velocity=velocity,damage=damage,element=element,hostile=hostile,life=hostile?6:1.6f};
            s.view=OathArt.Node(hostile?"circle":"diamond",pos,hostile?Vector2.one*.3f:new Vector2(.2f,.7f),
                hostile?OathArt.Hex("EC7890"):OathArt.Elements[element],18,actors);
            s.view.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(velocity.y,velocity.x)*Mathf.Rad2Deg-90);shots.Add(s);
        }
        void TickShots(float dt)
        {
            for(int i=shots.Count-1;i>=0;i--)
            {
                var s=shots[i];Vector2 previous=s.pos;s.pos+=s.velocity*dt;s.life-=dt;
                if(s.hostile){if(SegmentDistance(Position,previous,s.pos)<.5f){Hurt(s.damage);s.life=0;}}
                else foreach(var e in Enemies)
                    if(e.hp>0&&SegmentDistance(e.pos,previous,s.pos)<(e.type==2?1.1f:.65f))
                    {Damage(e,s.damage);if(s.element==1)e.slow=2.2f;s.life=0;break;}
                s.view.transform.position=s.pos;
                if(s.life<=0||Mathf.Abs(s.pos.x)>23||Mathf.Abs(s.pos.y)>15){Destroy(s.view.gameObject);shots.RemoveAt(i);}
            }
        }
        static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b){Vector2 v=b-a;return Vector2.Distance(p,a+v*Mathf.Clamp01(Vector2.Dot(p-a,v)/Mathf.Max(.00001f,v.sqrMagnitude)));}
        void TickFields(float dt)
        {
            for(int i=fields.Count-1;i>=0;i--)
            {
                var f=fields[i];f.life-=dt;f.pulse-=dt;
                if(f.pulse<=0){f.pulse=.35f;foreach(var e in Enemies)if(e.hp>0&&Vector2.Distance(e.pos,f.pos)<1.85f)Damage(e,f.damage);Particle(f.pos+UnityEngine.Random.insideUnitCircle*1.3f,OathArt.Elements[0],.2f,.55f,Vector2.up);}
                f.ring.transform.Rotate(0,0,dt*12);if(f.life<=0){Destroy(f.ring.gameObject);Destroy(f.glow.gameObject);fields.RemoveAt(i);}
            }
        }
        public void SpawnPickup(Vector2 pos,int type)
        {
            var p=new Pickup{pos=pos,type=type};Color c=type<4?OathArt.Elements[type]:type==4?OathArt.Gold:OathArt.Hex("C98495");
            p.view=OathArt.Node(type<4?"rune"+type:"gem",pos,Vector2.one*(type<4?.7f:.25f),c,6,actors);
            p.glow=OathArt.Node("glow",pos,Vector2.one*(type<4?3:1.1f),c*new Color(1,1,1,.55f),2,actors);pickups.Add(p);
        }
        void TickPickups(float dt)
        {
            for(int i=pickups.Count-1;i>=0;i--)
            {
                var p=pickups[i];float d=Vector2.Distance(p.pos,Position);
                if(p.type==4&&d<4)p.pos=Vector2.MoveTowards(p.pos,Position,dt*9);
                p.view.transform.position=p.pos+Vector2.up*Mathf.Sin(Time.time*2.6f+i)*.12f;p.glow.transform.position=p.pos;
                if(Vector2.Distance(p.pos,Position)>.85f)continue;
                if(p.type<4)AcquireSpirit(p.type);else if(p.type==4){Experience++;if(Experience>=ExperienceNeeded){Experience-=ExperienceNeeded;Level++;State=OathState.Blessing;Hud.ShowBlessing();}}
                else {Health=Mathf.Min(MaxHealth,Health+20);Burst(Position,OathArt.Hex("C98495"),8);}
                Destroy(p.view.gameObject);Destroy(p.glow.gameObject);pickups.RemoveAt(i);
                if(State!=OathState.Playing)break;
            }
        }
        public void Bless(int choice)
        {
            if(State!=OathState.Blessing)return;
            switch(choice){case 0:AttackMultiplier+=.18f;break;case 1:MaxHealth+=20;Health=Mathf.Min(MaxHealth,Health+40);break;case 2:SpiritInterval=Mathf.Max(1.5f,SpiritInterval-.2f);OathCharge=Mathf.Min(100,OathCharge+25);break;}
            State=OathState.Playing;Hud.ShowPlay();Play(pickupSound,.3f);
        }
        void UpdateVisuals(float dt)
        {
            bool title=State==OathState.Title;float size=title?4.6f:1.65f;
            knight.transform.position=Position+Vector2.up*Mathf.Sin(Time.time*3)*.025f;knight.transform.localScale=Vector3.one*size;
            knight.flipX=!title&&aim.x<0;knight.sortingOrder=12-Mathf.RoundToInt(Position.y*2);
            knight.color=invulnerable>0&&State==OathState.Playing?new Color(.65f,.82f,.94f):Color.white;
            shadow.transform.position=Position+Vector2.down*(title?2:.62f);shadow.transform.localScale=new Vector3(size*1.3f,size*.35f,1);
            halo.transform.position=Position;halo.transform.localScale=Vector3.one*(title?9:4);
            for(int i=0;i<4;i++)
            {
                var s=Spirits[i];bool visible=title||s.rank>0;s.view.enabled=s.glow.enabled=s.emblem.enabled=visible;if(!visible)continue;
                Vector2 offset=title?OathArt.Polar(Time.time*.15f+i*Mathf.PI/2,3.4f):new Vector2((i<2?-1:1)*(1.05f+(i%2)*.45f),.45f+(i%2)*1.0f);
                Vector2 target=Position+offset+Vector2.up*Mathf.Sin(Time.time*2.5f+i)*.15f;
                s.pos=Vector2.Lerp(s.pos,target,1-Mathf.Exp(-8*dt));s.view.transform.position=s.pos;s.glow.transform.position=s.pos;
                s.view.transform.localScale=Vector3.one*(title?1.25f:.67f+s.rank*.055f);
                s.view.sortingOrder=knight.sortingOrder+1;
                s.emblem.transform.position=s.pos+Vector2.up*(title?.83f:.53f);
                s.emblem.transform.localScale=Vector3.one*(title?.3f:.2f);s.emblem.sortingOrder=s.view.sortingOrder+1;
            }
            if(!title)
            {
                float halfWidth=ViewCamera.orthographicSize*ViewCamera.aspect;
                Vector2 center=new Vector2(Mathf.Clamp(Position.x,-Mathf.Max(0,22-halfWidth),Mathf.Max(0,22-halfWidth)),Mathf.Clamp(Position.y,-4,4));
                Vector3 destination=new Vector3(center.x,center.y,-10);
                ViewCamera.transform.position=Vector3.Lerp(ViewCamera.transform.position,destination,1-Mathf.Exp(-5*dt));
                if(shake>0){ViewCamera.transform.position+=(Vector3)(UnityEngine.Random.insideUnitCircle*shake);shake=Mathf.Max(0,shake-dt);}
            }
            auraTime+=dt;if(auraTime>.14f&&(title||State==OathState.Playing))
            {
                auraTime=0;Vector2 p=(Vector2)ViewCamera.transform.position+new Vector2(UnityEngine.Random.Range(-17,17),UnityEngine.Random.Range(-10,10));
                Particle(p,new Color(.67f,.65f,.54f,.3f),UnityEngine.Random.Range(.025f,.06f),3,Vector2.up*.16f);
            }
        }
        void Burst(Vector2 pos,Color color,int count){for(int i=0;i<count;i++)Particle(pos,color,UnityEngine.Random.Range(.04f,.13f),UnityEngine.Random.Range(.25f,.65f),UnityEngine.Random.insideUnitCircle*3);}
        void Particle(Vector2 pos,Color color,float size,float life,Vector2 velocity,string sprite="circle")
        {
            if(flecks.Count>=320)return;
            var f=new Fleck{velocity=velocity,life=life,maxLife=life,view=OathArt.Node(sprite,pos,Vector2.one*size,color,30,actors)};flecks.Add(f);
        }
        void TickParticles(float dt)
        {
            for(int i=flecks.Count-1;i>=0;i--){var f=flecks[i];f.life-=dt;f.view.transform.position+=(Vector3)f.velocity*dt;var c=f.view.color;c.a=Mathf.Min(c.a,f.life/f.maxLife);f.view.color=c;if(f.life<=0){Destroy(f.view.gameObject);flecks.RemoveAt(i);}}
        }
        void SetupAudio()
        {
            audioFx=gameObject.AddComponent<AudioSource>();ambient=gameObject.AddComponent<AudioSource>();
            swordSound=Tone("steel",.15f,620,0);pickupSound=Tone("pact",.6f,440,1);hurtSound=Tone("wound",.2f,85,0);dashSound=Tone("dash",.2f,210,0);
            ambient.clip=Tone("sanctum",8,55,2);ambient.loop=true;ambient.volume=.12f;ambient.Play();Muted=PlayerPrefs.GetInt("AshenOath.Muted",0)==1;AudioListener.volume=Muted?0:1;
        }
        static AudioClip Tone(string name,float duration,float frequency,int type)
        {
            int rate=22050;var data=new float[(int)(rate*duration)];var random=new System.Random(33);
            for(int i=0;i<data.Length;i++)
            {
                float t=(float)i/rate,u=t/duration;
                float wave=type==2?(Mathf.Sin(t*frequency*Mathf.PI*2)+.4f*Mathf.Sin(t*frequency*1.5f*Mathf.PI*2)+.2f*Mathf.Sin(t*frequency*2.375f*Mathf.PI*2))*.3f:
                    type==1?Mathf.Sin(t*frequency*(1+Mathf.Floor(u*3)*.25f)*Mathf.PI*2)*.35f:
                    ((float)random.NextDouble()*2-1)*.2f+Mathf.Sin(t*frequency*(1-u*.7f)*Mathf.PI*2)*.3f;
                data[i]=wave*(type==2?1:Mathf.Sin(Mathf.Min(1,u*20)*Mathf.PI/2)*(1-u));
            }
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        void Play(AudioClip clip,float volume){if(audioFx&&clip)audioFx.PlayOneShot(clip,volume);}
        public void ToggleSound(){Muted=!Muted;AudioListener.volume=Muted?0:1;PlayerPrefs.SetInt("AshenOath.Muted",Muted?1:0);}
        public Vector2? UnclaimedSpirit()
        {
            float best=float.MaxValue;Vector2? pos=null;foreach(var p in pickups)if(p.type<4&&Spirits[p.type].rank==0&&(p.pos-Position).sqrMagnitude<best){best=(p.pos-Position).sqrMagnitude;pos=p.pos;}return pos;
        }
    }
}
