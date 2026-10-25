using UnityEngine;
using UnityEngine.UI;

namespace OctopusSurvivors
{
    public sealed partial class GameHud
    {
        const float HudWidth = 1000f, ArtWidth = 1855f, ArtHeight = 365f;
        readonly Image[] combatAttributes = new Image[4], combatElements = new Image[3];
        readonly Image[] combatDash = new Image[2];
        Image combatSpecial, combatHp;
        RectTransform combatRoot;

        static Image CombatImage(Transform parent, string name, Vector2 position, Vector2 size, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, 0);
            rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = position; rt.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.sprite = sprite;
            image.color = Color.white; image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }

        // Coordinates on the supplied 2022 x 778 HUD artwork, cropped to its silhouette.
        static Vector2 HudPoint(float x, float y) => new Vector2((x - 94 - ArtWidth * .5f) * HudWidth / ArtWidth,
            (228 + ArtHeight - y) * HudWidth / ArtWidth);
        static Vector2 HudSize(float w, float h) => new Vector2(w, h) * (HudWidth / ArtWidth);

        static Image CombatSocket(Transform parent, string name, float x, float y, float diameter)
        {
            var mask = CombatImage(parent, name, HudPoint(x, y), HudSize(diameter, diameter), SpriteFactory.Circle(Color.white));
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var icon = CombatImage(mask.transform, "Full color icon", Vector2.zero, mask.rectTransform.sizeDelta, null);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(.5f, .5f);
            return icon;
        }

        Image CombatGauge(string name, float x, float y, float width, float height, Color color)
        {
            var track = CombatImage(combatRoot, name + " track", HudPoint(x, y), HudSize(width, height), SpriteFactory.Square(Color.white));
            track.preserveAspect = false; track.color = new Color(.075f, .085f, .12f);
            var fill = CombatImage(combatRoot, name, HudPoint(x, y), HudSize(width, height), SpriteFactory.Square(Color.white));
            fill.preserveAspect = false; fill.color = color;
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
            return fill;
        }

        void BuildCombatHud()
        {
            var go = new GameObject("Bottom center octopus HUD", typeof(RectTransform));
            go.transform.SetParent(_root.transform, false); combatRoot = go.GetComponent<RectTransform>();
            combatRoot.anchorMin = combatRoot.anchorMax = new Vector2(.5f, 0);
            combatRoot.pivot = new Vector2(.5f, 0); combatRoot.anchoredPosition = new Vector2(0, 14);
            combatRoot.sizeDelta = new Vector2(HudWidth, ArtHeight * HudWidth / ArtWidth);
            CombatImage(combatRoot, "Supplied pink tentacle artwork", new Vector2(0, combatRoot.sizeDelta.y * .5f),
                combatRoot.sizeDelta, CombatHudArt.Frame);
            float[] attributes = { 370, 531, 693, 855 };
            for (int i = 0; i < 4; i++) combatAttributes[i] = CombatSocket(combatRoot, "Attribute " + i, attributes[i], 334, 100);
            float[] elements = { 1080, 1248, 1418 };
            for (int i = 0; i < 3; i++) combatElements[i] = CombatSocket(combatRoot, "Element " + i, elements[i], 465, 108);
            combatSpecial = CombatSocket(combatRoot, "Cosmos or YinYang", 1723, 413, 210);
            combatHp = CombatGauge("Health", 633, 458, 420, 32, new Color(1f, .22f, .4f));
            _hpFill = combatHp;
            // One background covers the baked cyan fill, then each charge fills separately.
            var dashTrack = CombatImage(combatRoot, "Dash track", HudPoint(520, 521), HudSize(190, 28), SpriteFactory.Square(Color.white));
            dashTrack.preserveAspect = false; dashTrack.color = new Color(.075f, .085f, .12f);
            for (int i = 0; i < 2; i++)
            {
                combatDash[i] = CombatImage(combatRoot, "Dash charge " + (i + 1), HudPoint(471 + i * 98, 521), HudSize(92, 28), SpriteFactory.Square(Color.white));
                combatDash[i].preserveAspect = false; combatDash[i].color = new Color(.14f, .81f, 1f);
                combatDash[i].type = Image.Type.Filled; combatDash[i].fillMethod = Image.FillMethod.Horizontal;
            }
            _dashFill = combatDash[0];
            _hpText = UiUtil.Label(combatRoot, Vector2.zero, HudSize(420, 26), 11, Color.white, TextAnchor.MiddleCenter);
            _hpText.rectTransform.anchorMin = _hpText.rectTransform.anchorMax = new Vector2(.5f, 0);
            _hpText.rectTransform.pivot = new Vector2(.5f, .5f); _hpText.rectTransform.anchoredPosition = HudPoint(633, 458);
            _dashText = UiUtil.Label(combatRoot, Vector2.zero, new Vector2(80, 18), 11, Color.white, TextAnchor.MiddleLeft);
            _dashText.rectTransform.anchorMin = _dashText.rectTransform.anchorMax = new Vector2(.5f, 0);
            _dashText.rectTransform.pivot = new Vector2(0, .5f); _dashText.rectTransform.anchoredPosition = HudPoint(630, 521);
            _slot0Icon = combatAttributes[0]; _slot1Icon = combatAttributes[1];
            _slot2Icon = combatAttributes[2]; _slot3Icon = combatAttributes[3]; _specialIcon = combatSpecial;
            _loadout = UiUtil.Label(combatRoot, Vector2.zero, Vector2.one, 1, Color.clear); _loadout.gameObject.SetActive(false);
        }

        public static float DashSegmentFill(int segment, int charges, int capacity, float rechargeLeft)
        {
            if (segment >= capacity) return 0;
            if (segment < charges) return 1;
            return segment == charges ? Mathf.Clamp01(1 - rechargeLeft / GameConfig.DashRecharge) : 0;
        }

        void UpdateCombatHud(OctopusPlayer p)
        {
            float canvasWidth = _root.GetComponent<RectTransform>().rect.width;
            combatRoot.localScale = Vector3.one * Mathf.Min(1f, (canvasWidth - 48) / HudWidth);
            for (byte i = 0; i < 4; i++) SetCombatIcon(combatAttributes[i], RuntimeArt.ElementIcon(p.SlotElement(i)));
            for (byte i = 0; i < 3; i++) SetCombatIcon(combatElements[i], CombatHudArt.Combo(p.ComboAt(i)));
            SetCombatIcon(combatSpecial, RuntimeArt.SpecialIcon((SpecialId)p.Special.Value));
            combatHp.fillAmount = p.Hp.Value / Mathf.Max(1, p.MaxHp.Value);
            _hpText.text = p.Downed.Value ? "다운" : Mathf.CeilToInt(p.Hp.Value) + " / " + Mathf.CeilToInt(p.MaxHp.Value);
            for (int i = 0; i < 2; i++)
            {
                bool two = p.MaxDashCharges > 1;
                combatDash[i].enabled = two || i == 0;
                combatDash[i].rectTransform.anchoredPosition = HudPoint(two ? 471 + i * 98 : 520, 521);
                combatDash[i].rectTransform.sizeDelta = HudSize(two ? 92 : 190, 28);
                combatDash[i].fillAmount = DashSegmentFill(i, p.DashCharges.Value, p.MaxDashCharges, p.DashRechargeLeft.Value);
            }
            _dashText.text = p.DashCharges.Value + " / " + p.MaxDashCharges;
            _keys.gameObject.SetActive(canvasWidth >= 1600);
            _keys.rectTransform.sizeDelta = new Vector2(310, 62); _keys.fontSize = 15;
            _keys.text = "이동 방향키 · 획득 E · 합성 F\n변신 Space · 대시 Shift · 나가기 Esc";
        }

        static void SetCombatIcon(Image image, Sprite sprite)
        {
            image.sprite = sprite; image.enabled = sprite != null; image.color = Color.white;
        }
    }
}
