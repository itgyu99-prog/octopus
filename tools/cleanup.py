from pathlib import Path
p=Path('Source/문어_유한맵/Assets/Scripts')
def edit(file,old,new):
 f=p/file;s=f.read_text(encoding='utf-8-sig');assert old in s,(file,old[:80]);f.write_text(s.replace(old,new),encoding='utf-8')
def cut(file,start,end,new=''):
 f=p/file;s=f.read_text(encoding='utf-8-sig');a=s.index(start);b=s.index(end,a);f.write_text(s[:a]+new+s[b:],encoding='utf-8')
cut('World/GameSession.cs','        public readonly NetworkVariable<float> LevelUpLeft', '        public readonly NetworkVariable<byte> HostElement')
edit('World/GameSession.cs','            LevelUpLeft.Value = 0f;\n','')
edit('World/GameSession.cs','        readonly List<Respawn> _respawns = new List<Respawn>(16);\n','')
edit('World/GameSession.cs','            _respawns.Clear();\n','')
cut('World/GameSession.cs','        struct Respawn', '        struct Delayed')
cut('World/GameSession.cs','        void TickRespawns', '        void TickDelayed')
cut('World/GameSession.cs','        Vector2 FindSpreadPoint', '        void SpawnWave')
cut('World/GameSession.cs','                    case ActorKind.XpOrb:', '                    case ActorKind.Droplet:')
edit('UI/GameHud.cs','        Text _xpText;\n','')
edit('UI/GameHud.cs','        Image _xpFill;\n','')
# Removed legacy ability ticking: all new base casts are independent server actors.
edit('Player/OctopusPlayer.cs','                AbilityRunner.Tick(this, GameSession.Instance, dt);\n','')
# Do not leave old fire ring visual on the client, new synced FlameAura actor handles it.
f=p/'Player/OctopusPlayer.cs';s=f.read_text(encoding='utf-8');a=s.find('            if (!IsServer)',s.index('        void UpdateVisuals'))
print(s[a:a+1500])
# Reset unfinished picks on a developer jump; normal transition already has completed choices.
edit('World/GameSession.cs','                    player.ClearArtifact();','''                {
                    player.ClearArtifact();
                    player.StopCombat();
                    player.PendingPicks.Value = 0;
                    player.Offer0.Value = player.Offer1.Value = player.Offer2.Value = 0;
                    player.StageChoiceComplete.Value = true;
                }''')
# Require a valid selection even when readiness comes through an RPC.
edit('World/GameSession.cs','            if (rpc.Receive.SenderClientId == NetworkManager.ServerClientId)\n                HostReady.Value = ready;\n            else\n                GuestReady.Value = ready;', '''            if (rpc.Receive.SenderClientId == NetworkManager.ServerClientId)
                HostReady.Value = ready && HostElement.Value != 0;
            else
                GuestReady.Value = ready && GuestElement.Value != 0;''')
edit('World/GameSession.cs','            if (count < 2 && GuestReady.Value)\n                GuestReady.Value = false;', '''            if (count < 2)
            {
                GuestReady.Value = false;
                GuestElement.Value = 0;
            }''')
# Prevent late joins from entering without a starting selection. Disconnect/rejoin returns to lobby.
edit('Net/GameBootstrap.cs','response.Approved = already < GameConfig.MaxPlayers;', 'response.Approved = already < GameConfig.MaxPlayers && (GameSession.Instance == null || !GameSession.Instance.IsPlaying);')
edit('Net/GameBootstrap.cs','response.Reason = response.Approved ? string.Empty : "최대 2인";', 'response.Reason = response.Approved ? string.Empty : "진행 중인 게임에는 참가할 수 없습니다. 대기실에서 함께 시작해 주세요.";')
# Keep the inspector check relevant to the revised six recipes.
f=p.parent/'Editor/ElementEffectChecks.cs';s=f.read_text(encoding='utf-8-sig');a=s.index('            Check(ElementId.Fire');b=s.index('            foreach (ElementId',a)
s=s[:a]+'''            Check(ElementId.Fire, ElementId.Water, ComboId.Fog);
            Check(ElementId.Fire, ElementId.Grass, ComboId.Chili);
            Check(ElementId.Water, ElementId.Grass, ComboId.Tree);
            Check(ElementId.Fire, ElementId.Electric, ComboId.BlueFlame);
            Check(ElementId.Grass, ElementId.Electric, ComboId.Lemon);
            Check(ElementId.Water, ElementId.Electric, ComboId.Soda);
'''+s[b:];s=s.replace('GameConfig.ActorBytes != 24','GameConfig.ActorBytes != 25').replace('10종 ×','6종 ×');f.write_text(s,encoding='utf-8')
