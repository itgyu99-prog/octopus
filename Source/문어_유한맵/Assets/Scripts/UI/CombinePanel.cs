using UnityEngine;
using UnityEngine.UI;

namespace OctopusSurvivors
{
    public sealed partial class GameHud
    {
        GameObject combineRoot;
        Text combineTitle;
        readonly Button[] recipeButtons = new Button[6];
        readonly Button[] quickCombineButtons = new Button[6];
        readonly Image[] quickCombineIcons = new Image[6];
        readonly ComboId[] quickCombineRecipes = new ComboId[6];
        GameObject quickCombineRoot;
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
            quickCombineRoot = new GameObject("Left circular recipe icons", typeof(RectTransform));
            quickCombineRoot.transform.SetParent(_root.transform, false);
            var root = quickCombineRoot.GetComponent<RectTransform>();
            root.anchorMin = root.anchorMax = new Vector2(0, .5f);
            root.pivot = new Vector2(0, .5f); root.anchoredPosition = new Vector2(28, 0);
            root.sizeDelta = new Vector2(82, 540);
            for (int i = 0; i < quickCombineButtons.Length; i++)
            {
                int index = i;
                var button = UiUtil.Button(root, Vector2.zero, new Vector2(76, 76), "");
                button.name = "Combine recipe " + i;
                var rt = button.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
                var background = button.GetComponent<Image>(); background.sprite = SpriteFactory.Circle(Color.white);
                background.color = new Color(1f, .88f, .9f, .98f);
                button.targetGraphic = background; button.transition = Selectable.Transition.None;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                button.GetComponentInChildren<Text>().gameObject.SetActive(false);
                button.gameObject.AddComponent<CircularButtonHitArea>();
                var icon = CombatImage(button.transform, "Recipe artwork", Vector2.zero, new Vector2(54, 54), null);
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(.5f, .5f);
                button.onClick.AddListener(() =>
                {
                    var player = OctopusPlayer.Local;
                    if (player == null) return;
                    for (int pair = 0; pair < 6; pair++)
                        if (player.CanCombine(firstSlots[pair], secondSlots[pair], out var recipe) && recipe == quickCombineRecipes[index])
                        { player.RequestCombine(firstSlots[pair], secondSlots[pair]); return; }
                });
                quickCombineButtons[i] = button; quickCombineIcons[i] = icon;
                button.gameObject.SetActive(false);
            }
        }

        void UpdateQuickCombine(OctopusPlayer player, GameSession session)
        {
            bool visible = session != null && session.CombatActive && !player.Downed.Value && !ExitConfirmationOpen;
            int count = 0;
            for (int pair = 0; pair < 6 && visible; pair++)
            {
                if (!player.CanCombine(firstSlots[pair], secondSlots[pair], out var recipe)) continue;
                bool duplicate = false;
                for (int i = 0; i < count; i++) if (quickCombineRecipes[i] == recipe) duplicate = true;
                if (!duplicate) quickCombineRecipes[count++] = recipe;
            }
            for (int i = 0; i < 6; i++)
            {
                quickCombineButtons[i].gameObject.SetActive(i < count);
                if (i >= count) continue;
                quickCombineIcons[i].sprite = CombatHudArt.Combo(quickCombineRecipes[i]);
                quickCombineIcons[i].color = Color.white;
                quickCombineButtons[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(0, ((count - 1) * .5f - i) * 88);
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
