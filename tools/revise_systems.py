from pathlib import Path
p = Path(__file__).resolve().parents[1] / 'Source/문어_유한맵/Assets/Scripts'
def edit(name, old, new):
    f=p/name; s=f.read_text(encoding='utf-8-sig'); assert old in s,(name,old[:90]); f.write_text(s.replace(old,new),encoding='utf-8')
def block(name, start, end, new):
    f=p/name; s=f.read_text(encoding='utf-8-sig'); a=s.index(start); b=s.index(end,a); f.write_text(s[:a]+new+s[b:],encoding='utf-8')

edit('World/ArenaBuilder.cs','public const float Height = GameConfig.CameraSize * 2 * 3;\n        public const float Width = Height * Aspect;', 'public const float Height = GameConfig.CameraSize * 2 * 2;\n        public const float ArtworkAspect = 1492f / 1054f;\n        public const float Width = Height * ArtworkAspect;')
edit('Net/GameBootstrap.cs','config.ProtocolVersion = 2;', 'config.ProtocolVersion = 3;')
edit('Core/GameTypes.cs','Mist = 80,','Mist = 80, WaterEmitter = 81, RadialCurrent = 82,')
edit('Core/GameplayRules.cs','public float attributeDropChance;', 'public float attributeDropChance;\n        public int waterProjectileCount = 8;\n        public float attributeRepeatInterval = .22f;')
edit('Core/GameConfig.cs','// 속성은 3초마다 한 번씩 발동하고, 2개를 들고 있으면 번갈아 나간다.', '// 보유한 최대 4개 속성이 3초 주기에 한 개씩 순서대로 발동한다.')

n='Player/OctopusPlayer.cs'
edit(n,'public readonly NetworkVariable<byte> Slot2Lv = new NetworkVariable<byte>(0);','public readonly NetworkVariable<byte> Slot2Lv = new NetworkVariable<byte>(0);\n        public readonly NetworkVariable<byte> Slot3 = new NetworkVariable<byte>(0);\n        public readonly NetworkVariable<byte> Slot3Lv = new NetworkVariable<byte>(0);')
edit(n,'public readonly NetworkVariable<byte> PendingPicks', 'public readonly NetworkVariable<uint> ChoiceRevision = new NetworkVariable<uint>(0);\n        float nextLocalPick, nextLocalCombine;\n        public readonly NetworkVariable<byte> PendingPicks')
edit(n,'if (GameSession.Instance == null)\n                return;\n\n            // 변신', 'if (GameSession.Instance == null || !GameSession.Instance.CombatActive)\n                return;\n\n            // 변신')
edit(n,'PendingPicks.Value = 1;', 'PendingPicks.Value = 2;')
block(n,'        readonly float[] attributeCooldowns', '        void TickComboCycle', '''        float attributeCycleLeft;
        readonly float[] comboCooldowns = new float[3];
        void TickAttributeCycle(float dt)
        {
            attributeCycleLeft -= dt;
            if (attributeCycleLeft > 0f) return;
            for (byte i = 0; i < 4; i++)
            {
                byte slot = (byte)((ActiveSlot.Value + i) % 4);
                var element = SlotElement(slot);
                if (element == ElementId.None) continue;
                attributeCycleLeft = GameConfig.AttributeCycle * GameConfig.AttributeCdMul(UpAttrCd.Value);
                CastElement = element;
                CastLevel = EffectiveAttrLevel(slot);
                GameSession.Instance.CastBasicAbility(this, element, CastLevel);
                ActiveSlot.Value = (byte)((slot + 1) % 4);
                break;
            }
        }
''')
edit(n,'if (Downed.Value || GameSession.Instance == null || !GameSession.Instance.CombatActive)\n                return;','if (Downed.Value || GameSession.Instance == null || !GameSession.Instance.IsPlaying || GameSession.Instance.GameOver.Value)\n                return;\n            if (!GameSession.Instance.CombatActive && action != 2) return;')
edit(n,'if (Special.Value != 0)\n                    return;', 'if (Special.Value != 0 || !GameSession.Instance.SpecialUnlocked.Value)\n                    return;')
block(n,'        public bool GrantElement', '        public ComboId ComboAt', '''        public bool HasElement(ElementId element)
        {
            for (byte i = 0; i < 4; i++) if (SlotElement(i) == element) return true;
            return false;
        }
        public bool GrantElement(ElementId element)
        {
            if (!IsServer || !GameplayRules.IsBasic(element) || HasElement(element)) return false;
            for (byte i = 0; i < 4; i++)
                if (SlotElement(i) == ElementId.None) { SetSlot(i, element, 1); return true; }
            return false;
        }

''')
edit(n,'return first < 3 && second < 3', 'return first < 4 && second < 4')
edit(n,'!Downed.Value && ComboCount < session.UnlockedComboSlots.Value && ComboCount < 3 &&', '!Downed.Value && ComboCount < 3 &&')
edit(n,'if (IsOwner) CombineServerRpc(first, second);', 'if (!IsOwner || Time.unscaledTime < nextLocalCombine) return;\n            nextLocalCombine = Time.unscaledTime + .3f;\n            CombineServerRpc(first, second, (byte)ComboCount);')
edit(n,'void CombineServerRpc(byte first, byte second) => ServerCombine(first, second);', 'void CombineServerRpc(byte first, byte second, byte expectedCount)\n        { if (expectedCount == ComboCount) ServerCombine(first, second); }')
edit(n,'            SetSlot(first, ElementId.None, 0);\n            SetSlot(second, ElementId.None, 0);\n','')
edit(n,'            attributeCooldowns[slot] = 0f;', '            else if (slot == 3) { Slot3.Value = (byte)element; Slot3Lv.Value = rank; }')
edit(n,'if (!IsServer)\n                return;\n            Special.Value = specialId;', 'if (!IsServer || (specialId != 0 && (GameSession.Instance == null || !GameSession.Instance.SpecialUnlocked.Value)))\n                return;\n            Special.Value = specialId;')
edit(n,'slot > 2 ||', 'slot > 3 ||')
edit(n,'            SetSlot(slot, (ElementId)elementId, elementId == 0 ? 0 : 1);', '            for (byte i = 0; i < 4; i++)\n                if (i != slot && elementId != 0 && SlotElement(i) == (ElementId)elementId) return;\n            SetSlot(slot, (ElementId)elementId, elementId == 0 ? 0 : 1);')
edit(n,'        void RollOffers()\n        {', '        void RollOffers()\n        {\n            ChoiceRevision.Value++;')
edit(n,'if (!IsOwner || PendingPicks.Value == 0 || Offer(index) == UpgradeId.None)', 'if (!IsOwner || Time.unscaledTime < nextLocalPick || PendingPicks.Value == 0 || Offer(index) == UpgradeId.None)')
edit(n,'            ChooseUpgradeServerRpc((byte)index);', '            nextLocalPick = Time.unscaledTime + .3f;\n            ChooseUpgradeServerRpc((byte)index, ChoiceRevision.Value);')
edit(n,'void ChooseUpgradeServerRpc(byte index)\n        {\n            if (PendingPicks.Value == 0)', 'void ChooseUpgradeServerRpc(byte index, uint revision)\n        {\n            if (PendingPicks.Value == 0 || revision != ChoiceRevision.Value || GameSession.Instance.StageFlow.Value != (byte)StageState.Choosing)')
edit(n,'return Mathf.Clamp(SlotLevel(slot), 1, 5);', 'return Mathf.Clamp(1 + UpAttrRank.Value, 1, 5);')
edit(n,'slot == 2 ? Slot2.Value : 0', 'slot == 2 ? Slot2.Value : slot == 3 ? Slot3.Value : 0')
edit(n,'slot == 2 ? Slot2Lv.Value : 0', 'slot == 2 ? Slot2Lv.Value : slot == 3 ? Slot3Lv.Value : 0')
block(n,'                else if (!IsServer)\n                {\n                    bool fire', '\n            }\n        }\n    }', '''                else SetFireRing(0f);''')

n='World/GameSession.cs'
edit(n,'public readonly NetworkVariable<byte> UnlockedComboSlots = new NetworkVariable<byte>(0);', 'public readonly NetworkVariable<byte> UnlockedComboSlots = new NetworkVariable<byte>(3);\n        public readonly NetworkVariable<bool> SpecialUnlocked = new NetworkVariable<bool>(false);')
edit(n,'UnlockedComboSlots.Value = 0;', 'UnlockedComboSlots.Value = 3;\n            SpecialUnlocked.Value = false;')
edit(n,'if (TickStageTransition(dt))\n            {\n                SyncActors(dt);', 'if (TickStageTransition(dt))\n            {\n                TickPlayers(dt);\n                SyncActors(dt);')
edit(n,'if (p.Downed.Value)\n                    continue;\n\n                EnemyContact', 'if (p.Downed.Value || !CombatActive)\n                    continue;\n\n                EnemyContact')
edit(n,'            if (stage == 5 || stage == 10 || stage == 15)\n                UnlockedComboSlots.Value = (byte)Mathf.Max(UnlockedComboSlots.Value, stage / 5);\n            DropBossSpecial(pos);', '            if (stage == 5)\n            {\n                SpecialUnlocked.Value = true;\n                DropBossSpecial(pos);\n            }')
edit(n,'if (Random.value < GameplayRules.Data.attributeDropChance)\n                    SpawnPickup(GameplayRules.BasicElements[Random.Range(0, GameplayRules.BasicElements.Length)], enemy.Pos);', 'TryDropAttribute(enemy.Pos);')
edit(n,'if (actor.Dead || KeepAcrossStage(actor.Kind))', 'if (actor.Dead)')
edit(n,'!KeepAcrossStage(actor.Kind)', '!KeepOnStageClear(actor.Kind)')
edit(n,'static bool KeepAcrossStage(ActorKind kind)', 'static bool KeepOnStageClear(ActorKind kind)')
insert='''        public bool AttributeNeeded(ElementId element)
        {
            foreach (var player in OctopusPlayer.All)
                if (player != null && player.IsSpawned && !player.HasElement(element)) return true;
            return false;
        }
        void TryDropAttribute(Vector2 pos)
        {
            if (Random.value >= GameplayRules.Data.attributeDropChance) return;
            var candidates = new List<ElementId>(4);
            foreach (var element in GameplayRules.BasicElements)
                if (AttributeNeeded(element)) candidates.Add(element);
            if (candidates.Count > 0) SpawnPickup(candidates[Random.Range(0, candidates.Count)], pos);
        }

'''
edit(n,'        void StartArtifactStage(int stage)', insert+'        void StartArtifactStage(int stage)')
edit('UI/DevMenu.cs','            _status = UiUtil.Label', '            LabelAt(752f, "속성4  (누른 사람만)");\n            ElementRow(784f, 3);\n            _status = UiUtil.Label')
edit('UI/DevMenu.cs','-820f','-846f')
edit('UI/DevMenu.cs','특수요소  (둘 다 적용)', '특수요소 (5보스 처치 후)')
edit('Core/Upgrades.cs','case UpgradeId.AttributeRank: return "속성 데미지";', 'case UpgradeId.AttributeRank: return "속성 공격 강화";')
edit('Core/Upgrades.cs','return $"속성 피해 +{picks * 22}% → +{(picks + 1) * 22}%";', 'return $"피해 +22% · 물/전기 발사 +1회\\n불 범위 +0.3";')
f=p.parent/'Resources/GameplayRules.json';s=f.read_text(encoding='utf-8').replace('"attributeDropChance": 0.05','"attributeDropChance": 0.02,\n  "waterProjectileCount": 8,\n  "attributeRepeatInterval": 0.22');f.write_text(s,encoding='utf-8')
