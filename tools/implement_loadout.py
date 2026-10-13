from pathlib import Path
p=Path('Source/문어_유한맵/Assets/Scripts')
def edit(file, old, new):
 f=p/file; s=f.read_text(encoding='utf-8-sig'); assert old in s,(file,old[:80]); f.write_text(s.replace(old,new),encoding='utf-8')
def cut(file,start,end,new):
 f=p/file; s=f.read_text(encoding='utf-8-sig'); a=s.index(start); b=s.index(end,a); f.write_text(s[:a]+new+s[b:],encoding='utf-8')
edit('Core/GameTypes.cs','        Magnet = 10','        Soda = 11, Lemon = 12, BlueFlame = 13,\n        Magnet = 10')
edit('Core/GameTypes.cs','        PiercingMeteor = 70','''        SodaWave = 71, LemonShot = 72, FruitTree = 73, Fruit = 74,
        ChainEmitter = 75, ChainArc = 76, FlameAura = 77, ThornEmitter = 78, CurrentEmitter = 79,
        PiercingMeteor = 70''')
edit('Core/GameConfig.cs','public static int RequiredPieces(int stage) => Mathf.Max(1, stage) * 2;', 'public static int RequiredPieces(int stage) => GameplayRules.Pieces(stage);')
cut('Elements/ElementRules.cs','            combo = ComboId.None;', '\n        }\n    }\n}', '            return GameplayRules.TryRecipe(a, b, out combo);')
edit('Elements/ElementRules.cs','case ComboId.Tree: return "씨앗";', 'case ComboId.Tree: return "나무";\n                case ComboId.Soda: return "탄산수";\n                case ComboId.Lemon: return "레몬";\n                case ComboId.BlueFlame: return "청염";')
edit('Player/OctopusPlayer.cs','public sealed class OctopusPlayer','public sealed partial class OctopusPlayer')
edit('Player/OctopusPlayer.cs','        public readonly NetworkVariable<byte> Combo =', '''        public readonly NetworkVariable<byte> Slot2 = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> Slot2Lv = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> Combo1 = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> Combo2 = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> Combo =''')
edit('Player/OctopusPlayer.cs','                Level.Value = 1;', '''                var session = GameSession.Instance;
                if (session != null)
                {
                    GrantElement((ElementId)(hostPlayer ? session.HostElement.Value : session.GuestElement.Value));
                    if (session.StageFlow.Value == (byte)StageState.Choosing) GrantStageChoice();
                }''')
cut('Player/OctopusPlayer.cs','        void TickAttributeCycle(float dt)', '        [ServerRpc]\n        void PressServerRpc', '''        readonly float[] attributeCooldowns = new float[3];
        readonly float[] comboCooldowns = new float[3];
        void TickAttributeCycle(float dt)
        {
            for (byte slot = 0; slot < 3; slot++) attributeCooldowns[slot] -= dt;
            for (byte i = 0; i < 3; i++)
            {
                byte slot = (byte)((ActiveSlot.Value + i) % 3);
                var element = SlotElement(slot);
                if (element == ElementId.None || attributeCooldowns[slot] > 0f) continue;
                attributeCooldowns[slot] = GameConfig.AttributeCycle * GameConfig.AttributeCdMul(UpAttrCd.Value);
                CastElement = element;
                CastLevel = EffectiveAttrLevel(slot);
                GameSession.Instance.CastBasicAbility(this, element, CastLevel);
                ActiveSlot.Value = (byte)((slot + 1) % 3);
                break;
            }
        }
        void TickComboCycle(float dt)
        {
            for (byte slot = 0; slot < 3; slot++)
            {
                comboCooldowns[slot] -= dt;
                var combo = ComboAt(slot);
                if (combo == ComboId.None || comboCooldowns[slot] > 0f) continue;
                var tuning = GameplayRules.Tuning(combo);
                comboCooldowns[slot] = tuning.duration + tuning.cooldown * GameConfig.ElementCdMul(UpElemCd.Value);
                GameSession.Instance.CastPlannedCombo(this, combo);
            }
        }

''')
edit('Player/OctopusPlayer.cs','                case 3: TryCombine(); break;', '                case 3: break; // F opens the local recipe picker; selection uses an ownership-checked RPC.')
edit('Player/OctopusPlayer.cs','                if (_input.CombinePressed)\n                    PressServerRpc(3);', '                if (_input.CombinePressed)\n                    GameHud.Instance?.ToggleCombine();')
cut('Player/OctopusPlayer.cs','        bool GrantElement(ElementId element)', '        public void ApplyDamage', '''        public bool GrantElement(ElementId element)
        {
            if (!IsServer || !GameplayRules.IsBasic(element)) return false;
            for (byte i = 0; i < 3; i++)
                if (SlotElement(i) == element)
                {
                    if (SlotLevel(i) >= 5) return false;
                    SetSlot(i, element, SlotLevel(i) + 1);
                    return true;
                }
            for (byte i = 0; i < 3; i++)
                if (SlotElement(i) == ElementId.None) { SetSlot(i, element, 1); return true; }
            return false;
        }

        public ComboId ComboAt(byte slot) => (ComboId)(slot == 0 ? Combo.Value : slot == 1 ? Combo1.Value : slot == 2 ? Combo2.Value : 0);
        public int ComboCount => (Combo.Value != 0 ? 1 : 0) + (Combo1.Value != 0 ? 1 : 0) + (Combo2.Value != 0 ? 1 : 0);
        public bool HasCombo(ComboId combo) => ComboAt(0) == combo || ComboAt(1) == combo || ComboAt(2) == combo;
        public bool CanCombine(byte first, byte second, out ComboId result)
        {
            result = ComboId.None;
            var session = GameSession.Instance;
            return first < 3 && second < 3 && first != second && session != null && session.CombatActive &&
                !Downed.Value && ComboCount < session.UnlockedComboSlots.Value && ComboCount < 3 &&
                ElementRules.TryCombo(SlotElement(first), SlotElement(second), out result);
        }
        public void RequestCombine(byte first, byte second)
        {
            if (IsOwner) CombineServerRpc(first, second);
        }
        [ServerRpc]
        void CombineServerRpc(byte first, byte second) => ServerCombine(first, second);
        public bool ServerCombine(byte first, byte second)
        {
            if (!IsServer || !CanCombine(first, second, out var result)) return false;
            byte index = Combo.Value == 0 ? (byte)0 : Combo1.Value == 0 ? (byte)1 : (byte)2;
            if (index == 0) Combo.Value = (byte)result;
            else if (index == 1) Combo1.Value = (byte)result;
            else Combo2.Value = (byte)result;
            SetSlot(first, ElementId.None, 0);
            SetSlot(second, ElementId.None, 0);
            comboCooldowns[index] = 0f;
            return true;
        }
        void SetSlot(byte slot, ElementId element, int level)
        {
            byte rank = (byte)Mathf.Clamp(level, 0, 5);
            if (slot == 0) { Slot0.Value = (byte)element; Slot0Lv.Value = rank; }
            else if (slot == 1) { Slot1.Value = (byte)element; Slot1Lv.Value = rank; }
            else if (slot == 2) { Slot2.Value = (byte)element; Slot2Lv.Value = rank; }
            attributeCooldowns[slot] = 0f;
        }

''')
cut('Player/OctopusPlayer.cs','        public void DevSetLevel(int level)', '        public void DevSetElement', '')
cut('Player/OctopusPlayer.cs','        public void DevSetElement(byte slot, byte elementId)', '        public void DevResetSpecialCooldown', '''        public void DevSetElement(byte slot, byte elementId)
        {
            if (!IsServer || slot > 2 || (elementId != 0 && !GameplayRules.IsBasic((ElementId)elementId))) return;
            SetSlot(slot, (ElementId)elementId, elementId == 0 ? 0 : 1);
        }

''')
cut('Player/OctopusPlayer.cs','        public void AddXp(int amount)', '        void RollOffers()', '')
edit('Player/OctopusPlayer.cs','        public readonly NetworkVariable<int> Level = new NetworkVariable<int>(1);\n        public readonly NetworkVariable<int> Xp = new NetworkVariable<int>(0);\n','')
cut('Player/OctopusPlayer.cs','        public ElementId ActiveElement()', '        public int EffectiveAttrLevel', '''        public ElementId ActiveElement() => CastElement;

''')
cut('Player/OctopusPlayer.cs','        public ElementId SlotElement(byte slot)', '        public float MeleeRange()', '''        public ElementId SlotElement(byte slot) => (ElementId)(slot == 0 ? Slot0.Value : slot == 1 ? Slot1.Value : slot == 2 ? Slot2.Value : 0);
        public int SlotLevel(byte slot) => slot == 0 ? Slot0Lv.Value : slot == 1 ? Slot1Lv.Value : slot == 2 ? Slot2Lv.Value : 0;

''')
edit('Player/OctopusPlayer.cs','if ((ComboId)Combo.Value == ComboId.Chili)', 'if (HasCombo(ComboId.Chili))')
for field in ['_attrTimer','_comboTimer','_comboCd','_slotAge0','_slotAge1','_ageClock']:
 f=p/'Player/OctopusPlayer.cs'; s=f.read_text(encoding='utf-8'); import re
 s=re.sub(r'^.*\b'+field+r'\b.*\n','',s,flags=re.M); f.write_text(s,encoding='utf-8')
# Progression and server-owned starting selection.
edit('World/GameSession.cs','        public readonly NetworkVariable<byte> StageFlow', '''        public readonly NetworkVariable<byte> HostElement = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> GuestElement = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> UnlockedComboSlots = new NetworkVariable<byte>(0);
        public void SelectStartingElement(ElementId element)
        {
            if (IsSpawned && !IsPlaying && GameplayRules.IsBasic(element)) SelectStartingElementRpc((byte)element);
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void SelectStartingElementRpc(byte element, RpcParams rpc = default)
        {
            if (IsPlaying || !GameplayRules.IsBasic((ElementId)element)) return;
            if (rpc.Receive.SenderClientId == NetworkManager.ServerClientId) { HostElement.Value = element; HostReady.Value = false; }
            else { GuestElement.Value = element; GuestReady.Value = false; }
        }

        public readonly NetworkVariable<byte> StageFlow''')
edit('World/GameSession.cs','            if (!HostReady.Value)', '            if (!HostReady.Value || HostElement.Value == 0)')
edit('World/GameSession.cs','return PlayerCount.Value < 2 || GuestReady.Value;', 'return PlayerCount.Value == 2 && GuestReady.Value && GuestElement.Value != 0;')
edit('World/GameSession.cs','            _waveTimer = 0f;\n            _bossStage = 0;', '            UnlockedComboSlots.Value = 0;\n            _waveTimer = 0f;\n            _bossStage = 0;')
edit('World/GameSession.cs','            SeedPickups();\n','')
edit('World/GameSession.cs','            if (TickStageTransition(dt) || TickLevelUp(dt))','            if (TickStageTransition(dt))')
cut('World/GameSession.cs','        bool TickLevelUp(float dt)', '        bool AnyPendingPicks()', '')
cut('World/GameSession.cs','        public void RequestDevLevel(int level)', '        public void RequestDevElement', '')
cut('World/GameSession.cs','        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]\n        void DevLevelRpc', '        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]\n        void DevElementRpc', '')
cut('World/GameSession.cs','        void DevSetLevel(int level)', '        void DevSetElement', '')
edit('World/GameSession.cs','                MagnetXp(p, dt);','')
cut('World/GameSession.cs','        void MagnetXp(', '        ', '') if False else None
f=p/'World/GameSession.cs'; s=f.read_text(encoding='utf-8'); a=s.index('        void MagnetXp('); b=s.index('\n        ',s.index('\n        }',a)+10); s=s[:a]+s[b:]; f.write_text(s,encoding='utf-8')
cut('World/GameSession.cs','            var orb = Create(ActorKind.XpOrb', '            if (boss)', '')
edit('World/GameSession.cs','            DropBossSpecial(pos);','''            if (stage == 5 || stage == 10 || stage == 15)
                UnlockedComboSlots.Value = (byte)Mathf.Max(UnlockedComboSlots.Value, stage / 5);
            DropBossSpecial(pos);''')
edit('World/GameSession.cs','                if (Random.value < GameConfig.ArtifactDropChance)', '''                if (Random.value < GameplayRules.Data.attributeDropChance)
                    SpawnPickup(GameplayRules.BasicElements[Random.Range(0, GameplayRules.BasicElements.Length)], enemy.Pos);
                if (Stage.Value % GameConfig.BossEvery != 0 && Random.value < GameConfig.ArtifactDropChance)''')
# No periodic field seeding / infinite respawns: new attributes come from monster deaths.
cut('World/GameSession.cs','        void SeedPickups()', '        Vector2 FindSpreadPoint', '')
edit('World/GameSession.cs','            TickRespawns(dt);\n','')
cut('World/GameSession.cs','        public void CollectPickup(SimActor pickup)', '        public Vector2 DensestPoint', '''        public void CollectPickup(SimActor pickup)
        {
            if (IsServer && pickup != null && !pickup.Dead) pickup.Dead = true;
        }

''')
edit('Player/OctopusPlayer.cs','                if (GameSession.Instance != null)\n                    GameSession.Instance.OpenLevelUpWindow();\n','')
# Every slot offers the same independently applied six upgrade categories.
edit('Core/Upgrades.cs','"속성 단계"', '"속성 데미지"')
edit('Core/Upgrades.cs','"요소/특수요소 단계"', '"요소/특수요소 데미지"')
