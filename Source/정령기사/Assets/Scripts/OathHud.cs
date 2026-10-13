using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AshenOath
{
    public sealed class OathHud : MonoBehaviour
    {
        OathGame game;
        Canvas canvas;
        RectTransform root, screen, hud, toast;
        Font font;
        Text healthText, waveText, timeText, killsText, toastHeading, toastBody, objective, levelText, dashText, oathText, soundText;
        Image healthFill, xpFill, oathFill, dashFill, bossFill;
        RectTransform bossPanel;
        Text bossText;
        readonly Text[] spiritName=new Text[4], spiritStatus=new Text[4];
        readonly Image[] spiritIcon=new Image[4], spiritCharge=new Image[4];
        float toastLeft;
        static readonly Color White=OathArt.Hex("E8E4DA"), Muted=OathArt.Hex("9498A2"), Border=OathArt.Hex("484741"), Panel=OathArt.Hex("121720");
        public void Initialize(OathGame owner)
        {
            game=owner;
            font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","맑은 고딕","Segoe UI","Arial"},24);
            if(!font)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go=new GameObject("Oath Interface",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            root=go.GetComponent<RectTransform>();
            var eventGo=new GameObject("Oath Input",typeof(EventSystem),typeof(StandaloneInputModule));
            var vignette=Image(root,"Vignette",Vector2.zero,new Vector2(1600,900),Color.white);
            Stretch(vignette.rectTransform);vignette.sprite=OathArt.Sprite("vignette");vignette.raycastTarget=false;
            hud=Group(root,"In-game HUD");Stretch(hud);BuildHud();
            screen=Group(root,"Screen");Stretch(screen);
            toast=Group(root,"Pact notification");Anchor(toast,new Vector2(.5f,1),new Vector2(0,-135),new Vector2(720,83));
            var toastBg=Image(toast,"Toast background",Vector2.zero,new Vector2(720,83),new Color(.035f,.045f,.065f,.9f));
            Edge(toast,new Vector2(0,41),new Vector2(220,1),OathArt.Gold);
            toastHeading=Label(toast,"",new Vector2(0,12),new Vector2(680,30),22,OathArt.Gold,TextAnchor.MiddleCenter);
            toastBody=Label(toast,"",new Vector2(0,-18),new Vector2(680,28),16,White,TextAnchor.MiddleCenter);
            toast.gameObject.SetActive(false);
        }
        RectTransform Group(Transform parent,string name)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return go.GetComponent<RectTransform>();
        }
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Anchor(RectTransform r,Vector2 anchor,Vector2 pos,Vector2 size){r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;}
        Image Image(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);
            var img=go.GetComponent<Image>();Anchor(img.rectTransform,new Vector2(.5f,.5f),pos,size);img.color=color;img.raycastTarget=false;return img;
        }
        Text Label(Transform parent,string caption,Vector2 pos,Vector2 size,int fontSize,Color color,TextAnchor alignment=TextAnchor.MiddleLeft)
        {
            var go=new GameObject("Text",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(parent,false);
            var text=go.GetComponent<Text>();Anchor(text.rectTransform,new Vector2(.5f,.5f),pos,size);text.font=font;text.text=caption;text.fontSize=fontSize;
            text.color=color;text.alignment=alignment;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            return text;
        }
        void Edge(Transform p,Vector2 pos,Vector2 size,Color color){Image(p,"Fine rule",pos,size,color);}
        void Frame(Transform p,Vector2 size,Color c)
        {
            Edge(p,new Vector2(0,size.y*.5f),new Vector2(size.x,1),c);Edge(p,new Vector2(0,-size.y*.5f),new Vector2(size.x,1),c);
            Edge(p,new Vector2(size.x*.5f,0),new Vector2(1,size.y),c);Edge(p,new Vector2(-size.x*.5f,0),new Vector2(1,size.y),c);
        }
        Button Button(Transform parent,string caption,Vector2 pos,Vector2 size,Action action,bool primary=false)
        {
            var image=Image(parent,caption,pos,size,primary?OathArt.Hex("A88C60"):OathArt.Hex("1B222C"));image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.22f,1.2f,1.1f);colors.pressedColor=new Color(.65f,.65f,.65f);button.colors=colors;
            Frame(image.transform,size,primary?OathArt.Gold:Border);
            Label(image.transform,caption,Vector2.zero,size-Vector2.one*8,18,primary?OathArt.Ink:White,TextAnchor.MiddleCenter);
            button.onClick.AddListener(()=>action());return button;
        }
        Image Bar(Transform parent,Vector2 pos,Vector2 size,Color color)
        {
            var background=Image(parent,"Bar background",pos,size,OathArt.Hex("282B32"));
            var bar=Image(background.transform,"Fill",Vector2.zero,size,color);
            bar.rectTransform.anchorMin=new Vector2(0,.5f);bar.rectTransform.anchorMax=new Vector2(0,.5f);bar.rectTransform.pivot=new Vector2(0,.5f);bar.rectTransform.anchoredPosition=Vector2.zero;
            return bar;
        }
        void Fill(Image image,float amount,float width){image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width*Mathf.Clamp01(amount));}
        void BuildHud()
        {
            var upper=Image(hud,"Upper shade",Vector2.zero,new Vector2(1600,130),new Color(.035f,.047f,.065f,.94f));
            upper.rectTransform.anchorMin=new Vector2(0,1);upper.rectTransform.anchorMax=new Vector2(1,1);upper.rectTransform.pivot=new Vector2(.5f,1);upper.rectTransform.anchoredPosition=Vector2.zero;upper.rectTransform.sizeDelta=new Vector2(0,110);
            var stats=Group(hud,"Knight vitals");Anchor(stats,new Vector2(0,1),new Vector2(235,-54),new Vector2(400,90));
            var badge=Image(stats,"Knight insignia",new Vector2(-168,0),new Vector2(42,42),OathArt.Gold);badge.sprite=OathArt.Sprite("diamond");
            Label(stats,"I",new Vector2(-168,0),new Vector2(42,42),23,OathArt.Ink,TextAnchor.MiddleCenter);
            Label(stats,"서약의 기사",new Vector2(15,24),new Vector2(302,26),18,White);
            healthText=Label(stats,"100 / 100",new Vector2(114,24),new Vector2(105,24),14,Muted,TextAnchor.MiddleRight);
            healthFill=Bar(stats,new Vector2(16,-2),new Vector2(300,9),OathArt.Hex("B2656E"));
            xpFill=Bar(stats,new Vector2(16,-21),new Vector2(300,3),OathArt.Gold);
            levelText=Label(stats,"",new Vector2(16,-40),new Vector2(300,22),12,Muted);
            var center=Group(hud,"Wave");Anchor(center,new Vector2(.5f,1),new Vector2(0,-50),new Vector2(380,80));
            waveText=Label(center,"",new Vector2(0,18),new Vector2(380,24),14,OathArt.Gold,TextAnchor.MiddleCenter);
            timeText=Label(center,"",new Vector2(0,-12),new Vector2(380,38),28,White,TextAnchor.MiddleCenter);
            var right=Group(hud,"Objective");Anchor(right,new Vector2(1,1),new Vector2(-235,-53),new Vector2(400,86));
            Label(right,"THE HOLLOW SANCTUM",new Vector2(0,25),new Vector2(400,22),13,OathArt.Gold,TextAnchor.MiddleRight);
            objective=Label(right,"",new Vector2(0,-1),new Vector2(400,24),16,White,TextAnchor.MiddleRight);
            killsText=Label(right,"",new Vector2(0,-28),new Vector2(400,22),13,Muted,TextAnchor.MiddleRight);
            var lower=Image(hud,"Lower shade",Vector2.zero,new Vector2(1600,140),new Color(.035f,.047f,.065f,.94f));
            lower.rectTransform.anchorMin=new Vector2(0,0);lower.rectTransform.anchorMax=new Vector2(1,0);lower.rectTransform.pivot=new Vector2(.5f,0);lower.rectTransform.anchoredPosition=Vector2.zero;lower.rectTransform.sizeDelta=new Vector2(0,123);
            var spirits=Group(hud,"Spirit contracts");Anchor(spirits,new Vector2(.5f,0),new Vector2(0,62),new Vector2(728,96));
            for(int i=0;i<4;i++)
            {
                var card=Image(spirits,"Contract "+i,new Vector2(-273+i*182,0),new Vector2(172,87),Panel);Frame(card.transform,new Vector2(172,87),Border);
                spiritIcon[i]=Image(card.transform,"Rune",new Vector2(-54,3),new Vector2(28,37),OathArt.Elements[i]);spiritIcon[i].sprite=OathArt.Sprite("rune"+i);
                spiritName[i]=Label(card.transform,OathArt.Names[i],new Vector2(23,16),new Vector2(100,26),18,White);
                spiritStatus[i]=Label(card.transform,"미계약",new Vector2(23,-11),new Vector2(100,24),12,Muted);
                spiritCharge[i]=Bar(card.transform,new Vector2(0,-36),new Vector2(152,2),OathArt.Elements[i]);
            }
            var dash=Group(hud,"Dash");Anchor(dash,new Vector2(0,0),new Vector2(165,61),new Vector2(260,80));
            Label(dash,"SPACE  /  우클릭",new Vector2(0,21),new Vector2(260,24),13,OathArt.Gold);
            dashText=Label(dash,"그림자 걸음",new Vector2(0,-4),new Vector2(260,26),18,White);
            dashFill=Bar(dash,new Vector2(-30,-30),new Vector2(200,3),OathArt.Hex("83B2BB"));
            var oath=Group(hud,"Oath");Anchor(oath,new Vector2(1,0),new Vector2(-160,61),new Vector2(250,80));
            Label(oath,"E  /  서약 해방",new Vector2(0,21),new Vector2(250,24),13,OathArt.Gold);
            oathText=Label(oath,"",new Vector2(0,-4),new Vector2(250,26),18,White);
            oathFill=Bar(oath,new Vector2(-15,-30),new Vector2(220,3),OathArt.Gold);
            bossPanel=Group(hud,"Forgotten king");Anchor(bossPanel,new Vector2(.5f,1),new Vector2(0,-147),new Vector2(640,58));
            bossText=Label(bossPanel,"망 각 의 왕",new Vector2(0,14),new Vector2(640,26),18,White,TextAnchor.MiddleCenter);
            bossFill=Bar(bossPanel,new Vector2(0,-10),new Vector2(600,5),OathArt.Hex("B2656E"));bossPanel.gameObject.SetActive(false);
            var controls=Label(hud,"WASD 이동  ·  좌클릭 / J 검격  ·  ESC 일시정지  ·  M 음향",Vector2.zero,new Vector2(620,24),12,Muted,TextAnchor.MiddleCenter);
            Anchor(controls.rectTransform,new Vector2(.5f,0),new Vector2(0,137),new Vector2(620,24));
        }
        void ClearScreen()
        {
            for(int i=screen.childCount-1;i>=0;i--){var child=screen.GetChild(i);child.gameObject.SetActive(false);Destroy(child.gameObject);}
            toast.gameObject.SetActive(false);toastLeft=0;
        }
        void Shade(float alpha=.9f)
        {
            var image=Image(screen,"Curtain",Vector2.zero,new Vector2(1600,900),new Color(.035f,.047f,.067f,alpha));Stretch(image.rectTransform);image.raycastTarget=true;
        }
        public void ShowTitle()
        {
            ClearScreen();hud.gameObject.SetActive(false);
            var wash=Image(screen,"Title wash",Vector2.zero,new Vector2(1600,900),new Color(.025f,.035f,.052f,.28f));Stretch(wash.rectTransform);
            var panel=Group(screen,"Title typography");Anchor(panel,new Vector2(.5f,.5f),new Vector2(-350,0),new Vector2(620,750));
            Edge(panel,new Vector2(-200,300),new Vector2(90,2),OathArt.Gold);
            Label(panel,"A   S P I R I T B O U N D   C H R O N I C L E",new Vector2(0,257),new Vector2(490,30),13,OathArt.Gold);
            Label(panel,"ASHEN\nOATH",new Vector2(0,144),new Vector2(510,210),78,White).lineSpacing=.91f;
            Label(panel,"잿 빛  서 약",new Vector2(0,4),new Vector2(490,45),29,OathArt.Gold);
            Label(panel,"왕은 사라졌다. 서약은 남았다.",new Vector2(0,-61),new Vector2(490,28),19,White);
            Label(panel,"버려진 성소에서 네 정령과 계약하고,\n잿빛 왕관 아래 잠든 어둠을 깨우세요.",new Vector2(0,-113),new Vector2(490,64),17,Muted);
            Button(panel,"서약을 맺는다     →",new Vector2(-67,-205),new Vector2(350,58),game.StartRun,true);
            Button(panel,"기사의 기록",new Vector2(-156,-273),new Vector2(173,42),ShowGuide);
            Button(panel,"종료",new Vector2(28,-273),new Vector2(155,42),()=>Application.Quit());
            Label(panel,"ENTER 시작    ·    최고 기록  "+game.Best+" 처치",new Vector2(0,-331),new Vector2(490,25),12,Muted);
            var seal=Label(screen,"I\n\nTHE LAST KNIGHT\n네 개의 정령, 하나의 서약",Vector2.zero,new Vector2(360,100),15,OathArt.Gold,TextAnchor.MiddleCenter);
            Anchor(seal.rectTransform,new Vector2(.5f,.5f),new Vector2(470,-305),new Vector2(360,120));
            var foot=Label(screen,"ASHEN OATH     /     CHAPTER I : THE HOLLOW SANCTUM",Vector2.zero,new Vector2(800,25),11,Muted);
            Anchor(foot.rectTransform,new Vector2(0,0),new Vector2(440,29),new Vector2(800,25));
            soundText=Label(screen,game.Muted?"M  음향 꺼짐":"M  음향 켜짐",Vector2.zero,new Vector2(170,25),12,Muted,TextAnchor.MiddleRight);
            Anchor(soundText.rectTransform,new Vector2(1,0),new Vector2(-120,29),new Vector2(170,25));
        }
        public void ShowPlay(){ClearScreen();hud.gameObject.SetActive(true);}
        public void ShowGuide()
        {
            ClearScreen();Shade(.96f);
            Label(screen,"기사의 기록",new Vector2(0,305),new Vector2(900,60),38,White,TextAnchor.MiddleCenter);
            Label(screen,"룬에 다가가면 정령이 곁에 머뭅니다. 정령은 적을 발견하면 스스로 공격합니다.",new Vector2(0,246),new Vector2(1200,35),18,Muted,TextAnchor.MiddleCenter);
            string[] descriptions={"적이 선 땅에 불꽃 문양을 새깁니다.\n설치된 화염이 지속 피해를 줍니다.","서리 파편 세 개를 날립니다.\n맞은 적의 이동을 늦춥니다.","가까운 적 여럿을 가시로 묶습니다.\n동시에 여러 적을 제어합니다.","보랏빛 번개가 적 사이를 잇습니다.\n밀집한 적에게 연쇄 피해를 줍니다."};
            for(int i=0;i<4;i++)
            {
                var card=Image(screen,"Spirit lore",new Vector2(-480+i*320,66),new Vector2(295,272),Panel);Frame(card.transform,new Vector2(295,272),Border);
                var icon=Image(card.transform,"Rune",new Vector2(0,81),new Vector2(44,54),OathArt.Elements[i]);icon.sprite=OathArt.Sprite("rune"+i);
                Label(card.transform,OathArt.Names[i]+" / "+OathArt.Titles[i],new Vector2(0,23),new Vector2(280,30),20,OathArt.Elements[i],TextAnchor.MiddleCenter);
                Label(card.transform,descriptions[i],new Vector2(0,-49),new Vector2(260,83),16,White,TextAnchor.MiddleCenter);
            }
            Label(screen,"WASD / 방향키 이동     ·     좌클릭 / J 검격     ·     SPACE / 우클릭 회피\nE 서약 해방 (20회 처치로 충전)     ·     ESC 일시정지     ·     M 음향\n\n세 파동을 버티고 2분 15초에 나타나는 망각의 왕을 쓰러뜨리세요.",new Vector2(0,-169),new Vector2(1200,130),18,Muted,TextAnchor.MiddleCenter);
            Button(screen,"돌아가기",new Vector2(0,-325),new Vector2(280,54),ShowTitle,true);
        }
        public void ShowPause()
        {
            ClearScreen();Shade(.83f);
            Label(screen,"서약은 기다린다",new Vector2(0,143),new Vector2(850,65),42,White,TextAnchor.MiddleCenter);
            Label(screen,"잠시 숨을 고르세요.",new Vector2(0,83),new Vector2(850,35),18,Muted,TextAnchor.MiddleCenter);
            Button(screen,"여정을 계속한다",new Vector2(0,0),new Vector2(340,57),game.Resume,true);
            Button(screen,"처음부터 다시",new Vector2(0,-73),new Vector2(340,48),game.StartRun);
            Button(screen,"성소를 떠난다",new Vector2(0,-138),new Vector2(340,48),game.ReturnToTitle);
        }
        public void ShowBlessing()
        {
            ClearScreen();Shade(.91f);
            Label(screen,"B L E S S I N G   O F   T H E   O A T H",new Vector2(0,263),new Vector2(1000,28),14,OathArt.Gold,TextAnchor.MiddleCenter);
            Label(screen,"서약이 깊어집니다",new Vector2(0,204),new Vector2(1000,70),40,White,TextAnchor.MiddleCenter);
            Label(screen,"당신에게 남겨질 축복을 고르세요.   /   기사 레벨 "+game.Level,new Vector2(0,147),new Vector2(1000,30),17,Muted,TextAnchor.MiddleCenter);
            string[] names={"벼려진 의지","불굴의 심장","깊어지는 계약"};
            string[] copy={"검과 모든 정령의 공격력\n+18%","최대 생명력 +20\n생명력 40 회복","정령 공격 간격 −0.2초\n서약 해방 25% 충전"};
            for(int i=0;i<3;i++)
            {
                int choice=i;var card=Image(screen,"Blessing "+i,new Vector2(-365+i*365,-53),new Vector2(330,310),Panel);Frame(card.transform,new Vector2(330,310),Border);
                var icon=Image(card.transform,"Blessing icon",new Vector2(0,91),new Vector2(43,54),OathArt.Gold);icon.sprite=OathArt.Sprite("rune"+(i==0?3:i));
                Label(card.transform,names[i],new Vector2(0,29),new Vector2(310,35),24,White,TextAnchor.MiddleCenter);
                Label(card.transform,copy[i],new Vector2(0,-32),new Vector2(300,68),18,Muted,TextAnchor.MiddleCenter);
                Button(card.transform,(i+1)+"   축복 선택",new Vector2(0,-112),new Vector2(274,46),()=>game.Bless(choice),true);
            }
            Label(screen,"1 · 2 · 3 키로도 선택할 수 있습니다",new Vector2(0,-266),new Vector2(1000,28),14,Muted,TextAnchor.MiddleCenter);
        }
        public void ShowEnd(bool won)
        {
            ClearScreen();Shade(.9f);
            Label(screen,won?"O A T H   F U L F I L L E D":"A S H E S   R E M A I N",new Vector2(0,205),new Vector2(1000,35),16,OathArt.Gold,TextAnchor.MiddleCenter);
            Label(screen,won?"서약은 꺼지지 않는다":"쓰러져도, 서약은 남는다",new Vector2(0,128),new Vector2(1200,80),44,White,TextAnchor.MiddleCenter);
            Label(screen,won?"망각의 왕이 무너졌습니다. 성소에 다시 빛이 깃듭니다.":"정령들은 당신의 다음 여정을 기다립니다.",new Vector2(0,54),new Vector2(1000,38),19,Muted,TextAnchor.MiddleCenter);
            Label(screen,"처치  "+game.Kills+"     /     기사 레벨  "+game.Level+"     /     생존  "+Clock(game.Elapsed),new Vector2(0,-14),new Vector2(1000,37),20,OathArt.Gold,TextAnchor.MiddleCenter);
            Button(screen,"새로운 서약",new Vector2(0,-107),new Vector2(340,57),game.StartRun,true);
            Button(screen,"처음 화면으로",new Vector2(0,-180),new Vector2(340,48),game.ReturnToTitle);
        }
        public void Toast(string heading,string body)
        {
            toastHeading.text=heading;toastBody.text=body;toastLeft=3.8f;toast.gameObject.SetActive(true);
        }
        static string Clock(float seconds)=>$"{(int)seconds/60:00}:{(int)seconds%60:00}";
        public void Tick(float dt)
        {
            if(soundText)soundText.text=game.Muted?"M  음향 꺼짐":"M  음향 켜짐";
            if(game.State==OathState.Blessing)
            {
                if(Input.GetKeyDown(KeyCode.Alpha1))game.Bless(0);else if(Input.GetKeyDown(KeyCode.Alpha2))game.Bless(1);else if(Input.GetKeyDown(KeyCode.Alpha3))game.Bless(2);
            }
            if(toastLeft>0){toastLeft-=dt;if(toastLeft<=0)toast.gameObject.SetActive(false);}
            if(!hud.gameObject.activeSelf)return;
            Fill(healthFill,game.Health/game.MaxHealth,300);Fill(xpFill,(float)game.Experience/game.ExperienceNeeded,300);
            healthText.text=$"{Mathf.CeilToInt(game.Health)} / {game.MaxHealth:0}";levelText.text=$"LV {game.Level:00}  ·  서약의 잔재  {game.Experience} / {game.ExperienceNeeded}";
            waveText.text=game.Wave==4?"THE FORGOTTEN KING":$"WAVE  {game.Wave:00}  /  03";timeText.text=Clock(game.Elapsed);
            int owned=0;for(int i=0;i<4;i++)if(game.Spirits[i].rank>0)owned++;
            var rune=game.UnclaimedSpirit();
            if(rune.HasValue)
            {
                Vector2 delta=rune.Value-game.Position;string direction=Mathf.Abs(delta.x)>Mathf.Abs(delta.y)?(delta.x>0?"오른쪽":"왼쪽"):(delta.y>0?"위쪽":"아래쪽");
                objective.text=$"미계약 룬  ·  {direction} {delta.magnitude:0}m";
            }
            else objective.text=game.Wave==4?"망각의 왕을 쓰러뜨리세요":"네 정령과 함께 성소를 지키세요";
            killsText.text=$"계약 {owned} / 4    ·    처치 {game.Kills}";
            Fill(dashFill,1-game.DashCooldown/1.6f,200);dashText.text=game.DashCooldown<=0?"그림자 걸음  ·  준비":$"그림자 걸음  ·  {game.DashCooldown:0.0}s";
            Fill(oathFill,game.OathCharge/100,220);oathText.text=game.OathCharge>=100?"준비 완료  ·  E":$"공명 {game.OathCharge:0}%";
            for(int i=0;i<4;i++)
            {
                var s=game.Spirits[i];spiritIcon[i].color=s.rank>0?OathArt.Elements[i]:new Color(.3f,.33f,.37f);
                spiritName[i].color=s.rank>0?White:Muted;spiritStatus[i].text=s.rank==0?"룬을 찾아 계약":$"계약 {s.rank}  ·  "+(s.cooldown<=0?"대기":$"{s.cooldown:0.0}s");
                Fill(spiritCharge[i],s.rank==0?0:1-s.cooldown/game.SpiritInterval,152);
            }
            bool boss=game.Boss!=null&&game.Boss.hp>0;bossPanel.gameObject.SetActive(boss);if(boss)Fill(bossFill,game.Boss.hp/game.Boss.maxHp,600);
        }
    }
}
