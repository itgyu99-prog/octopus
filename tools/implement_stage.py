from pathlib import Path
p = Path('Source/문어_유한맵/Assets/Scripts')
def edit(file, old, new):
    f=p/file; s=f.read_text(encoding='utf-8-sig')
    assert old in s, (file, old[:100])
    f.write_text(s.replace(old,new),encoding='utf-8')
edit('Core/GameTypes.cs','    public enum UpgradeId', '    public enum StageState : byte { Combat, Choosing, Countdown }\n\n    public enum UpgradeId')
edit('World/GameSession.cs','        readonly List<SimActor> _actors', '''        public readonly NetworkVariable<byte> StageFlow = new NetworkVariable<byte>((byte)StageState.Combat);
        public readonly NetworkVariable<float> NextStageCountdown = new NetworkVariable<float>(0f);
        public readonly NetworkVariable<bool> StatueActivated = new NetworkVariable<bool>(false);
        public bool CombatActive => IsPlaying && !GameOver.Value && StageFlow.Value == (byte)StageState.Combat && !AnyPendingPicks();

        readonly List<SimActor> _actors''')
edit('World/GameSession.cs','        bool _portalSpawned;\n','')
edit('World/GameSession.cs','            if (TickLevelUp(dt))','            if (TickStageTransition(dt) || TickLevelUp(dt))')
edit('World/GameSession.cs','            TickActors(dt);\n            TickPlayers(dt);','''            TickActors(dt);
            if (StageFlow.Value != (byte)StageState.Combat) { SyncActors(dt); return; }
            TickPlayers(dt);
            if (StageFlow.Value != (byte)StageState.Combat) { SyncActors(dt); return; }''')
edit('World/GameSession.cs','        void SyncActors(float dt)', '''        bool TickStageTransition(float dt)
        {
            if (StageFlow.Value == (byte)StageState.Combat) return false;
            if (StageFlow.Value == (byte)StageState.Choosing)
            {
                bool ready = OctopusPlayer.All.Count > 0;
                foreach (var player in OctopusPlayer.All)
                    if (player != null && player.IsSpawned && !player.StageChoiceComplete.Value) ready = false;
                if (ready)
                {
                    StageFlow.Value = (byte)StageState.Countdown;
                    NextStageCountdown.Value = 3f;
                }
                return true;
            }
            NextStageCountdown.Value = Mathf.Max(0f, NextStageCountdown.Value - dt);
            if (NextStageCountdown.Value <= 0f) StartArtifactStage(Stage.Value + 1);
            return true;
        }

        void CompleteStage()
        {
            if (!IsServer || StageFlow.Value != (byte)StageState.Combat) return;
            StageFlow.Value = (byte)StageState.Choosing;
            StatueActivated.Value = true;
            LevelUpLeft.Value = 0f;
            _delayed.Clear();
            // Clear combat actors directly: no KillEnemy, XP or loot side effects.
            foreach (var actor in _actors)
                if (actor.Kind != ActorKind.Statue && !KeepAcrossStage(actor.Kind)) actor.Dead = true;
            foreach (var player in OctopusPlayer.All)
                if (player != null && player.IsSpawned)
                {
                    player.StopCombat();
                    player.GrantStageChoice();
                }
        }

        void SyncActors(float dt)''')
edit('World/GameSession.cs','            _portalSpawned = false;\n            SpawnMarker(ActorKind.Statue, RandomInterior(7f), 1.55f);','''            StageFlow.Value = (byte)StageState.Combat;
            NextStageCountdown.Value = 0f;
            StatueActivated.Value = false;
            LevelUpLeft.Value = 0f;
            _waveTimer = 0f;
            SpawnMarker(ActorKind.Statue, Vector2.zero, 1.55f);''')
f=p/'World/GameSession.cs'; s=f.read_text(encoding='utf-8'); start=s.index('            int need = GameConfig.RequiredPieces(Stage.Value);',s.index('        void TickArtifacts()')); end=s.index('\n        void SpawnMarker',start)
s=s[:start]+'''            if (Stage.Value % GameConfig.BossEvery != 0 &&
                ArtifactDelivered.Value >= GameConfig.RequiredPieces(Stage.Value)) CompleteStage();
        }
''' +s[end:]
s=s.replace('            if (stage == 5)\n                DropBossSpecial(pos);\n            else if (stage == 10)\n                SpecialPassive.Value = true;', '''            DropBossSpecial(pos);
            if (stage == 10) SpecialPassive.Value = true;
            CompleteStage();''')
# Clearing or damaging actors during a boss kill may re-enter loops. Gate all further damage.
s=s.replace('            if (enemy.Dead || (enemy.Kind != ActorKind.Enemy && enemy.Kind != ActorKind.Boss))','            if (!CombatActive || enemy.Dead || (enemy.Kind != ActorKind.Enemy && enemy.Kind != ActorKind.Boss))')
s=s.replace('            for (int i = 0; i < _actors.Count; i++)\n            {\n                var a = _actors[i];\n                if (a.Dead)\n                    continue;\n\n                if (TickElementEffect(a, dt)) continue;', '            for (int i = 0; i < _actors.Count; i++)\n            {\n                if (!CombatActive) break;\n                var a = _actors[i];\n                if (a.Dead)\n                    continue;\n\n                if (TickElementEffect(a, dt)) continue;')
f.write_text(s,encoding='utf-8')
edit('Player/OctopusPlayer.cs','        public readonly NetworkVariable<bool> HasArtifact', '        public readonly NetworkVariable<bool> StageChoiceComplete = new NetworkVariable<bool>(true);\n        public readonly NetworkVariable<bool> HasArtifact')
edit('Player/OctopusPlayer.cs','        void CancelCasts()', '''        public void StopCombat()
        {
            if (!IsServer) return;
            CancelCasts();
            AttackLeft.Value = 0f;
            ComboWindow.Value = false;
        }

        public void GrantStageChoice()
        {
            if (!IsServer) return;
            PendingPicks.Value = 1;
            StageChoiceComplete.Value = false;
            RollOffers();
            if (Offer0.Value == 0) { PendingPicks.Value = 0; StageChoiceComplete.Value = true; }
        }

        void CancelCasts()''')
edit('Player/OctopusPlayer.cs','if (Downed.Value || GameSession.Instance == null || GameSession.Instance.LevelUpLeft.Value > 0.05f)', 'if (Downed.Value || GameSession.Instance == null || !GameSession.Instance.CombatActive)')
edit('Player/OctopusPlayer.cs','            if (!IsServer || Downed.Value || Invincible.Value || _hitIFrame > 0f)','            if (!IsServer || GameSession.Instance == null || !GameSession.Instance.CombatActive || Downed.Value || Invincible.Value || _hitIFrame > 0f)')
edit('Player/OctopusPlayer.cs','            else\n                Offer0.Value = Offer1.Value = Offer2.Value = 0;','''            else
            {
                Offer0.Value = Offer1.Value = Offer2.Value = 0;
                StageChoiceComplete.Value = true;
            }''')
edit('Core/Upgrades.cs','            UpgradeId.Attack,\n            UpgradeId.Range,','            UpgradeId.MaxHp,')
edit('Core/Upgrades.cs','                    return int.MaxValue;', '                    return byte.MaxValue;')
