from pathlib import Path
import re
p=Path(__file__).resolve().parents[1]/'Source/문어_유한맵/Assets/Scripts'
f=p/'Elements/AbilityRunner.cs';s=f.read_text(encoding='utf-8-sig');a=s.index('        public static void BeginCast');b=s.index('        public static void CastCombo');s=s[:a]+s[b:];f.write_text(s,encoding='utf-8')
f=p/'Player/OctopusPlayer.cs';s=f.read_text(encoding='utf-8-sig')
s=re.sub(r'        public readonly NetworkVariable<byte> Slot[0-3]Lv = .*\n','',s)
s=re.sub(r'        public (?:int|float) (?:CastStepsLeft|CastStepTimer|FireRingLeft|FireRingRadius);\n','',s)
s=s.replace('            CastStepsLeft = 0;\n','').replace('            FireRingLeft = 0f;\n','')
s=s.replace('SetSlot(i, element, 1)','SetSlot(i, element)').replace('SetSlot(slot, (ElementId)elementId, elementId == 0 ? 0 : 1)','SetSlot(slot, (ElementId)elementId)')
s=s.replace('void SetSlot(byte slot, ElementId element, int level)','void SetSlot(byte slot, ElementId element)').replace('            byte rank = (byte)Mathf.Clamp(level, 0, 5);\n','')
s=re.sub(r' Slot[0-3]Lv.Value = rank;','',s)
s=re.sub(r'        public int SlotLevel\(byte slot\) => .*\n','',s)
s=s.replace('NearestPickup(Pos, GameConfig.PickupRange)','NearestPickup(Pos, GameConfig.PickupRange, this)')
f.write_text(s,encoding='utf-8')
f=p/'World/GameSession.cs';s=f.read_text(encoding='utf-8-sig').replace('NearestPickup(Vector2 pos, float range)','NearestPickup(Vector2 pos, float range, OctopusPlayer player = null)')
a=s.index('        public SimActor NearestPickup');b=s.index('        public void CollectPickup',a)
piece=s[a:b].replace('                float d = (a.Pos - pos).sqrMagnitude;', '''                if (player != null && (ElementRules.IsAttributePickup(a.Kind)
                    ? player.HasElement(ElementRules.FromPickup(a.Kind))
                    : player.Special.Value != 0 || !SpecialUnlocked.Value)) continue;
                float d = (a.Pos - pos).sqrMagnitude;''')
s=s[:a]+piece+s[b:];f.write_text(s,encoding='utf-8')
