from pathlib import Path
p=Path('Source/문어_유한맵/Assets/Scripts')
def edit(file,old,new):
 f=p/file;s=f.read_text(encoding='utf-8-sig');assert old in s,(file,old[:100]);f.write_text(s.replace(old,new),encoding='utf-8')
def cut(file,start,end,new):
 f=p/file;s=f.read_text(encoding='utf-8-sig');a=s.index(start);b=s.index(end,a);f.write_text(s[:a]+new+s[b:],encoding='utf-8')
edit('World/GameSession.cs','                if (TickElementEffect(a, dt)) continue;','                if (TickPlannedAbility(a, dt) || TickElementEffect(a, dt)) continue;')
edit('Elements/ElementEffects.cs','DamageCircle(a.Pos, a.Size, a.Damage * dt, StatusMask.None, 0f, 0f);\n                    break;\n                case ActorKind.ChiliShot:', 'DamageCircle(a.Pos, a.Size, a.Damage * dt, StatusMask.Slow, .3f, 0f);\n                    break;\n                case ActorKind.ChiliShot:')
edit('Elements/ElementEffects.cs','if (!IsServer || p == null || p.Downed.Value) return;', 'if (!IsServer || p == null || p.Downed.Value) return;\n            if (combo == ComboId.Fog || combo == ComboId.Chili || combo == ComboId.Tree || combo >= ComboId.Soda) { CastPlannedCombo(p, combo); return; }')
edit('Elements/ElementEffectVisual.cs','k == ActorKind.CurrentFan || k == ActorKind.MagnetPair', 'k == ActorKind.ChainArc || k == ActorKind.CurrentFan || k == ActorKind.MagnetPair')
edit('Elements/ElementEffectVisual.cs','k >= ActorKind.SteamField && k <= ActorKind.MagnetBlast;', '(k >= ActorKind.SteamField && k <= ActorKind.MagnetBlast) || (k >= ActorKind.SodaWave && k <= ActorKind.CurrentEmitter);')
edit('Elements/ElementEffectVisual.cs','                case ActorKind.SteamField:', '''                case ActorKind.SodaWave:
                    for (int row = 0; row < 3; row++)
                    {
                        var wave = new Vector2[13];
                        for (int i = 0; i < wave.Length; i++) wave[i] = new Vector2(-.8f + i * .13f, (row - 1) * .38f + Mathf.Sin(i * .7f + t * 9f) * .16f);
                        Curve(wave, .13f, row == 1 ? Color.white : Cyan);
                    }
                    break;
                case ActorKind.LemonShot:
                    Blob(Vector2.zero, .9f, new Color(.95f,.8f,.1f));
                    Ring(Vector2.zero, .65f, new Color(1f,1f,.6f), .1f);
                    for(int i=0;i<6;i++) Curve(new[]{Vector2.zero,Polar(i*Mathf.PI/3f,.62f)},.06f,Color.white);
                    break;
                case ActorKind.FruitTree:
                    Ring(Vector2.zero, 1f, new Color(.3f,.8f,.3f,.2f), .015f);
                    Curve(new[]{new Vector2(0f,-.2f),new Vector2(0f,.45f)},.18f,new Color(.45f,.23f,.1f));
                    Blob(new Vector2(0f,.45f),.4f,Leaf);
                    for(int i=0;i<4;i++) Blob(new Vector2((i-1.5f)*.14f,.4f+Mathf.Sin(i)*.12f),.07f,new Color(1f,.4f,.15f));
                    break;
                case ActorKind.Fruit:
                    Ring(Vector2.zero,1f,new Color(1f,.5f,.2f,.3f),.04f);
                    Blob(new Vector2(0f,Mathf.Max(0f,1f-t/.6f)*3f),.7f,new Color(1f,.4f,.15f));
                    break;
                case ActorKind.ChainArc:
                    Arc(Vector2.left,Vector2.right,new Color(.2f,.5f,1f),t);
                    break;
                case ActorKind.FlameAura:
                    Ring(Vector2.zero,1f,new Color(1f,.4f,.1f,.4f),.04f);
                    for(int i=0;i<12;i++)
                    {
                        Vector2 flame=Polar(i*Mathf.PI/6f,.75f);
                        Blob(flame,.18f,new Color(1f,.2f,.02f,.65f));
                        Curve(new[]{flame,flame+new Vector2(Mathf.Sin(t*9f+i)*.08f,.18f)},.1f,new Color(1f,.8f,.15f,.8f));
                    }
                    break;
                case ActorKind.SteamField:''')
# Starting selection before Ready; shift existing controls to keep all choices visible.
edit('UI/WaitingRoom.cs','new Vector2(860, 560)', 'new Vector2(860, 690)')
edit('UI/WaitingRoom.cs','            _readyButton =', '''            var choose = UiUtil.Label(_root.transform, new Vector2(40,-250), new Vector2(780,36),24,Color.white);
            choose.text = "시작 속성을 선택하세요";
            for (int i=0;i<GameplayRules.BasicElements.Length;i++)
            {
                ElementId element=GameplayRules.BasicElements[i];
                var button=UiUtil.Button(_root.transform,new Vector2((i-1.5f)*180f,30f),new Vector2(165,56),ElementRules.Name(element));
                button.onClick.AddListener(()=>GameSession.Instance?.SelectStartingElement(element));
                DisableKeyboardSubmit(button);
            }
            _readyButton =''')
edit('UI/WaitingRoom.cs','new Vector2(0, -20)', 'new Vector2(0, -50)')
edit('UI/WaitingRoom.cs','new Vector2(0, -100)', 'new Vector2(0, -130)')
edit('UI/WaitingRoom.cs','new Vector2(40, -430)', 'new Vector2(40, -555)')
edit('UI/WaitingRoom.cs','            _readyButton.interactable = true;', '            _readyButton.interactable = (isHost ? session.HostElement.Value : session.GuestElement.Value) != 0;\n            _hostRow.text += "  / " + ElementRules.Name((ElementId)session.HostElement.Value);\n            if(guestHere) _guestRow.text += "  / " + ElementRules.Name((ElementId)session.GuestElement.Value);')
# Remove experience UI and dev controls.
cut('UI/DevMenu.cs','            LabelAt(660f, "레벨', '            _status =', '            LabelAt(660f, "속성3  (누른 사람만)");\n            ElementRow(696f, 2);\n\n')
cut('UI/DevMenu.cs','        static int CurrentLevel()', '        void RequestStage', '')
cut('UI/DevMenu.cs','        void ApplyLevel(', '        void SetStatus', '')
edit('UI/DevMenu.cs','ElementId.Grass, ElementId.Stone, ElementId.Electric','ElementId.Grass, ElementId.Electric')
edit('UI/GameHud.cs','public sealed class GameHud', 'public sealed partial class GameHud')
cut('UI/GameHud.cs','            UiUtil.Bar(_root.transform, new Vector2(40, -100)', '            _loadout =', '')
edit('UI/GameHud.cs','new Vector2(112, -160), new Vector2(688, 210)', 'new Vector2(112, -105), new Vector2(688, 310)')
edit('UI/GameHud.cs','            _slot0Icon = HudIcon', '            _slot2Icon = HudIcon(_root.transform, new Vector2(42f, -209f), 46f);\n            _slot0Icon = HudIcon')
edit('UI/GameHud.cs','new Vector2(42f, -160f), 58f','new Vector2(42f, -105f), 46f')
edit('UI/GameHud.cs','new Vector2(42f, -224f), 58f','new Vector2(42f, -157f), 46f')
edit('UI/GameHud.cs','new Vector2(42f, -288f), 58f','new Vector2(42f, -325f), 46f')
edit('UI/GameHud.cs','        Image _slot1Icon;', '        Image _slot1Icon;\n        Image _slot2Icon;')
edit('UI/GameHud.cs','요소결합 F   변신 Space   레벨업 1 2 3','합성창 F   변신 Space   능력 선택 1 2 3')
edit('UI/GameHud.cs','            BuildCards();', '            BuildCards();\n            BuildCombine();')
cut('UI/GameHud.cs','            int need = GameConfig.XpToNext', '            var session = GameSession.Instance;', '')
cut('UI/GameHud.cs','            string a = SlotText(p, 0);', '            _slot0Icon.sprite', '''            string slots = $"속성1 {SlotText(p,0)}\\n\\n속성2 {SlotText(p,1)}\\n\\n속성3 {SlotText(p,2)}";
            string combos = "";
            for(byte i=0;i<3;i++) combos += (i>0 ? " · " : "") + (p.ComboAt(i)!=ComboId.None ? ElementRules.Name(p.ComboAt(i)) : session!=null && i<session.UnlockedComboSlots.Value ? "빈 칸" : "잠김");
            _loadout.text = $"{slots}\\n\\n요소: {combos}\\n{SpecialText(p)}";
            _slot2Icon.sprite = RuntimeArt.ElementIcon(p.SlotElement(2));
            _slot2Icon.enabled = _slot2Icon.sprite != null;
''')
edit('UI/GameHud.cs','            UpdateCards(p, over);','            UpdateCards(p, over);\n            UpdateCombine(p, session);')
cut('UI/GameHud.cs','            float left = session != null ? session.LevelUpLeft.Value : 0f;', '            for (int i = 0; i < CardCount; i++)\n            {\n                var id = p.Offer(i);', '''            bool choosing = session != null && session.StageFlow.Value == (byte)StageState.Choosing;
            bool countdown = session != null && session.StageFlow.Value == (byte)StageState.Countdown;
            bool picking = !gameOver && choosing && !p.StageChoiceComplete.Value;
            bool show = !gameOver && (choosing || countdown);
            _cardRoot.SetActive(show);
            if (!show) return;
            var ally = Teammate(p);
            string allyStatus = ally == null ? "" : ally.StageChoiceComplete.Value ? "팀원 선택 완료" : "팀원 선택 중";
            _cardHeader.text = countdown
                ? $"스테이지 클리어! 다음 스테이지까지 {Mathf.CeilToInt(session.NextStageCountdown.Value)}초"
                : picking ? $"스테이지 클리어! 능력을 고르세요 · {allyStatus}" : $"선택 완료 · {allyStatus}";
            if (!picking)
            {
                for(int i=0;i<CardCount;i++) _cards[i].gameObject.SetActive(false);
                return;
            }

''')
edit('UI/GameHud.cs','_stage.text = $"스테이지 {stage}   조각 {got} / {needPieces}{held}{boss}";', '_stage.text = stage % 5 == 0 ? $"스테이지 {stage}   보스 처치 목표" : $"스테이지 {stage}   조각 {got} / {needPieces}{held}";')
edit('World/PearlStackVisual.cs','            Animate(Time.time);','''            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = session.StatueActivated.Value ? new Color(1f,.85f,.3f) : Color.white;
            if (session.StatueActivated.Value) _label.text = "CLEAR";
            else if (session.Stage.Value % 5 == 0) _label.text = "BOSS";
            Animate(Time.time);''')
