using UnityEngine;
using UnityEngine.UI;

namespace OctopusSurvivors
{
    public sealed partial class GameHud
    {
        GameObject combineRoot;
        Text combineTitle;
        readonly Button[] recipeButtons = new Button[6];
        GameObject quickCombineRoot;
        readonly Button[] quickCombineButtons = new Button[6];
        readonly byte[] firstSlots = { 0, 0, 0, 1, 1, 2 };
        readonly byte[] secondSlots = { 1, 2, 3, 2, 3, 3 };
        void BuildCombine()
        {
            combineRoot = UiUtil.Panel(_root.transform, new Vector2(0, 20), new Vector2(720, 610), new Color(.06f,.1f,.16f,.97f)).gameObject;
            var rt = combineRoot.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(.5f,.5f);
            combineTitle = UiUtil.Label(combineRoot.transform,new Vector2(25,-20),new Vector2(670,80),24,Color.white);
            for (int i=0;i<6;i++)
            {
                int pair=i;
                var button=UiUtil.Button(combineRoot.transform,new Vector2(0,170-i*66),new Vector2(650,56),"");
                var navigation=button.navigation;navigation.mode=Navigation.Mode.None;button.navigation=navigation;
                button.onClick.AddListener(()=>
                {
                    var player=OctopusPlayer.Local;
                    if(player!=null) player.RequestCombine(firstSlots[pair],secondSlots[pair]);
                });
                recipeButtons[i]=button;
            }
            var close=UiUtil.Button(combineRoot.transform,new Vector2(0,-265),new Vector2(220,45),"닫기 (F)");
            close.onClick.AddListener(()=>combineRoot.SetActive(false));
            combineRoot.SetActive(false);
            BuildQuickCombine();
        }
        void BuildQuickCombine()
        {
            quickCombineRoot = new GameObject("QuickCombineButtons", typeof(RectTransform));
            quickCombineRoot.transform.SetParent(_root.transform, false);
            var rt = quickCombineRoot.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(22f, 0f);
            rt.sizeDelta = new Vector2(84f, 500f);
            for (int i = 0; i < 6; i++)
            {
                int pair = i;
                var button = UiUtil.Button(quickCombineRoot.transform, Vector2.zero, new Vector2(72f, 72f), "");
                var brt = button.GetComponent<RectTransform>();
                brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
                brt.pivot = new Vector2(0.5f, 0.5f);
                brt.anchoredPosition = new Vector2(0f, 210f - i * 84f);
                var graphic = button.GetComponent<Image>();
                graphic.sprite = SpriteFactory.Circle(Color.white);
                graphic.type = Image.Type.Simple;
                graphic.color = new Color(0.93f, 0.79f, 0.84f, 0.97f);
                button.onClick.AddListener(() =>
                {
                    var player = OctopusPlayer.Local;
                    if (player != null && player.CanCombine(firstSlots[pair], secondSlots[pair], out _))
                        player.RequestCombine(firstSlots[pair], secondSlots[pair]);
                });
                quickCombineButtons[i] = button;
                button.gameObject.SetActive(false);
            }
        }
        void UpdateQuickCombine(OctopusPlayer player, GameSession session)
        {
            if (quickCombineRoot == null) return;
            bool enabled = session != null && session.CombatActive && !player.Downed.Value && !ExitConfirmationOpen;
            int visible = 0;
            for (int i = 0; i < 6; i++)
            {
                bool available = enabled && player.CanCombine(firstSlots[i], secondSlots[i], out var combo);
                var button = quickCombineButtons[i];
                button.gameObject.SetActive(available);
                if (!available) continue;
                var label = button.GetComponentInChildren<Text>();
                label.text = ElementRules.Name(combo);
                label.fontSize = 14;
                label.alignment = TextAnchor.MiddleCenter;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 10;
                label.resizeTextMaxSize = 15;
                button.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 210f - visible * 84f);
                visible++;
            }
        }
        public void ToggleCombine()
        {
            if (ExitConfirmationOpen) return;
            var session=GameSession.Instance;
            if(combineRoot!=null && session!=null && session.CombatActive) combineRoot.SetActive(!combineRoot.activeSelf);
        }
        bool CloseCombineIfOpen()
        {
            if (combineRoot == null || !combineRoot.activeInHierarchy) return false;
            combineRoot.SetActive(false);
            return true;
        }
        void UpdateCombine(OctopusPlayer player, GameSession session)
        {
            if(combineRoot==null || !combineRoot.activeSelf) return;
            if(session==null || !session.CombatActive || player.Downed.Value) { combineRoot.SetActive(false); return; }
            combineTitle.text=$"요소 합성  {player.ComboCount}/3칸 사용 중\n보유 속성으로 합성 · 재료 속성 2개는 소모됩니다";
            for(int i=0;i<6;i++)
            {
                byte a=firstSlots[i],b=secondSlots[i];
                bool valid=ElementRules.TryCombo(player.SlotElement(a),player.SlotElement(b),out var combo);
                recipeButtons[i].GetComponentInChildren<Text>().text=valid
                    ? $"{ElementRules.Name(player.SlotElement(a))} + {ElementRules.Name(player.SlotElement(b))} → {ElementRules.Name(combo)} (재료 소모)"
                    : $"속성 {a+1} + {b+1} : 서로 다른 속성 2개 필요";
                recipeButtons[i].interactable=player.CanCombine(a,b,out _);
            }
        }
    }
}
