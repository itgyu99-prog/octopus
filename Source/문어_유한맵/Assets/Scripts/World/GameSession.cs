using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace OctopusSurvivors
{
    public sealed class SimActor
    {
        public ushort Id;
        public ActorKind Kind;
        public ElementId Element;
        public GiantMonsterDefinition Giant;
        public GiantState GiantState;
        public float CounterTime, ChargeTravel;
        public int HitPlayers;
        public Vector2 Pos;
        public Vector2 Vel;
        public float Size;
        public float Life;
        public float Hp;
        public float MaxHp;
        public float Damage;
        public int Bounces;
        public StatusMask StatusOnHit;
        public OctopusPlayer Owner;
        public bool Dead;
        public float BurnTime;
        public float BurnDps;
        public float SlowTime;
        public float StunTime;
        public float SpicyTime;
        public float HitFlash;
        public Vector2 SpicyDir;
        public Vector2 Knockback;
        public float ContactCooldown;
        public bool Fallen;
        public float Age, Pulse, Duration, SpicyDps = 10f, SpicyTurn, ShockTime, RootTime;
        public int EffectLevel = 1;
        public SimActor Target, LastHit;
        public Vector2 Anchor;
        public byte SkillPhase;
        public float SkillTime;
        public Vector2 SkillPoint;
        public int Mark;
        public ushort HitBy;
        public bool BlackFlame;
        public readonly HashSet<ushort> HitTargets = new HashSet<ushort>();
        public HashSet<ushort> BlueFlameSplashTargets;
    }

    public sealed partial class GameSession : NetworkBehaviour
    {
        public static GameSession Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        public readonly NetworkVariable<byte> Phase = new NetworkVariable<byte>(
            (byte)GamePhase.Waiting, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<byte> PlayerCount = new NetworkVariable<byte>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<bool> HostReady = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<bool> GuestReady = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<bool> GameOver = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<float> Elapsed = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<int> Stage = new NetworkVariable<int>(
            1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<int> ArtifactDelivered = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<bool> BossAlive = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<float> BossHpRatio = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<float> TeamUltCooldown = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<bool> SpecialPassive = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<byte> HostElement = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> GuestElement = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> UnlockedComboSlots = new NetworkVariable<byte>(3);
        public readonly NetworkVariable<bool> SpecialUnlocked = new NetworkVariable<bool>(false);
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

        public readonly NetworkVariable<byte> StageFlow = new NetworkVariable<byte>((byte)StageState.Combat);
        public readonly NetworkVariable<float> NextStageCountdown = new NetworkVariable<float>(0f);
        public readonly NetworkVariable<bool> StatueActivated = new NetworkVariable<bool>(false);
        public bool CombatActive => IsPlaying && !GameOver.Value && StageFlow.Value == (byte)StageState.Combat && !AnyPendingPicks();

        readonly List<SimActor> _actors = new List<SimActor>(128);
        readonly List<Delayed> _delayed = new List<Delayed>(16);
        ushort _nextId = 1;
        float _waveTimer;
        float _syncTimer;
        int _bossStage;
        int _flameGen;
        WorldView _view;
        ushort _syncFrame;
        ushort _viewFrame;
        byte _viewChunksGot;
        byte _viewChunksNeed;

        struct Delayed
        {
            public float Time;
            public float Interval;
            public ActorKind Kind;
            public OctopusPlayer Owner;
            public int Remaining;
            public float Damage;
            public Vector2 Center;
            public bool UseCenter;
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            ArenaBuilder.Build();
            EnsureView();
            LanLobby.Hide();

            if (IsServer)
            {
                Phase.Value = (byte)GamePhase.Waiting;
                GameOver.Value = false;
                Elapsed.Value = 0f;
                Stage.Value = 1;
            }

            Phase.OnValueChanged += OnPhaseChanged;
            ApplyPhaseUi();

            NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(NetChannels.Actors, OnActorsMessage);
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager != null && NetworkManager.CustomMessagingManager != null)
                NetworkManager.CustomMessagingManager.UnregisterNamedMessageHandler(NetChannels.Actors);

            Phase.OnValueChanged -= OnPhaseChanged;
            if (Instance == this)
                Instance = null;

            WaitingRoom.Hide();
            ArenaBuilder.Destroy();
            if (_view != null)
                _view.Clear();
        }

        void OnPhaseChanged(byte prev, byte next) => ApplyPhaseUi();

        void ApplyPhaseUi()
        {
            if ((GamePhase)Phase.Value == GamePhase.Waiting)
            {
                GameHud.Hide();
                WaitingRoom.Show();
            }
            else
            {
                WaitingRoom.Hide();
                GameHud.Show();
            }
        }

        public bool IsPlaying => (GamePhase)Phase.Value == GamePhase.Playing;

        public bool CanStart()
        {
            if (!IsServer || IsPlaying)
                return false;
            if (!HostReady.Value || HostElement.Value == 0)
                return false;
            return PlayerCount.Value == 1 ||
                (PlayerCount.Value == 2 && GuestReady.Value && GuestElement.Value != 0);
        }

        public void ToggleLocalReady()
        {
            if (!IsSpawned || IsPlaying)
                return;
            bool mine = IsServer ? HostReady.Value : GuestReady.Value;
            SetReadyRpc(!mine);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void SetReadyRpc(bool ready, RpcParams rpc = default)
        {
            if (IsPlaying)
                return;
            if (rpc.Receive.SenderClientId == NetworkManager.ServerClientId)
                HostReady.Value = ready && HostElement.Value != 0;
            else
                GuestReady.Value = ready && GuestElement.Value != 0;
        }

        public void RequestStart()
        {
            if (IsServer && CanStart())
                BeginGame();
        }

        void BeginGame()
        {
            Phase.Value = (byte)GamePhase.Playing;
            GameOver.Value = false;
            Elapsed.Value = 0f;
            Stage.Value = 1;
            BossAlive.Value = false;
            BossHpRatio.Value = 0f;
            TeamUltCooldown.Value = 0f;
            SpecialPassive.Value = false;
            UnlockedComboSlots.Value = 3;
            SpecialUnlocked.Value = false;
            _waveTimer = 0f;
            _bossStage = 0;
            _actors.Clear();
            _delayed.Clear();

            GameBootstrap.SpawnAllPlayers();
            StartArtifactStage(1);
        }

        public void RequestDevStage(int stage)
        {
            if (!IsSpawned || !IsPlaying)
                return;
            if (IsServer)
                DevJumpStage(stage);
            else
                DevStageRpc(stage);
        }

        public void RequestDevSpecial(byte specialId)
        {
            if (!IsSpawned || !IsPlaying)
                return;
            if (IsServer)
                DevSetSpecial(specialId);
            else
                DevSpecialRpc(specialId);
        }

        public void RequestDevPassive(bool on)
        {
            if (!IsSpawned || !IsPlaying)
                return;
            if (IsServer)
                DevSetPassive(on);
            else
                DevPassiveRpc(on);
        }

        public void RequestDevElement(byte slot, byte elementId)
        {
            if (!IsSpawned || !IsPlaying)
                return;
            if (IsServer)
                DevSetElement(slot, elementId, NetworkManager.LocalClientId);
            else
                DevElementRpc(slot, elementId);
        }

        public void RequestDevResetSpecialCooldown()
        {
            if (!IsSpawned || !IsPlaying)
                return;
            if (IsServer)
                DevResetSpecialCooldown();
            else
                DevResetSpecialCooldownRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void DevStageRpc(int stage) => DevJumpStage(stage);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void DevSpecialRpc(byte specialId) => DevSetSpecial(specialId);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void DevPassiveRpc(bool on) => DevSetPassive(on);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void DevElementRpc(byte slot, byte elementId, RpcParams rpc = default) =>
            DevSetElement(slot, elementId, rpc.Receive.SenderClientId);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void DevResetSpecialCooldownRpc() => DevResetSpecialCooldown();

        void DevJumpStage(int stage)
        {
            if (!IsServer || !IsPlaying || GameOver.Value)
                return;

            stage = Mathf.Max(1, stage);
            ClearLiveBosses();
            Elapsed.Value = (stage - 1) * GameConfig.StageDuration + 0.05f;
            StartArtifactStage(stage);
        }

        void DevSetSpecial(byte specialId)
        {
            if (!IsServer)
                return;
            for (int i = 0; i < OctopusPlayer.All.Count; i++)
            {
                var player = OctopusPlayer.All[i];
                if (player != null && player.IsSpawned)
                    player.DevSetSpecial(specialId);
            }
        }

        void DevSetPassive(bool on)
        {
            if (!IsServer)
                return;
            SpecialPassive.Value = on;
        }

        void DevSetElement(byte slot, byte elementId, ulong clientId)
        {
            if (!IsServer)
                return;
            for (int i = 0; i < OctopusPlayer.All.Count; i++)
            {
                var player = OctopusPlayer.All[i];
                if (player != null && player.IsSpawned && player.OwnerClientId == clientId)
                    player.DevSetElement(slot, elementId);
            }
        }

        void DevResetSpecialCooldown()
        {
            if (!IsServer)
                return;
            for (int i = 0; i < OctopusPlayer.All.Count; i++)
            {
                var player = OctopusPlayer.All[i];
                if (player != null && player.IsSpawned)
                    player.DevResetSpecialCooldown();
            }
        }

        void ClearLiveBosses()
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                if (!_actors[i].Dead && _actors[i].Kind == ActorKind.Boss)
                    _actors[i].Dead = true;
            }

            BossAlive.Value = false;
            BossHpRatio.Value = 0f;
            _bossStage = 0;
        }

        void Update()
        {
            if (!IsServer)
                return;

            SyncPlayerCount();
            if (IsPlaying)
                ServerTick(Time.deltaTime);
        }

        void SyncPlayerCount()
        {
            int connected = NetworkManager != null ? NetworkManager.ConnectedClientsIds.Count : 0;
            byte count = (byte)Mathf.Clamp(connected, 0, 255);
            if (PlayerCount.Value != count)
                PlayerCount.Value = count;

            // 참가자가 나가면 준비 상태도 같이 풀어 준다.
            if (count < 2)
            {
                GuestReady.Value = false;
                GuestElement.Value = 0;
            }
        }

        void ServerTick(float dt)
        {
            if (GameOver.Value)
                return;

            if (TickStageTransition(dt))
            {
                TickPlayers(dt);
                SyncActors(dt);
                return;
            }

            Elapsed.Value += BossAlive.Value ? 0f : dt;

            if (TeamUltCooldown.Value > 0f)
                TeamUltCooldown.Value = Mathf.Max(0f, TeamUltCooldown.Value - dt);

            if (!BossAlive.Value)
            {
                _waveTimer -= dt;
                if (_waveTimer <= 0f)
                {
                    SpawnWave(GameConfig.WaveBurst(Elapsed.Value));
                    _waveTimer = GameConfig.WaveInterval(Elapsed.Value);
                }
            }

            TickDelayed(dt);
            TickActors(dt);
            if (StageFlow.Value != (byte)StageState.Combat) { SyncActors(dt); return; }
            TickPlayers(dt);
            if (StageFlow.Value != (byte)StageState.Combat) { SyncActors(dt); return; }
            TickArtifacts();
            TickPotions();
            CheckTeamUltimate();
            CheckGameOver();
            SyncActors(dt);
        }

        bool TickStageTransition(float dt)
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
            _delayed.Clear();
            // Clear combat actors directly: no KillEnemy, XP or loot side effects.
            foreach (var actor in _actors)
                if (actor.Kind != ActorKind.Statue && !ElementRules.IsSpecialPickup(actor.Kind)) actor.Dead = true;
            foreach (var player in OctopusPlayer.All)
                if (player != null && player.IsSpawned)
                {
                    player.StopCombat();
                    player.GrantStageChoice();
                }
        }

        void SyncActors(float dt)
        {
            _syncTimer -= dt;
            if (_syncTimer <= 0f)
            {
                _syncTimer = 0.04f;
                BroadcastActors();
            }

            RenderLocal();
        }

        bool AnyPendingPicks()
        {
            for (int i = 0; i < OctopusPlayer.All.Count; i++)
            {
                var p = OctopusPlayer.All[i];
                if (p != null && p.IsSpawned && p.PendingPicks.Value > 0)
                    return true;
            }

            return false;
        }

        void TickPlayers(float dt)
        {
            for (int i = 0; i < OctopusPlayer.All.Count; i++)
            {
                var p = OctopusPlayer.All[i];
                if (p == null || !p.IsSpawned)
                    continue;
                p.ServerTick(dt);
                if (p.Downed.Value || !CombatActive)
                    continue;

                EnemyContact(p, dt);

            }

            TickRevives(dt);
        }

        void TickRevives(float dt)
        {
            for (int i = 0; i < OctopusPlayer.All.Count; i++)
            {
                var downed = OctopusPlayer.All[i];
                if (downed == null || !downed.IsSpawned || !downed.Downed.Value)
                    continue;

                OctopusPlayer ally = null;
                for (int j = 0; j < OctopusPlayer.All.Count; j++)
                {
                    var other = OctopusPlayer.All[j];
                    if (other == null || other == downed || !other.IsSpawned || other.Downed.Value)
                        continue;
                    ally = other;
                    break;
                }

                bool occupying = ally != null &&
                                 Vector2.Distance(ally.Pos, downed.Pos) <= GameConfig.ReviveRadius;
                if (occupying)
                    downed.ReviveProgress.Value = Mathf.Min(1f, downed.ReviveProgress.Value + dt / GameConfig.ReviveHold);
                else
                    downed.ReviveProgress.Value = 0f;

                if (downed.ReviveProgress.Value >= 1f)
                    downed.Revive();
            }
        }

        void CheckTeamUltimate()
        {
            if (TeamUltCooldown.Value > 0f)
                return;

            OctopusPlayer a = null;
            OctopusPlayer b = null;
            for (int i = 0; i < OctopusPlayer.All.Count; i++)
            {
                var p = OctopusPlayer.All[i];
                if (p == null || !p.IsSpawned || p.Downed.Value || !p.Transformed.Value)
                    continue;
                if (a == null) a = p;
                else if (b == null) b = p;
            }

            if (a == null || b == null)
                return;
            if (a.Special.Value == 0 || b.Special.Value == 0 || a.Special.Value == b.Special.Value)
                return;
            if (Vector2.Distance(a.Pos, b.Pos) > GameConfig.PlayerRadius * 2.6f)
                return;

            int level = Mathf.Max(a.ElementLevel, b.ElementLevel);
            SpecialAbilities.CastPair(this, (a.Pos + b.Pos) * 0.5f, (SpecialId)a.Special.Value, (SpecialId)b.Special.Value, level);
            TeamUltCooldown.Value = GameConfig.TeamUltimateCooldown;
        }

        void TickActors(float dt)
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                if (!CombatActive) break;
                var a = _actors[i];
                if (a.Dead)
                    continue;

                if (TickUpgradeAbility(a, dt) || TickPlannedAbility(a, dt) || TickElementEffect(a, dt)) continue;

                switch (a.Kind)
                {
                    case ActorKind.Enemy:
                    case ActorKind.Boss:
                        TickEnemy(a, dt);
                        break;

                    case ActorKind.Droplet:
                    case ActorKind.Pepper:
                    case ActorKind.BounceRock:
                    case ActorKind.AutoBolt:
                        TickProjectile(a, dt);
                        break;

                    case ActorKind.Fog:
                        a.Life -= dt;
                        DamageCircle(a.Pos, a.Size, a.Damage * dt, StatusMask.Burn | StatusMask.Slow, 1.4f, 7f);
                        if (a.Life <= 0f) a.Dead = true;
                        break;

                    case ActorKind.HellfireZone:
                        a.Life -= dt;
                        DamageCircle(a.Pos, a.Size, a.Damage * dt, StatusMask.Burn | StatusMask.Slow, 1.6f, 14f);
                        if (a.Life <= 0f) a.Dead = true;
                        break;

                    case ActorKind.BlackHole:
                        TickBlackHole(a, dt);
                        break;

                    case ActorKind.HealthPotion:
                        break;

                    case ActorKind.AllySpirit:
                        TickAllySpirit(a, dt);
                        break;

                    case ActorKind.PiercingMeteor:
                        TickPiercingMeteor(a, dt);
                        break;

                    case ActorKind.GiantSpike:
                        TickGiantSpike(a, dt);
                        break;

                    case ActorKind.BossBullet:
                        TickBossBullet(a, dt);
                        break;

                    case ActorKind.Meteorite:
                        a.Life -= dt;
                        if (!a.Fallen && a.Life <= 0.15f)
                        {
                            a.Fallen = true;
                            if (a.Bounces < 0)
                                DamageAll(a.Damage);
                            else
                                DamageCircle(a.Pos, 2.1f, a.Damage, StatusMask.None, 0f, 0f);
                            SpawnTransient(ActorKind.Explosion, a.Pos, 3f, 0.2f);
                        }

                        if (a.Life <= 0f) a.Dead = true;
                        break;

                    case ActorKind.VolcanoRock:
                        a.Life -= dt;
                        if (a.Life <= 0f)
                        {
                            Vector2 dir = a.Vel.sqrMagnitude > 0.01f ? a.Vel.normalized : Vector2.right;
                            DamageCone(a.Pos, dir, 4.2f, 160f, a.Damage, StatusMask.Burn, 2f, 10f);
                            SpawnTransient(ActorKind.Explosion, a.Pos + dir * 1.5f, 3.4f, 0.35f);
                            a.Dead = true;
                        }

                        break;

                    case ActorKind.Tree:
                        a.Life -= dt;
                        if (!a.Fallen && a.Life <= 0.2f)
                        {
                            a.Fallen = true;
                            Vector2 dir = a.Vel.sqrMagnitude > 0.01f ? a.Vel.normalized : Vector2.right;
                            DamageLine(a.Pos, a.Pos + dir.normalized * 5.5f, 0.9f, a.Damage, StatusMask.None, 0f, 0f);
                        }

                        if (a.Life <= 0f) a.Dead = true;
                        break;

                    case ActorKind.DotZone:
                        a.Life -= dt;
                        DamageCircle(a.Pos, a.Size, 14f * dt, StatusMask.Burn, 1f, 8f);
                        if (a.Life <= 0f) a.Dead = true;
                        break;

                    default:
                        if (ElementRules.IsAttributePickup(a.Kind) || ElementRules.IsSpecialPickup(a.Kind))
                            break;

                        if (a.Kind == ActorKind.Vine) a.Age += dt;
                        a.Life -= dt;
                        if (a.Kind == ActorKind.Fish && a.Life < 0.55f && !a.Fallen)
                        {
                            a.Fallen = true;
                            KillInCircle(a.Pos, 1.15f);
                        }

                        if (a.Kind == ActorKind.LightningBolt && !a.Fallen)
                        {
                            a.Fallen = true;
                            if (DamageCircle(a.Pos, 1.1f, a.Damage, StatusMask.None, 0f, 0f, true))
                                SpawnPersistent(ActorKind.DotZone, a.Pos, Vector2.zero, 1.3f, 3.2f, 8f, a.Owner, StatusMask.Burn);
                        }

                        if (a.Life <= 0f) a.Dead = true;
                        break;
                }
            }

            for (int i = _actors.Count - 1; i >= 0; i--)
            {
                if (_actors[i].Dead)
                    _actors.RemoveAt(i);
            }
        }

        void TickProjectile(SimActor a, float dt)
        {
            Vector2 previous = a.Pos;
            a.Pos += a.Vel * dt;
            a.Life -= dt;
            a.Pos = GameConfig.ClampToMap(a.Pos, 0.2f);

            if (HitProjectileSegment(a, previous))
            {
                if (a.Kind == ActorKind.Droplet)
                {
                    DamageCircle(a.Pos, 1.25f, a.Damage * 0.75f, StatusMask.Slow, 1.3f, 0f);
                    SpawnTransient(ActorKind.WaterImpact, a.Pos, 1.6f, 0.2f);
                    SplitWater(a);
                    a.Dead = true;
                }
                else if (a.Kind == ActorKind.AutoBolt)
                {
                    a.Dead = true;
                }
                else if (a.Kind == ActorKind.Pepper)
                {
                    a.Dead = true;
                }
                else if (a.Kind == ActorKind.BounceRock)
                {
                    a.Bounces--;
                    if (a.Bounces <= 0)
                    {
                        a.Dead = true;
                    }
                    else
                    {
                        var next = RandomEnemy(a.Pos);
                        a.Vel = next != null ? (next.Pos - a.Pos).normalized * 11f : -a.Vel;
                    }
                }

                return;
            }

            if (a.Life <= 0f)
            {
                if (a.Kind == ActorKind.Droplet)
                {
                    DamageCircle(a.Pos, 1.1f, a.Damage * 0.5f, StatusMask.Slow, 0.8f, 0f);
                    SpawnTransient(ActorKind.WaterImpact, a.Pos, 1.3f, 0.18f);
                    SplitWater(a);
                }

                a.Dead = true;
            }
        }

        bool HitProjectileSegment(SimActor projectile, Vector2 previous)
        {
            SimActor target = null;
            float nearest = float.MaxValue;
            Vector2 travel = projectile.Pos - previous;
            float lengthSquared = travel.sqrMagnitude;
            float hitTime = 0f;
            for (int i = 0; i < _actors.Count; i++)
            {
                var enemy = _actors[i];
                if (enemy.Dead || (enemy.Kind != ActorKind.Enemy && enemy.Kind != ActorKind.Boss)) continue;
                float radius = projectile.Size + (enemy.Kind == ActorKind.Boss ? enemy.Size * .5f : enemy.Giant != null ? enemy.Size * .4f : GameConfig.EnemyRadius);
                float t = lengthSquared > .00001f ? Mathf.Clamp01(Vector2.Dot(enemy.Pos - previous, travel) / lengthSquared) : 0f;
                if ((enemy.Pos - (previous + travel * t)).sqrMagnitude > radius * radius || t >= nearest) continue;
                nearest = t; target = enemy; hitTime = t;
            }
            if (target == null) return false;
            projectile.Pos = previous + travel * hitTime;
            ApplyDamage(target, projectile.Damage, projectile.StatusOnHit, 1.4f, 8f);
            return true;
        }

        void TickBlackHole(SimActor hole, float dt)
        {
            hole.Life -= dt;
            float radius = hole.Size * 2.4f;
            for (int i = 0; i < _actors.Count; i++)
            {
                var e = _actors[i];
                if (e.Dead || (e.Kind != ActorKind.Enemy && e.Kind != ActorKind.Boss))
                    continue;
                if (hole.Bounces < 0 && e.Kind == ActorKind.Boss)
                    continue;
                Vector2 to = hole.Pos - e.Pos;
                if (to.sqrMagnitude > radius * radius)
                    continue;
                float pull = hole.Bounces < 0 || e.Kind == ActorKind.Boss ? 1.2f : 6.5f;
                e.Pos += to.normalized * pull * dt;
                ApplyDamage(e, hole.Damage * dt, StatusMask.None, 0f, 0f);
            }

            if (hole.Life <= 0f)
            {
                DamageCircle(hole.Pos, radius * 0.8f, hole.Damage * 6f, StatusMask.None, 0f, 0f, false, 1f, hole.Bounces >= 0);
                SpawnTransient(ActorKind.Explosion, hole.Pos, radius * 1.6f, 0.3f);
                hole.Dead = true;
            }
        }

        void TickEnemy(SimActor enemy, float dt)
        {
            if (enemy.BurnTime > 0f)
            {
                enemy.BurnTime -= dt;
                enemy.Hp -= enemy.BurnDps * dt * GiantDefenseFactor(enemy);
                if (enemy.BurnTime <= 0f)
                    enemy.BlackFlame = false;
            }

            if (enemy.SlowTime > 0f) enemy.SlowTime -= dt;
            if (enemy.StunTime > 0f) enemy.StunTime -= dt;
            if (enemy.SpicyTime > 0f)
            {
                enemy.SpicyTime -= dt;
                enemy.Hp -= enemy.SpicyDps * dt * GiantDefenseFactor(enemy);
                enemy.SpicyTurn -= dt;
                if (enemy.SpicyTurn <= 0f)
                {
                    enemy.SpicyTurn = Random.Range(0.15f, 0.35f);
                    enemy.SpicyDir = Random.insideUnitCircle.normalized;
                }
            }

            if (enemy.HitFlash > 0f)
                enemy.HitFlash -= dt;

            if (enemy.Knockback.sqrMagnitude > 0.01f)
            {
                enemy.Pos += enemy.Knockback * dt;
                enemy.Knockback = Vector2.Lerp(enemy.Knockback, Vector2.zero, 1f - Mathf.Exp(-6f * dt));
            }

            if (enemy.Hp <= 0f)
            {
                KillEnemy(enemy);
                return;
            }

            float speed = GameConfig.EnemySpeed(Elapsed.Value);
            if (enemy.Kind == ActorKind.Enemy && enemy.Giant == null)
                speed *= enemy.Element == ElementId.Electric ? 1.5f : enemy.Element == ElementId.Grass ? .72f : enemy.Element == ElementId.Fire ? 1.15f : .9f;
            if (enemy.Kind == ActorKind.Boss) speed *= 0.65f;
            if (enemy.SlowTime > 0f) speed *= 0.45f;
            if (enemy.StunTime > 0f || enemy.RootTime > 0f) speed = 0f;
            enemy.RootTime = Mathf.Max(0f, enemy.RootTime - dt);
            enemy.ShockTime = Mathf.Max(0f, enemy.ShockTime - dt);

            if (enemy.Giant != null) { TickGiant(enemy, dt, speed); return; }

            if (enemy.Kind == ActorKind.Boss)
            {
                TickBossPattern(enemy, dt, speed);
                return;
            }

            if (enemy.SpicyTime > 0f)
            {
                if (enemy.SpicyDir.sqrMagnitude < 0.01f)
                    enemy.SpicyDir = Random.insideUnitCircle.normalized;
                enemy.Pos += enemy.SpicyDir * speed * 2.3f * dt;
            }
            else
            {
                var target = NearestLivingPlayer(enemy.Pos);
                if (target != null)
                {
                    Vector2 dir = target.Pos - enemy.Pos;
                    if (dir.sqrMagnitude > 0.01f)
                        enemy.Pos += dir.normalized * speed * dt;
                }
            }

            enemy.Pos = GameConfig.ClampToMap(enemy.Pos, GameConfig.EnemyRadius);
            enemy.ContactCooldown -= dt;
        }

        void EnemyContact(OctopusPlayer player, float dt)
        {
            if (player.Invincible.Value)
                return;

            for (int i = 0; i < _actors.Count; i++)
            {
                var e = _actors[i];
                if (e.Dead || (e.Kind != ActorKind.Enemy && e.Kind != ActorKind.Boss))
                    continue;
                if (e.Giant != null) continue;
                if (e.SpicyTime > 0f || e.StunTime > 0f)
                    continue;
                float reach = GameConfig.PlayerRadius + (e.Kind == ActorKind.Boss ? e.Size * 0.5f : GameConfig.EnemyRadius);
                if ((e.Pos - player.Pos).sqrMagnitude > reach * reach)
                    continue;
                if (e.ContactCooldown > 0f)
                    continue;
                e.ContactCooldown = 0.45f;
                player.ApplyDamage(e.Kind == ActorKind.Boss ? GameConfig.EnemyContactDamage * 3f : GameConfig.EnemyContactDamage,
                    e.Kind == ActorKind.Boss ? DamageSource.BossContact : DamageSource.NormalEnemy);
            }
        }


        void TickDelayed(float dt)
        {
            for (int i = _delayed.Count - 1; i >= 0; i--)
            {
                var d = _delayed[i];
                d.Time -= dt;
                if (d.Time <= 0f)
                {
                    FireDelayed(d);
                    d.Remaining--;
                    d.Time = d.Interval;
                }

                if (d.Remaining <= 0)
                    _delayed.RemoveAt(i);
                else
                    _delayed[i] = d;
            }
        }

        void FireDelayed(Delayed d)
        {
            switch (d.Kind)
            {
                case ActorKind.Fish:
                {
                    Vector2 p = DensestPoint(d.Owner != null ? d.Owner.Pos : Vector2.zero, 12f);
                    SpawnPersistent(ActorKind.Fish, p, Vector2.zero, 1.6f, 0.7f, 0f, d.Owner, StatusMask.Execute);
                    break;
                }

                case ActorKind.LightningBolt:
                {
                    Vector2 p = ActiveCenter() + Random.insideUnitCircle * 18f;
                    var enemy = RandomEnemy(p);
                    if (enemy != null) p = enemy.Pos + Random.insideUnitCircle * 0.6f;
                    var bolt = SpawnTransient(ActorKind.LightningBolt, p, 1.4f, 0.35f);
                    bolt.Damage = 42f;
                    bolt.Owner = d.Owner;
                    break;
                }

                case ActorKind.Meteorite:
                {
                    Vector2 origin = d.UseCenter ? d.Center : (d.Owner != null ? d.Owner.Pos : Vector2.zero);
                    Vector2 p = origin + Random.insideUnitCircle * 11f;
                    p = GameConfig.ClampToMap(p, 1f);
                    var rock = SpawnTransient(ActorKind.Meteorite, p, 1.8f, 0.45f);
                    rock.Damage = d.Damage;
                    rock.Owner = d.Owner;
                    break;
                }
            }
        }

        void SpawnWave(int count)
        {
            int living = 0;
            for (int i = 0; i < _actors.Count; i++)
                if (!_actors[i].Dead && _actors[i].Kind == ActorKind.Enemy) living++;

            count = Mathf.Min(count, Mathf.Max(0, GameConfig.MaxEnemies - living));
            for (int i = 0; i < count; i++)
            {
                var e = Create(ActorKind.Enemy, RandomEdge(), 0.7f);
                e.Element = GameplayRules.BasicElements[Random.Range(0, GameplayRules.BasicElements.Length)];
                e.Size = e.Element == ElementId.Grass ? 1.05f : e.Element == ElementId.Electric ? .65f : .8f;
                e.Hp = e.MaxHp = GameConfig.EnemyHp(Elapsed.Value) * (e.Element == ElementId.Grass ? 1.6f : e.Element == ElementId.Electric ? .7f : 1f);
                TryMakeGiant(e);
            }
        }

        void SpawnBoss(int stage)
        {
            _bossStage = stage;
            float scale = stage / (float)GameConfig.BossEvery;
            var boss = Create(ActorKind.Boss, RandomEdge(), 2.4f + 0.25f * scale);
            boss.Hp = boss.MaxHp = GameConfig.BossHp(Elapsed.Value) * scale;
            BossAlive.Value = true;
            BossHpRatio.Value = 1f;
        }

        void OnBossDefeated(Vector2 pos)
        {
            int stage = _bossStage;
            BossAlive.Value = false;
            BossHpRatio.Value = 0f;
            _bossStage = 0;

            if (stage == 5)
            {
                SpecialUnlocked.Value = true;
                DropBossSpecial(pos);
            }
            if (stage == 10) SpecialPassive.Value = true;
            CompleteStage();
        }

        void DropBossSpecial(Vector2 pos)
        {
            var pool = new[] { SpecialId.Meteor, SpecialId.Hellfire, SpecialId.Spirit, SpecialId.DarkMatter };
            const int count = 2;
            for (int i = 0; i < count; i++)
            {
                var special = pool[Random.Range(0, pool.Length)];
                float side = count == 1 ? 0f : (i - (count - 1) * 0.5f) * 4.2f;
                var actor = Create(ElementRules.PickupKind(special), pos + new Vector2(side, 1.4f), 1.35f);
                actor.Life = 99999f;
            }
        }

        public SimActor SpawnPickup(ElementId element, Vector2 pos)
        {
            var a = Create(ElementRules.PickupKind(element), pos, 0.85f);
            a.Life = 99999f;
            return a;
        }

        public SimActor SpawnProjectile(ActorKind kind, Vector2 pos, Vector2 vel, float size, float life, float damage, OctopusPlayer owner, StatusMask status)
        {
            var a = Create(kind, pos, size);
            a.Vel = vel;
            a.Life = life;
            a.Damage = damage;
            a.Owner = owner;
            a.StatusOnHit = status;
            return a;
        }

        public SimActor SpawnPersistent(ActorKind kind, Vector2 pos, Vector2 vel, float size, float life, float damage, OctopusPlayer owner, StatusMask status)
        {
            var a = Create(kind, pos, size);
            a.Vel = vel;
            a.Life = life;
            a.Damage = damage;
            a.Owner = owner;
            a.StatusOnHit = status;
            return a;
        }

        public SimActor SpawnTransient(ActorKind kind, Vector2 pos, float size, float life)
        {
            var a = Create(kind, pos, size);
            // Vine actors only display a strike whose damage was already applied.
            // Give the plant time to read without slowing the .16-second damage pulses.
            a.Life = kind == ActorKind.Vine ? GrassThornVisual.DefaultLifetime : life;
            if (kind == ActorKind.Vine) a.Duration = a.Life;
            return a;
        }

        public void QueueFish(OctopusPlayer owner, int count)
        {
            _delayed.Add(new Delayed { Time = 0.05f, Interval = 0.5f, Kind = ActorKind.Fish, Owner = owner, Remaining = count });
        }

        public void QueueLightning(OctopusPlayer owner, int count)
        {
            _delayed.Add(new Delayed { Time = 0.05f, Interval = 0.38f, Kind = ActorKind.LightningBolt, Owner = owner, Remaining = count });
        }

        public void QueueMeteors(OctopusPlayer owner, int count, float damage)
        {
            _delayed.Add(new Delayed
            {
                Time = 0.05f, Interval = 0.13f, Kind = ActorKind.Meteorite,
                Owner = owner, Remaining = count, Damage = damage
            });
        }

        public void QueueMeteors(OctopusPlayer owner, int count, float damage, Vector2 center)
        {
            _delayed.Add(new Delayed
            {
                Time = 0.05f, Interval = 0.1f, Kind = ActorKind.Meteorite,
                Owner = owner, Remaining = count, Damage = damage, Center = center, UseCenter = true
            });
        }

        SimActor Create(ActorKind kind, Vector2 pos, float size)
        {
            bool objective = kind == ActorKind.Boss || kind == ActorKind.Statue || kind == ActorKind.ArtifactPiece || ElementRules.IsSpecialPickup(kind);
            if (_actors.Count >= GameConfig.MaxActors - (objective ? 0 : 8))
                return new SimActor { Dead = true };

            var a = new SimActor
            {
                Id = _nextId++,
                Kind = kind,
                Pos = GameConfig.ClampToMap(pos, .5f),
                Size = size,
                Life = 1f,
                Hp = 1f
            };
            _actors.Add(a);
            return a;
        }

        public bool DamageCircle(Vector2 pos, float radius, float damage, StatusMask status, float statusTime, float burnDps,
            bool reportKill = false, float statusChance = 1f, bool hitBoss = true)
        {
            bool killed = false;
            float r2 = radius * radius;
            for (int i = 0; i < _actors.Count; i++)
            {
                var e = _actors[i];
                if (e.Dead || (e.Kind != ActorKind.Enemy && e.Kind != ActorKind.Boss))
                    continue;
                if (!hitBoss && e.Kind == ActorKind.Boss)
                    continue;
                if ((e.Pos - pos).sqrMagnitude > r2)
                    continue;

                var mask = status;
                if (statusChance < 1f && Random.value > statusChance)
                    mask &= ~StatusMask.Stun;

                if (ApplyDamage(e, damage, mask, statusTime, burnDps))
                    killed = true;
            }

            return killed;
        }

        public void DamageCone(Vector2 pos, Vector2 dir, float radius, float angle, float damage, StatusMask status, float statusTime, float burnDps, HashSet<ushort> hitTargets = null)
        {
            dir.Normalize();
            float half = angle * 0.5f;
            for (int i = 0; i < _actors.Count; i++)
            {
                var e = _actors[i];
                if (e.Dead || (e.Kind != ActorKind.Enemy && e.Kind != ActorKind.Boss))
                    continue;
                Vector2 to = e.Pos - pos;
                if (to.sqrMagnitude > radius * radius)
                    continue;
                if (Vector2.Angle(dir, to) > half)
                    continue;
                hitTargets?.Add(e.Id);

                if ((status & StatusMask.Knockback) != 0 && e.Kind != ActorKind.Boss)
                    e.Knockback = to.normalized * 22f;

                ApplyDamage(e, damage, status, statusTime, burnDps);
            }
        }

        public void DamageLine(Vector2 a, Vector2 b, float width, float damage, StatusMask status, float statusTime, float stunChance)
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                var e = _actors[i];
                if (e.Dead || (e.Kind != ActorKind.Enemy && e.Kind != ActorKind.Boss))
                    continue;
                if (DistToSegment(e.Pos, a, b) > width + (e.Giant != null ? e.Size * .4f : GameConfig.EnemyRadius))
                    continue;
                var mask = status;
                if ((status & StatusMask.Stun) != 0 && Random.value > stunChance)
                    mask &= ~StatusMask.Stun;
                ApplyDamage(e, damage, mask, 0.7f, 0f);
            }
        }

        public void StrikeNearestVines(Vector2 from, float range, int count, float damage)
        {
            var list = TempEnemies(from, range);
            list.Sort((x, y) => (x.Pos - from).sqrMagnitude.CompareTo((y.Pos - from).sqrMagnitude));
            int n = Mathf.Min(count, list.Count);
            for (int i = 0; i < n; i++)
            {
                ApplyDamage(list[i], damage, StatusMask.None, 0f, 0f);
                SpawnTransient(ActorKind.Vine, list[i].Pos, 0.9f, 0.18f);
            }
        }

        public bool HitEnemy(SimActor projectile, float range, float damage, StatusMask status)
        {
            float r2 = range * range;
            for (int i = 0; i < _actors.Count; i++)
            {
                var e = _actors[i];
                if (e.Dead || (e.Kind != ActorKind.Enemy && e.Kind != ActorKind.Boss))
                    continue;
                if ((e.Pos - projectile.Pos).sqrMagnitude > r2)
                    continue;
                ApplyDamage(e, damage, status, 1.4f, 8f);
                return true;
            }

            return false;
        }

        public void TryMeleeArc(OctopusPlayer player, Vector2 origin, float range, float damage, Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.01f)
                dir = Vector2.right;
            dir.Normalize();
            var extra = player.MeleeStatus(out float burnDps, out float spicyChance);
            if ((extra & StatusMask.Spicy) != 0 && Random.value > spicyChance)
                extra &= ~StatusMask.Spicy;
            float dealt = damage * player.MeleeDamageMul();

            // 사진처럼 캐릭터 앞에 붙는 활꼴: 전방 원 + 앞쪽만 판정.
            Vector2 center = origin + dir * (range * 0.48f);
            float radius = range * 0.55f;
            float r2 = radius * radius;

            for (int i = 0; i < _actors.Count; i++)
            {
                var e = _actors[i];
                if (e.Dead || (e.Kind != ActorKind.Enemy && e.Kind != ActorKind.Boss))
                    continue;
                if ((e.Pos - center).sqrMagnitude > r2)
                    continue;
                if (Vector2.Dot(e.Pos - origin, dir) < 0f)
                    continue;

                ApplyDamage(e, dealt, extra, 1.5f, burnDps);
                if ((extra & StatusMask.Spicy) != 0) ApplySpicy(e, player.ElementLevel);
            }
        }

        public void StrikeWhip(OctopusPlayer player, List<Vector2> points, float width, float damage, HashSet<ushort> alreadyHit)
        {
            if (player == null || points == null || points.Count == 0)
                return;
            var extra = player.MeleeStatus(out float burnDps, out float spicyChance);
            float dealt = damage * player.MeleeDamageMul();
            for (int i = 0; i < _actors.Count; i++)
            {
                var e = _actors[i];
                if (e.Dead || (e.Kind != ActorKind.Enemy && e.Kind != ActorKind.Boss))
                    continue;
                if (alreadyHit != null && alreadyHit.Contains(e.Id))
                    continue;
                float reach = width + (e.Kind == ActorKind.Boss || e.Giant != null ? e.Size * 0.35f : GameConfig.EnemyRadius);
                float reach2 = reach * reach;
                bool touched = false;
                for (int p = 0; p < points.Count; p++)
                {
                    if ((e.Pos - points[p]).sqrMagnitude <= reach2)
                    {
                        touched = true;
                        break;
                    }
                }

                if (!touched)
                    continue;
                if (alreadyHit != null)
                    alreadyHit.Add(e.Id);
                var mask = extra;
                if ((mask & StatusMask.Spicy) != 0 && Random.value > spicyChance)
                    mask &= ~StatusMask.Spicy;
                ApplyDamage(e, dealt, mask, 1.5f, burnDps);
                if ((mask & StatusMask.Spicy) != 0)
                    ApplySpicy(e, player.ElementLevel);
            }
        }

        public SimActor NearestEnemy(Vector2 pos, float range)
        {
            SimActor best = null;
            float bestD = range * range;
            for (int i = 0; i < _actors.Count; i++)
            {
                var e = _actors[i];
                if (e.Dead || (e.Kind != ActorKind.Enemy && e.Kind != ActorKind.Boss))
                    continue;
                float d = (e.Pos - pos).sqrMagnitude;
                if (d < bestD)
                {
                    best = e;
                    bestD = d;
                }
            }

            return best;
        }

        public SimActor NearestPickup(Vector2 pos, float range, OctopusPlayer player = null)
        {
            SimActor best = null;
            float bestD = range * range;
            for (int i = 0; i < _actors.Count; i++)
            {
                var a = _actors[i];
                if (a.Dead || (!ElementRules.IsAttributePickup(a.Kind) && !ElementRules.IsSpecialPickup(a.Kind)))
                    continue;
                if (player != null && (ElementRules.IsAttributePickup(a.Kind)
                    ? player.HasElement(ElementRules.FromPickup(a.Kind))
                    : player.Special.Value != 0 || !SpecialUnlocked.Value)) continue;
                float d = (a.Pos - pos).sqrMagnitude;
                if (d < bestD)
                {
                    best = a;
                    bestD = d;
                }
            }

            return best;
        }

        public void CollectPickup(SimActor pickup)
        {
            if (IsServer && pickup != null && !pickup.Dead) pickup.Dead = true;
        }

        public Vector2 DensestPoint(Vector2 around, float search)
        {
            SimActor best = null;
            int bestN = -1;
            for (int i = 0; i < _actors.Count; i++)
            {
                var e = _actors[i];
                if (e.Dead || (e.Kind != ActorKind.Enemy && e.Kind != ActorKind.Boss))
                    continue;
                if ((e.Pos - around).sqrMagnitude > search * search)
                    continue;
                int n = 0;
                for (int j = 0; j < _actors.Count; j++)
                {
                    var o = _actors[j];
                    if (o.Dead || (o.Kind != ActorKind.Enemy && o.Kind != ActorKind.Boss))
                        continue;
                    if ((o.Pos - e.Pos).sqrMagnitude < 9f)
                        n++;
                }

                if (n > bestN)
                {
                    bestN = n;
                    best = e;
                }
            }

            return best != null ? best.Pos : around + Random.insideUnitCircle * 2f;
        }

        public SimActor RandomEnemy(Vector2 from)
        {
            SimActor best = null;
            int n = 0;
            for (int i = 0; i < _actors.Count; i++)
            {
                var e = _actors[i];
                if (e.Dead || (e.Kind != ActorKind.Enemy && e.Kind != ActorKind.Boss))
                    continue;
                n++;
                if (Random.Range(0, n) == 0)
                    best = e;
            }

            return best;
        }

        void KillInCircle(Vector2 pos, float radius)
        {
            float r2 = radius * radius;
            for (int i = 0; i < _actors.Count; i++)
            {
                var e = _actors[i];
                if (e.Dead || e.Kind != ActorKind.Enemy)
                    continue;
                if ((e.Pos - pos).sqrMagnitude <= r2)
                    KillEnemy(e);
            }
        }

        bool ApplyDamage(SimActor enemy, float damage, StatusMask status, float statusTime, float burnDps)
        {
            if (!CombatActive || enemy.Dead || (enemy.Kind != ActorKind.Enemy && enemy.Kind != ActorKind.Boss))
                return false;

            if (SpecialPassive.Value)
                damage *= GameConfig.SpecialPassiveMul;

            if (enemy.Giant != null && enemy.Giant.attackPattern == GiantPattern.Urchin && enemy.GiantState == GiantState.Attack)
            {
                damage *= enemy.Giant.defenseDamageMultiplier;
                if (damage > 0 && enemy.CounterTime <= 0 && enemy.StunTime <= 0)
                { enemy.CounterTime = enemy.Giant.counterCooldown; GiantSpikes(enemy, true); }
            }

            if ((status & StatusMask.Execute) != 0 && enemy.Kind != ActorKind.Boss)
                enemy.Hp = 0f;
            else
                enemy.Hp -= damage;

            if ((status & StatusMask.Burn) != 0)
            {
                enemy.BurnTime = Mathf.Max(enemy.BurnTime, statusTime);
                enemy.BurnDps = Mathf.Max(enemy.BurnDps, burnDps);
            }

            if ((status & StatusMask.Slow) != 0)
                enemy.SlowTime = Mathf.Max(enemy.SlowTime, statusTime);
            if ((status & StatusMask.Stun) != 0 && enemy.Kind != ActorKind.Boss)
                enemy.StunTime = Mathf.Max(enemy.StunTime, statusTime * (enemy.Giant != null ? .5f : 1f));
            if ((status & StatusMask.Spicy) != 0 && enemy.Kind != ActorKind.Boss)
            {
                enemy.SpicyTime = Mathf.Max(enemy.SpicyTime, statusTime);
                enemy.SpicyDir = Random.insideUnitCircle.normalized;
            }

            Vector2 from = enemy.Pos;
            var attacker = NearestLivingPlayer(enemy.Pos);
            if (attacker != null)
                from = attacker.Pos;
            Vector2 away = enemy.Pos - from;
            if (away.sqrMagnitude < 0.04f)
                away = Vector2.up;
            float kick = enemy.Kind == ActorKind.Boss ? GameConfig.BossKnockback : GameConfig.Knockback;
            enemy.Knockback = away.normalized * kick * (enemy.Giant != null ? enemy.Giant.knockbackMultiplier : 1f);
            enemy.HitFlash = GameConfig.HitFlashSeconds;

            if (enemy.Hp <= 0f)
            {
                KillEnemy(enemy);
                return true;
            }

            return false;
        }

        void KillEnemy(SimActor enemy)
        {
            if (enemy.Dead)
                return;
            enemy.Dead = true;
            enemy.GiantState = GiantState.Dead;

            bool boss = enemy.Kind == ActorKind.Boss;
            if (boss)
                OnBossDefeated(enemy.Pos);
            else
            {
                TryDropAttribute(enemy.Pos, enemy.Element);
                if (Stage.Value % GameConfig.BossEvery != 0 && Random.value < GameConfig.ArtifactDropChance)
                    SpawnMarker(ActorKind.ArtifactPiece, enemy.Pos, 0.42f);
                if (Random.value < GameConfig.PotionDropChance)
                    SpawnMarker(ActorKind.HealthPotion, enemy.Pos, 0.36f);
            }
        }

        public bool AttributeNeeded(ElementId element)
        {
            foreach (var player in OctopusPlayer.All)
                if (player != null && player.IsSpawned && !player.HasElement(element)) return true;
            return false;
        }
        void TryDropAttribute(Vector2 pos, ElementId element)
        {
            if (element != ElementId.None && AttributeNeeded(element) && Random.value < GameplayRules.Data.attributeDropChance)
                SpawnPickup(element, pos);
        }

        void StartArtifactStage(int stage)
        {
            fireCacheUntil = -1f;
            ClearLiveBosses();
            ClearFieldForNextStagePreservingGiants(stage % GameConfig.BossEvery == 0);
            _actors.RemoveAll(actor => actor.Dead);
            for (int i = 0; i < OctopusPlayer.All.Count; i++)
            {
                var player = OctopusPlayer.All[i];
                if (player != null && player.IsSpawned)
                {
                    player.ClearArtifact();
                    player.StopCombat();
                    player.PendingPicks.Value = 0;
                    player.Offer0.Value = player.Offer1.Value = player.Offer2.Value = 0;
                    player.StageChoiceComplete.Value = true;
                }
            }

            Stage.Value = Mathf.Max(1, stage);
            ArtifactDelivered.Value = 0;
            StageFlow.Value = (byte)StageState.Combat;
            NextStageCountdown.Value = 0f;
            StatueActivated.Value = false;
            _waveTimer = 0f;
            SpawnMarker(ActorKind.Statue, Vector2.zero, 1.55f);
            if (Stage.Value % GameConfig.BossEvery == 0)
                SpawnBoss(Stage.Value);
        }

        void ClearFieldForNextStage() => ClearFieldForNextStagePreservingGiants(false);

        void ClearFieldForNextStagePreservingGiants(bool preserveGiants)
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                var actor = _actors[i];
                if (actor.Dead || (preserveGiants && actor.Giant != null))
                    continue;
                actor.Dead = true;
            }

            _delayed.Clear();
        }

        void TickPotions()
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                var potion = _actors[i];
                if (potion.Dead || potion.Kind != ActorKind.HealthPotion)
                    continue;

                float reach = GameConfig.PlayerRadius + potion.Size;
                float reach2 = reach * reach;
                for (int p = 0; p < OctopusPlayer.All.Count; p++)
                {
                    var player = OctopusPlayer.All[p];
                    if (player == null || !player.IsSpawned || player.Downed.Value)
                        continue;
                    if ((player.Pos - potion.Pos).sqrMagnitude > reach2)
                        continue;
                    player.Heal(GameConfig.PotionHeal);
                    potion.Dead = true;
                    break;
                }
            }
        }

        void TickBossPattern(SimActor boss, float dt, float speed)
        {
            if (_bossStage == 5)
                TickChargeBoss(boss, dt, speed);
            else if (_bossStage == 10)
                TickBulletBoss(boss, dt, speed);
            else if (_bossStage == 15)
                TickAreaBoss(boss, dt, speed);
            else
                ChaseBoss(boss, dt, speed);

            boss.Pos = GameConfig.ClampToMap(boss.Pos, GameConfig.EnemyRadius);
            boss.ContactCooldown -= dt;
            BossHpRatio.Value = Mathf.Clamp01(boss.Hp / Mathf.Max(1f, boss.MaxHp));
        }

        void ChaseBoss(SimActor boss, float dt, float speed)
        {
            var target = NearestLivingPlayer(boss.Pos);
            if (target == null)
                return;
            Vector2 dir = target.Pos - boss.Pos;
            if (dir.sqrMagnitude > 0.01f)
                boss.Pos += dir.normalized * speed * dt;
        }

        void TickChargeBoss(SimActor boss, float dt, float speed)
        {
            if (boss.SkillPhase == 0)
            {
                var target = NearestLivingPlayer(boss.Pos);
                Vector2 delta = target != null ? target.Pos - boss.Pos : Vector2.zero;
                if (target == null || delta.sqrMagnitude < 0.04f)
                {
                    ChaseBoss(boss, dt, speed);
                    return;
                }

                Vector2 end = GameConfig.ClampToMap(boss.Pos + delta, GameConfig.EnemyRadius);
                boss.SkillPoint = end;
                boss.SkillPhase = 1;
                boss.SkillTime = GameConfig.BossChargeWindup;
                var lane = SpawnTransient(ActorKind.BossLane, (boss.Pos + end) * 0.5f, Vector2.Distance(boss.Pos, end), GameConfig.BossChargeWindup + 0.25f);
                lane.Vel = end - boss.Pos;
                return;
            }

            if (boss.SkillPhase == 1)
            {
                boss.SkillTime -= dt;
                if (boss.SkillTime > 0f)
                    return;
                boss.SkillPhase = 2;
                float travel = Mathf.Max(0.05f, GameConfig.BossChargeTravelSeconds);
                boss.SkillTime = travel;
                boss.Vel = (boss.SkillPoint - boss.Pos) / travel;
                return;
            }

            Vector2 prev = boss.Pos;
            boss.SkillTime -= dt;
            boss.Pos += boss.Vel * dt;
            DamageChargeSegment(boss, prev, boss.Pos);
            if (boss.SkillTime <= 0f)
            {
                boss.Pos = boss.SkillPoint;
                boss.Vel = Vector2.zero;
                boss.SkillPhase = 0;
            }
        }

        void DamageChargeSegment(SimActor boss, Vector2 from, Vector2 to)
        {
            if (boss.ContactCooldown > 0f)
                return;

            float reach = GameConfig.PlayerRadius + boss.Size * 0.5f;
            for (int i = 0; i < OctopusPlayer.All.Count; i++)
            {
                var player = OctopusPlayer.All[i];
                if (player == null || !player.IsSpawned || player.Downed.Value)
                    continue;
                if (DistToSegment(player.Pos, from, to) > reach)
                    continue;
                boss.ContactCooldown = 0.45f;
                player.ApplyDamage(GameConfig.EnemyContactDamage * 3f, DamageSource.BossSkill);
                return;
            }
        }

        void TickBulletBoss(SimActor boss, float dt, float speed)
        {
            if (boss.SkillPhase == 0)
            {
                boss.SkillPhase = 1;
                boss.SkillTime = GameConfig.BossBulletInterval;
            }

            ChaseBoss(boss, dt, speed);
            boss.SkillTime -= dt;
            if (boss.SkillTime > 0f)
                return;

            boss.SkillTime = GameConfig.BossBulletInterval;
            Vector2 dir = Random.insideUnitCircle;
            if (dir.sqrMagnitude < 0.01f)
                dir = Vector2.right;
            SpawnProjectile(ActorKind.BossBullet, boss.Pos, dir.normalized * GameConfig.BossBulletSpeed, 0.28f,
                GameConfig.BossBulletLife, GameConfig.EnemyContactDamage * 3f, null, StatusMask.None);
        }

        void TickAreaBoss(SimActor boss, float dt, float speed)
        {
            if (boss.SkillPhase == 0)
            {
                boss.SkillPhase = 1;
                boss.SkillTime = GameConfig.BossAreaGaugeSeconds;
                RemoveActors(ActorKind.BossArea);
                RemoveActors(ActorKind.BossAreaFill);
                SpawnTransient(ActorKind.BossArea, boss.Pos, GameConfig.BossAreaRadius, GameConfig.BossAreaGaugeSeconds + 0.25f);
                SpawnTransient(ActorKind.BossAreaFill, boss.Pos, 0.05f, GameConfig.BossAreaGaugeSeconds + 0.25f);
                return;
            }

            if (boss.SkillPhase == 1)
            {
                boss.SkillTime -= dt;
                float gauge = Mathf.Max(0.05f, GameConfig.BossAreaGaugeSeconds);
                float filled = GameConfig.BossAreaRadius * Mathf.Clamp01(1f - boss.SkillTime / gauge);
                HoldBossArea(boss.Pos, Mathf.Max(0.05f, filled));
                if (boss.SkillTime > 0f)
                    return;

                DamagePlayersInCircle(boss.Pos, GameConfig.BossAreaRadius, 30f);
                RemoveActors(ActorKind.BossArea);
                RemoveActors(ActorKind.BossAreaFill);
                boss.SkillPhase = 2;
                boss.SkillTime = GameConfig.BossAreaGap;
                return;
            }

            ChaseBoss(boss, dt, speed);
            boss.SkillTime -= dt;
            if (boss.SkillTime <= 0f)
                boss.SkillPhase = 0;
        }

        void HoldBossArea(Vector2 pos, float fillRadius)
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                var actor = _actors[i];
                if (actor.Dead)
                    continue;
                if (actor.Kind == ActorKind.BossArea)
                    actor.Pos = pos;
                else if (actor.Kind == ActorKind.BossAreaFill)
                {
                    actor.Pos = pos;
                    actor.Size = fillRadius;
                }
            }
        }

        void DamagePlayersInCircle(Vector2 pos, float radius, float damage)
        {
            float r2 = radius * radius;
            for (int i = 0; i < OctopusPlayer.All.Count; i++)
            {
                var player = OctopusPlayer.All[i];
                if (player == null || !player.IsSpawned || player.Downed.Value)
                    continue;
                if ((player.Pos - pos).sqrMagnitude > r2)
                    continue;
                player.ApplyDamage(damage, DamageSource.BossSkill);
            }
        }

        void TickBossBullet(SimActor bullet, float dt)
        {
            bullet.Pos += bullet.Vel * dt;
            bullet.Life -= dt;
            float reach = GameConfig.PlayerRadius + bullet.Size;
            float reach2 = reach * reach;
            for (int i = 0; i < OctopusPlayer.All.Count; i++)
            {
                var player = OctopusPlayer.All[i];
                if (player == null || !player.IsSpawned || player.Downed.Value)
                    continue;
                if ((player.Pos - bullet.Pos).sqrMagnitude > reach2)
                    continue;
                player.ApplyDamage(bullet.Damage, DamageSource.BossSkill);
                bullet.Dead = true;
                return;
            }

            if (bullet.Life <= 0f)
                bullet.Dead = true;
        }

        void TickAllySpirit(SimActor spirit, float dt)
        {
            spirit.Life -= dt;
            spirit.ContactCooldown -= dt;
            SimActor target = null;
            float best = float.MaxValue;
            for (int i = 0; i < _actors.Count; i++)
            {
                var enemy = _actors[i];
                if (!CombatActive || enemy.Dead || (enemy.Kind != ActorKind.Enemy && enemy.Kind != ActorKind.Boss))
                    continue;
                float dist = (enemy.Pos - spirit.Pos).sqrMagnitude;
                if (dist >= best)
                    continue;
                best = dist;
                target = enemy;
            }

            if (target != null)
            {
                Vector2 to = target.Pos - spirit.Pos;
                if (to.sqrMagnitude > 0.01f)
                    spirit.Pos += to.normalized * GameConfig.EnemySpeed(Elapsed.Value) * dt;
                float reach = spirit.Size + (target.Kind == ActorKind.Boss || target.Giant != null ? target.Size * 0.5f : GameConfig.EnemyRadius);
                if ((target.Pos - spirit.Pos).sqrMagnitude <= reach * reach && spirit.ContactCooldown <= 0f)
                {
                    ApplyDamage(target, spirit.Damage, StatusMask.Stun | StatusMask.Knockback, 1.6f, 0f);
                    spirit.ContactCooldown = GameConfig.TeamSpiritHitInterval;
                }
            }

            if (spirit.Life <= 0f)
                spirit.Dead = true;
        }

        void TickPiercingMeteor(SimActor meteor, float dt)
        {
            meteor.Pos += meteor.Vel * dt;
            meteor.Life -= dt;
            float reach2 = meteor.Size * meteor.Size;
            for (int i = 0; i < _actors.Count; i++)
            {
                var enemy = _actors[i];
                if (enemy.Dead || enemy.HitBy == meteor.Id)
                    continue;
                if (enemy.Kind != ActorKind.Enemy && enemy.Kind != ActorKind.Boss)
                    continue;
                if ((enemy.Pos - meteor.Pos).sqrMagnitude > reach2)
                    continue;
                enemy.HitBy = meteor.Id;
                ApplyDamage(enemy, meteor.Damage, StatusMask.None, 0f, 0f);
            }

            if (meteor.Life <= 0f)
                meteor.Dead = true;
        }

        public void SpawnAllySpirit(Vector2 pos, float damage, float life)
        {
            var spirit = Create(ActorKind.AllySpirit, pos, 0.45f);
            if (spirit.Dead)
                return;
            spirit.Life = life;
            spirit.Damage = damage;
        }

        public void SpawnPiercingMeteor(Vector2 pos, Vector2 dir, float damage)
        {
            var meteor = Create(ActorKind.PiercingMeteor, pos, 2.1f);
            if (meteor.Dead)
                return;
            meteor.Vel = dir.normalized * GameConfig.PiercingMeteorSpeed;
            meteor.Damage = damage;
            meteor.Life = GameConfig.MapSize / Mathf.Max(0.1f, GameConfig.PiercingMeteorSpeed);
        }

        public void DamageAll(float damage)
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                var enemy = _actors[i];
                if (!CombatActive || enemy.Dead || (enemy.Kind != ActorKind.Enemy && enemy.Kind != ActorKind.Boss))
                    continue;
                ApplyDamage(enemy, damage, StatusMask.None, 0f, 0f);
            }
        }

        public void CastBlackFlame(Vector2 center, float radius, float spread, float damage, float burnTime, float burnDps)
        {
            int gen = ++_flameGen;
            var open = new List<SimActor>(16);
            float radius2 = radius * radius;
            float spread2 = spread * spread;
            for (int i = 0; i < _actors.Count; i++)
            {
                var enemy = _actors[i];
                if (!CombatActive || enemy.Dead || (enemy.Kind != ActorKind.Enemy && enemy.Kind != ActorKind.Boss))
                    continue;
                if ((enemy.Pos - center).sqrMagnitude > radius2)
                    continue;
                InfectFlame(enemy, gen, damage, burnTime, burnDps, open);
            }

            int guard = 0;
            while (open.Count > 0 && guard++ < 512)
            {
                var source = open[0];
                open.RemoveAt(0);
                for (int i = 0; i < _actors.Count; i++)
                {
                    var enemy = _actors[i];
                    if (enemy.Dead || enemy.Mark == gen)
                        continue;
                    if (enemy.Kind != ActorKind.Enemy && enemy.Kind != ActorKind.Boss)
                        continue;
                    if ((enemy.Pos - source.Pos).sqrMagnitude > spread2)
                        continue;
                    InfectFlame(enemy, gen, damage, burnTime, burnDps, open);
                }
            }
        }

        void InfectFlame(SimActor enemy, int gen, float damage, float burnTime, float burnDps, List<SimActor> open)
        {
            enemy.Mark = gen;
            enemy.BlackFlame = true;
            ApplyDamage(enemy, damage, StatusMask.Burn, burnTime, burnDps);
            open.Add(enemy);
        }

        void TickArtifacts()
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                var piece = _actors[i];
                if (piece.Dead || piece.Kind != ActorKind.ArtifactPiece)
                    continue;

                OctopusPlayer taker = null;
                float best = GameConfig.ArtifactPickupRange * GameConfig.ArtifactPickupRange;
                for (int p = 0; p < OctopusPlayer.All.Count; p++)
                {
                    var player = OctopusPlayer.All[p];
                    if (player == null || !player.IsSpawned || player.Downed.Value || player.HasArtifact.Value)
                        continue;
                    float d = (player.Pos - piece.Pos).sqrMagnitude;
                    if (d > best)
                        continue;
                    best = d;
                    taker = player;
                }

                if (taker == null)
                    continue;
                taker.GiveArtifact();
                piece.Dead = true;
            }

            for (int p = 0; p < OctopusPlayer.All.Count; p++)
            {
                var player = OctopusPlayer.All[p];
                if (player == null || !player.IsSpawned || player.Downed.Value || !player.HasArtifact.Value)
                    continue;
                if (!NearKind(player.Pos, ActorKind.Statue, GameConfig.StatueDeliverRange))
                    continue;
                player.ClearArtifact();
                ArtifactDelivered.Value++;
                if (Stage.Value % GameConfig.BossEvery != 0 && ArtifactDelivered.Value >= GameConfig.RequiredPieces(Stage.Value))
                { CompleteStage(); return; }
            }

            if (Stage.Value % GameConfig.BossEvery != 0 &&
                ArtifactDelivered.Value >= GameConfig.RequiredPieces(Stage.Value)) CompleteStage();
        }

        void SpawnMarker(ActorKind kind, Vector2 pos, float size)
        {
            var actor = Create(kind, pos, size);
            actor.Life = 99999f;
        }

        void RemoveActors(ActorKind kind)
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                if (!_actors[i].Dead && _actors[i].Kind == kind)
                    _actors[i].Dead = true;
            }
        }

        bool HasLiving(ActorKind kind)
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                if (!_actors[i].Dead && _actors[i].Kind == kind)
                    return true;
            }

            return false;
        }

        bool NearKind(Vector2 pos, ActorKind kind, float range)
        {
            float r2 = range * range;
            for (int i = 0; i < _actors.Count; i++)
            {
                var a = _actors[i];
                if (a.Dead || a.Kind != kind)
                    continue;
                if ((a.Pos - pos).sqrMagnitude <= r2)
                    return true;
            }

            return false;
        }

        Vector2 RandomInterior(float avoidPlayers)
        {
            Vector2 center = ActiveCenter();
            float avoid2 = avoidPlayers * avoidPlayers;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                var candidate = GameConfig.ClampToMap(center + Random.insideUnitCircle.normalized * Random.Range(8f, 24f), 1f);

                bool blocked = false;
                for (int i = 0; i < OctopusPlayer.All.Count && !blocked; i++)
                {
                    var player = OctopusPlayer.All[i];
                    if (player == null || !player.IsSpawned)
                        continue;
                    if ((player.Pos - candidate).sqrMagnitude < avoid2)
                        blocked = true;
                }

                if (!blocked)
                    return candidate;
            }

            return GameConfig.ClampToMap(center + new Vector2(12f, 8f), 1f);
        }

        Vector2 ActiveCenter()
        {
            Vector2 sum = Vector2.zero;
            int count = 0;
            for (int i = 0; i < OctopusPlayer.All.Count; i++)
            {
                var player = OctopusPlayer.All[i];
                if (player == null || !player.IsSpawned) continue;
                sum += player.Pos; count++;
            }
            return count > 0 ? sum / count : Vector2.zero;
        }

        OctopusPlayer NearestLivingPlayer(Vector2 pos)
        {
            OctopusPlayer best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < OctopusPlayer.All.Count; i++)
            {
                var p = OctopusPlayer.All[i];
                if (p == null || !p.IsSpawned || p.Downed.Value)
                    continue;
                float d = (p.Pos - pos).sqrMagnitude;
                if (d < bestD)
                {
                    best = p;
                    bestD = d;
                }
            }

            return best;
        }

        List<SimActor> TempEnemies(Vector2 from, float range)
        {
            var list = new List<SimActor>(16);
            float r2 = range * range;
            for (int i = 0; i < _actors.Count; i++)
            {
                var e = _actors[i];
                if (e.Dead || (e.Kind != ActorKind.Enemy && e.Kind != ActorKind.Boss))
                    continue;
                if ((e.Pos - from).sqrMagnitude <= r2)
                    list.Add(e);
            }

            return list;
        }

        Vector2 RandomEdge()
        {
            Vector2 center = ActiveCenter();
            float angle = Random.value * Mathf.PI * 2f;
            float distance = Random.Range(GameConfig.CameraSize + 3f, GameConfig.CameraSize + 7f);
            return GameConfig.ClampToMap(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance, GameConfig.EnemyRadius);
        }

        static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude);
            t = Mathf.Clamp01(t);
            return Vector2.Distance(p, a + ab * t);
        }

        void CheckGameOver()
        {
            int n = 0;
            bool allDown = true;
            for (int i = 0; i < OctopusPlayer.All.Count; i++)
            {
                var p = OctopusPlayer.All[i];
                if (p == null || !p.IsSpawned)
                    continue;
                n++;
                if (!p.Downed.Value)
                    allDown = false;
            }

            if (n > 0 && allDown)
                GameOver.Value = true;
        }

        void EnsureView()
        {
            if (_view == null)
            {
                var go = new GameObject("WorldView");
                _view = go.AddComponent<WorldView>();
            }
        }

        void RenderLocal()
        {
            EnsureView();
            _view.BeginFrame();
            for (int i = 0; i < _actors.Count; i++)
            {
                var a = _actors[i];
                if (a.Dead)
                    continue;
                _view.Show(a.Id, a.Kind, a.Pos, a.Size, PackFlags(a), a.Age, VisualDuration(a), PackHp(a));
            }

            _view.EndFrame();
        }

        void BroadcastActors()
        {
            if (NetworkManager == null || NetworkManager.ConnectedClientsIds.Count <= 1)
                return;

            int living = 0;
            for (int i = 0; i < _actors.Count; i++)
            {
                if (!_actors[i].Dead)
                    living++;
            }

            int chunkSize = GameConfig.ActorSyncChunk;
            int chunks = Mathf.Max(1, (living + chunkSize - 1) / chunkSize);
            _syncFrame++;
            int sent = 0;
            int actorIndex = 0;

            for (int chunk = 0; chunk < chunks; chunk++)
            {
                int remaining = living - sent;
                int n = Mathf.Clamp(remaining, 0, chunkSize);
                int bytes = 8 + n * GameConfig.ActorBytes;
                using var writer = new FastBufferWriter(Mathf.Max(32, bytes), Allocator.Temp);
                writer.WriteValueSafe(_syncFrame);
                writer.WriteValueSafe((byte)chunk);
                writer.WriteValueSafe((byte)chunks);
                writer.WriteValueSafe((ushort)n);

                int written = 0;
                while (written < n && actorIndex < _actors.Count)
                {
                    var a = _actors[actorIndex++];
                    if (a.Dead)
                        continue;
                    writer.WriteValueSafe(a.Id);
                    writer.WriteValueSafe((byte)a.Kind);
                    writer.WriteValueSafe(PackFlags(a));
                    writer.WriteValueSafe(a.Pos.x);
                    writer.WriteValueSafe(a.Pos.y);
                    writer.WriteValueSafe(a.Size);
                    writer.WriteValueSafe(a.Age);
                    writer.WriteValueSafe(VisualDuration(a));
                    writer.WriteValueSafe(PackHp(a));
                    written++;
                }

                sent += written;
                SendActorChunk(writer);
            }
        }

        void SendActorChunk(FastBufferWriter writer)
        {
            var ids = NetworkManager.ConnectedClientsIds;
            for (int i = 0; i < ids.Count; i++)
            {
                ulong id = ids[i];
                if (id == NetworkManager.ServerClientId)
                    continue;
                NetworkManager.CustomMessagingManager.SendNamedMessage(
                    NetChannels.Actors, id, writer, NetworkDelivery.Unreliable);
            }
        }

        void OnActorsMessage(ulong sender, FastBufferReader reader)
        {
            if (IsServer)
                return;
            EnsureView();
            reader.ReadValueSafe(out ushort frame);
            reader.ReadValueSafe(out byte chunk);
            reader.ReadValueSafe(out byte chunks);
            reader.ReadValueSafe(out ushort count);

            if (frame != _viewFrame)
            {
                _view.BeginFrame();
                _viewFrame = frame;
                _viewChunksGot = 0;
                _viewChunksNeed = chunks;
            }

            for (int i = 0; i < count; i++)
            {
                reader.ReadValueSafe(out ushort id);
                reader.ReadValueSafe(out byte kind);
                reader.ReadValueSafe(out byte extra);
                reader.ReadValueSafe(out float x);
                reader.ReadValueSafe(out float y);
                reader.ReadValueSafe(out float size);
                reader.ReadValueSafe(out float age);
                reader.ReadValueSafe(out float duration);
                reader.ReadValueSafe(out byte hp);
                _view.Show(id, (ActorKind)kind, new Vector2(x, y), size, extra, age, duration, hp);
            }

            _viewChunksGot++;
            if (_viewChunksGot >= _viewChunksNeed)
                _view.EndFrame();
        }

        // Enemy snapshots use the otherwise unused duration channel for their visual element.
        static float VisualDuration(SimActor a) => a.Kind == ActorKind.Enemy
            ? (a.Giant != null ? 1000 + a.Giant.id * 100 + (int)a.GiantState * 10 + Mathf.Repeat(Mathf.Atan2(a.SkillPoint.y,a.SkillPoint.x)*Mathf.Rad2Deg,360f)/360f : (float)a.Element) : a.Duration;

        static byte PackFlags(SimActor a)
        {
            if (a.Kind == ActorKind.WaterBalloonShot)
                return (byte)Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(a.Vel.y, a.Vel.x) * Mathf.Rad2Deg, 360f) / 360f * 255f);
            if ((a.Kind == ActorKind.GiantSpike || a.Kind == ActorKind.Shockwave || a.Kind == ActorKind.MeleeSlash || a.Kind == ActorKind.BossLane || ElementEffectVisual.IsDirectional(a.Kind)) && a.Vel.sqrMagnitude > 0.0001f)
            {
                float deg = Mathf.Atan2(a.Vel.y, a.Vel.x) * Mathf.Rad2Deg;
                if (deg < 0f) deg += 360f;
                return (byte)Mathf.RoundToInt(deg / 360f * 255f);
            }

            byte f = 0;
            if (a.Kind == ActorKind.Enemy || a.Kind == ActorKind.Boss)
            {
                if (a.BlackFlame && a.BurnTime > 0f) f |= (byte)EnemyFlag.BlackFlame;
                if (a.BurnTime > 0f) f |= (byte)EnemyFlag.Burning;
                if (a.SlowTime > 0f) f |= (byte)EnemyFlag.Slowed;
                if (a.StunTime > 0f) f |= (byte)EnemyFlag.Stunned;
                if (a.SpicyTime > 0f) f |= (byte)EnemyFlag.Spicy;
                if (a.Kind == ActorKind.Boss) f |= (byte)EnemyFlag.Boss;
                if (a.HitFlash > 0f) f |= (byte)EnemyFlag.Hit;
            }

            return f;
        }

        static byte PackHp(SimActor a)
        {
            if (a.MaxHp <= 0.01f || (a.Kind != ActorKind.Enemy && a.Kind != ActorKind.Boss))
                return 255;
            return (byte)Mathf.Clamp(Mathf.RoundToInt(a.Hp / a.MaxHp * 255f), 0, 255);
        }
    }
}


