using UnityEngine;
using UnityEngine.UI;

namespace OctopusSurvivors
{
    public sealed partial class GameHud
    {
        sealed class CombatSlot
        {
            public Image Dark, Ready;
            public Text State;
            public void Set(Sprite icon,float ready,bool usable=true)
            {
                Dark.sprite=Ready.sprite=icon;
                Dark.enabled=Ready.enabled=icon!=null;
                Ready.fillAmount=usable ? Mathf.Clamp01(ready) : 0;
                State.text=icon==null ? "" : !usable ? "대기" : ready>=.999f ? "" : Mathf.RoundToInt(ready*100)+"%";
            }
        }
        readonly CombatSlot[] combatAttributes=new CombatSlot[4],combatElements=new CombatSlot[3];
        CombatSlot combatSpecial;
        Image combatHp;
        RectTransform combatRoot;
        static Image CombatImage(Transform parent,string name,Vector2 position,Vector2 size,Sprite sprite)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            go.transform.SetParent(parent,false);
            var rt=go.GetComponent<RectTransform>(); rt.anchorMin=rt.anchorMax=new Vector2(.5f,0);
            rt.pivot=new Vector2(.5f,.5f); rt.anchoredPosition=position; rt.sizeDelta=size;
            var image=go.GetComponent<Image>(); image.sprite=sprite; image.preserveAspect=true; image.raycastTarget=false;
            return image;
        }
        static CombatSlot CreateCombatSlot(Transform parent,string name,Vector2 position,float size)
        {
            var mask=CombatImage(parent,name,position,Vector2.one*size,SpriteFactory.Circle(Color.white));
            mask.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var dark=CombatImage(mask.transform,"Unavailable",new Vector2(0,size*.5f),Vector2.one*size,null);
            dark.color=new Color(.22f,.22f,.27f,1);
            var ready=CombatImage(mask.transform,"Ready from bottom",new Vector2(0,size*.5f),Vector2.one*size,null);
            ready.type=Image.Type.Filled; ready.fillMethod=Image.FillMethod.Vertical; ready.fillOrigin=(int)Image.OriginVertical.Bottom;
            var text=UiUtil.Label(parent,Vector2.zero,new Vector2(size+16,20),12,Color.white,TextAnchor.MiddleCenter);
            text.name=name+" availability";
            text.rectTransform.anchorMin=text.rectTransform.anchorMax=new Vector2(.5f,0);
            text.rectTransform.pivot=new Vector2(.5f,.5f); text.rectTransform.anchoredPosition=position+Vector2.down*(size*.5f+7);
            text.raycastTarget=false;
            return new CombatSlot { Dark=dark,Ready=ready,State=text };
        }
        void BuildCombatHud()
        {
            var go=new GameObject("Octopus combat interface",typeof(RectTransform)); go.transform.SetParent(_root.transform,false);
            combatRoot=go.GetComponent<RectTransform>(); combatRoot.anchorMin=combatRoot.anchorMax=new Vector2(.5f,0);
            combatRoot.pivot=new Vector2(.5f,0); combatRoot.anchoredPosition=Vector2.zero; combatRoot.sizeDelta=new Vector2(640,221);
            CombatImage(go.transform,"Original octopus frame",new Vector2(0,110.35f),new Vector2(640,220.7f),CombatHudArt.Frame);
            // Pixel measurements refer to the supplied 1678x937 reference, cropped at (35,190).
            for(int i=0;i<4;i++) combatAttributes[i]=CreateCombatSlot(go.transform,"Attribute "+i,
                new Vector2(((i==0?290:i==1?435:i==2?575:715)-35)*640f/1595-320,(740-370)*640f/1595),34);
            for(int i=0;i<3;i++) combatElements[i]=CreateCombatSlot(go.transform,"Element "+i,
                new Vector2(((i==0?890:i==1?1105:1320)-35)*640f/1595-320,(740-615)*640f/1595),49);
            combatSpecial=CreateCombatSlot(go.transform,"Special",new Vector2((1295-35)*640f/1595-320,(740-370)*640f/1595),104);
            var track=CombatImage(go.transform,"Health track",new Vector2((490-35)*640f/1595-320,(740-543)*640f/1595),new Vector2(180,29),SpriteFactory.Square(Color.white));
            track.color=new Color(.09f,.08f,.15f); track.preserveAspect=false;
            combatHp=CombatImage(go.transform,"Health",new Vector2((490-35)*640f/1595-320,(740-543)*640f/1595),new Vector2(176,25),SpriteFactory.Square(Color.white));
            combatHp.preserveAspect=false; combatHp.color=new Color(.91f,.22f,.28f);
            combatHp.type=Image.Type.Filled; combatHp.fillMethod=Image.FillMethod.Horizontal;
            _loadout.gameObject.SetActive(false);
            foreach(var image in new[] {_slot0Icon,_slot1Icon,_slot2Icon,_slot3Icon,_specialIcon}) image.gameObject.SetActive(false);
        }
        void UpdateCombatHud(OctopusPlayer p)
        {
            if(combatRoot==null)return;
            float availableWidth=_root.GetComponent<RectTransform>().rect.width-48;
            float scale=Mathf.Min(1.3f,availableWidth/640);
            combatRoot.localScale=Vector3.one*scale;
            // Keep the controls in the free left corner, outside the enlarged HUD.
            float sideWidth=(_root.GetComponent<RectTransform>().rect.width-640*scale)*.5f-40;
            _keys.gameObject.SetActive(sideWidth>=260);
            _keys.rectTransform.sizeDelta=new Vector2(Mathf.Max(260,sideWidth),60);
            _keys.rectTransform.anchoredPosition=new Vector2(20,10);
            _keys.fontSize=16;
            _keys.text="이동 방향키 · 획득 E · 합성 F\n변신 Space · 능력 1/2/3 · 나가기 Esc";
            bool cycling=!p.Downed.Value && !p.Transformed.Value;
            for(byte i=0;i<4;i++) combatAttributes[i].Set(RuntimeArt.ElementIcon(p.SlotElement(i)),p.AttributeReadyFraction(i),cycling);
            for(byte i=0;i<3;i++) combatElements[i].Set(CombatHudArt.Combo(p.ComboAt(i)),p.ComboReadyFraction(i),cycling);
            float total=GameConfig.SpecialCooldown*GameConfig.ElementCdMul(p.UpElemCd.Value);
            float ready=1f-Mathf.Clamp01(p.SpecialCd.Value/Mathf.Max(.01f,total));
            combatSpecial.Set(RuntimeArt.SpecialIcon((SpecialId)p.Special.Value),ready,!p.Downed.Value);
            combatHp.fillAmount=p.Hp.Value/Mathf.Max(1,p.MaxHp.Value);
        }
    }
}
