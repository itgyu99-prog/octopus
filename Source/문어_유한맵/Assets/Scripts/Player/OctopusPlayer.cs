using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace OctopusSurvivors
{
    public sealed partial class OctopusPlayer : NetworkBehaviour
    {
        public static readonly List<OctopusPlayer> All = new List<OctopusPlayer>();
        public static OctopusPlayer Local { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            All.Clear();
            Local = null;
        }

        public readonly NetworkVariable<float> Hp = new NetworkVariable<float>(GameConfig.BaseMaxHp);
        public readonly NetworkVariable<float> MaxHp = new NetworkVariable<float>(GameConfig.BaseMaxHp);
        public readonly NetworkVariable<bool> Downed = new NetworkVariable<bool>(false);
        public readonly NetworkVariable<float> ReviveProgress = new NetworkVariable<float>(0f);
        public readonly NetworkVariable<bool> InSteam = new NetworkVariable<bool>(false);
        public readonly NetworkVariable<bool> Charging = new NetworkVariable<bool>(false);
        public readonly NetworkVariable<bool> Invincible = new NetworkVariable<bool>(false);
        public readonly NetworkVariable<Vector2> NetPos = new NetworkVariable<Vector2>(Vector2.zero);
        public readonly NetworkVariable<Vector2> Facing = new NetworkVariable<Vector2>(Vector2.right);
        public readonly NetworkVariable<float> AttackLeft = new NetworkVariable<float>(0f);
        public readonly NetworkVariable<Vector2> MoveInput = new NetworkVariable<Vector2>(
            Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public readonly NetworkVariable<byte> Slot0 = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> Slot1 = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> Slot2 = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> Slot3 = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> Combo1 = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> Combo2 = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> Combo = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> ActiveSlot = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<bool> ComboWindow = new NetworkVariable<bool>(false);

        public readonly NetworkVariable<byte> Special = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<bool> Transformed = new NetworkVariable<bool>(false);
        public readonly NetworkVariable<float> SpecialCd = new NetworkVariable<float>(0f);

        public readonly NetworkVariable<float> HopZ = new NetworkVariable<float>(0f);
        public readonly NetworkVariable<byte> Palette = new NetworkVariable<byte>(0);

        public readonly NetworkVariable<byte> UpAttack = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> UpRange = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> UpMoveSpeed = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> UpMaxHp = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> UpAttrRank = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> UpElemRank = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> UpAttrCd = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> UpElemCd = new NetworkVariable<byte>(0);

        public readonly NetworkVariable<uint> ChoiceRevision = new NetworkVariable<uint>(0);
        float nextLocalPick, nextLocalCombine;
        public readonly NetworkVariable<byte> PendingPicks = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> Offer0 = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> Offer1 = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> Offer2 = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<bool> StageChoiceComplete = new NetworkVariable<bool>(true);
        public readonly NetworkVariable<bool> HasArtifact = new NetworkVariable<bool>(false);

        public Vector2 Pos => IsServer ? (Vector2)transform.position : NetPos.Value;

        // 진행 중인 속성 기술 상태 (서버 전용)
        public ElementId CastElement;
        public int CastLevel;

        float _hurtLeft;
        float _walkClock;
        Vector3 _bodyBaseScale = Vector3.one;
        float _bodyGroundOffset;
        Material _armMat;
        readonly HashSet<ushort> _whipHits = new HashSet<ushort>();
        readonly List<Vector2> _whipPoints = new List<Vector2>(96);
        float _downedClock;
        float _meleeCd;
        float _hitIFrame;
        public readonly NetworkVariable<byte> ExtraDash = new NetworkVariable<byte>(0);
        public readonly NetworkVariable<byte> DashCharges = new NetworkVariable<byte>(1);
        public readonly NetworkVariable<float> DashRechargeLeft = new NetworkVariable<float>(0f);
        float _dashLeft;
        Vector2 _dashDirection;
        public int MaxDashCharges => 1 + ExtraDash.Value;
        PlayerInputReader _input;
        public readonly NetworkVariable<ulong> SkillLevels = new NetworkVariable<ulong>(0);
        public bool HasUpgrade(UpgradeId id) => UpgradeCount(id) > 0;
        public int ComboUpgradeLevel(ComboId id) => UpgradeCount(Upgrades.ForCombo(id));
        Transform _body;
        SpriteRenderer _bodySr;
        SpriteRenderer _ringSr;
        SpriteRenderer _auraSr;
        SpriteRenderer _slashSr;
        Color _baseColor;

        public override void OnNetworkSpawn()
        {
            All.Add(this);
            BuildVisuals();

            if (IsServer)
            {
                SkillLevels.Value = 0;
                bool hostPlayer = OwnerClientId == NetworkManager.ServerClientId;
                Palette.Value = (byte)(hostPlayer ? 0 : 1);
                Vector2 spawn = hostPlayer ? new Vector2(-2.2f, 0f) : new Vector2(2.2f, 0f);
                transform.position = spawn;
                NetPos.Value = spawn;
                MaxHp.Value = GameConfig.MaxHp(0);
                Hp.Value = MaxHp.Value;
                var session = GameSession.Instance;
                if (session != null)
                {
                    GrantElement((ElementId)(hostPlayer ? session.HostElement.Value : session.GuestElement.Value));
                    if (session.StageFlow.Value == (byte)StageState.Choosing) GrantStageChoice();
                }
            }

            ApplyPalette(Palette.Value);
            Palette.OnValueChanged += OnPaletteChanged;
            Hp.OnValueChanged += OnHpChanged;

            if (IsOwner)
            {
                Local = this;
                _input = new PlayerInputReader();
                _input.Enable();
                var cam = Camera.main;
                if (cam != null)
                {
                    var follow = cam.GetComponent<PlayerCameraFollow>() ?? cam.gameObject.AddComponent<PlayerCameraFollow>();
                    follow.Target = transform;
                    cam.orthographicSize = GameConfig.CameraSize;
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            All.Remove(this);
            Palette.OnValueChanged -= OnPaletteChanged;
            Hp.OnValueChanged -= OnHpChanged;
            if (Local == this)
                Local = null;
            _input?.Disable();
        }

        void OnPaletteChanged(byte prev, byte next) => ApplyPalette(next);

        void OnHpChanged(float prev, float next)
        {
            if (next < prev - 0.01f)
                _hurtLeft = 0.32f;
        }

        void BuildVisuals()
        {
            var shadow = CreateSprite("Shadow", SpriteFactory.Circle(new Color(0f, 0f, 0f, 0.35f)), 0.7f, -2);
            shadow.transform.localPosition = new Vector3(0f, -0.12f, 0f);

            _auraSr = CreateSprite("Aura", SpriteFactory.Circle(Color.white), 1.6f, 8);
            _auraSr.color = new Color(1f, 1f, 1f, 0f);

            var bodySprite = OctopusSheet.Frame(OctopusAnim.Idle, 0);
            _body = CreateSprite("Body", bodySprite, bodySprite.rect.width / OctopusSheet.PixelsPerUnit, 12).transform;
            _bodySr = _body.GetComponent<SpriteRenderer>();
            // Measure opaque pixels, excluding the padded transparent sprite canvas.
            var pixels = bodySprite.texture.GetPixels32();
            int low = bodySprite.texture.height, high = 0;
            for (int y = 0; y < bodySprite.texture.height; y++)
                for (int x = 0; x < bodySprite.texture.width; x++)
                    if (pixels[y * bodySprite.texture.width + x].a > 16)
                    { low = Mathf.Min(low, y); high = Mathf.Max(high, y); }
            float bodyHeight = Mathf.Max(1, high - low + 1) / bodySprite.pixelsPerUnit;
            _bodyBaseScale = Vector3.one * (GameConfig.CameraSize * 2f * GameConfig.PlayerScreenHeight / bodyHeight);
            _body.localScale = _bodyBaseScale;
            _bodyGroundOffset = (bodySprite.texture.height * .5f - low) / bodySprite.pixelsPerUnit * _bodyBaseScale.y - GameConfig.PlayerRadius * .5f;
            var shader = Resources.Load<Shader>("OctopusArmStretch");
            if (shader != null)
            {
                _armMat = new Material(shader);
                _armMat.SetFloat("_Range", 1f);
                _armMat.SetFloat("_BodyRadius", OctopusSheet.BodyRadius);
                _bodySr.material = _armMat;
            }

            _ringSr = CreateSprite("FireRing", SpriteFactory.Ring(Color.white), 1f, 10);
            _ringSr.color = new Color(1f, 0.4f, 0.1f, 0f);

            _slashSr = CreateSprite("MeleeSlash", RuntimeArt.Whip(0), 2.4f, 14);
            _slashSr.color = new Color(1f, 1f, 1f, 0f);
        }

        SpriteRenderer CreateSprite(string childName, Sprite sprite, float worldSize, int order)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            float px = sprite != null ? sprite.rect.width / sprite.pixelsPerUnit : 1f;
            if (px > 0.001f)
                go.transform.localScale = Vector3.one * (worldSize / px);
            return sr;
        }

        void ApplyPalette(byte index)
        {
            _baseColor = index == 0 ? Color.white : new Color(0.65f, 0.92f, 1f);
            if (_bodySr != null)
                _bodySr.color = _baseColor;
        }

        void Update()
        {
            if (IsOwner && IsSpawned && _input != null)
            {
                var session = GameSession.Instance;
                if (_input.BackPressed && session != null && session.IsPlaying)
                {
                    MoveInput.Value = Vector2.zero;
                    GameHud.Instance?.HandleBack();
                    return; // Back consumes this frame before any other local action.
                }
                if (GameHud.Instance != null && GameHud.Instance.ExitConfirmationOpen)
                {
                    MoveInput.Value = Vector2.zero;
                    return;
                }
                MoveInput.Value = LanLobby.IsVisible ? Vector2.zero : _input.Move;

                if (_input.DashPressed && !LanLobby.IsVisible)
                    DashServerRpc(_input.Move);
                if (_input.AttackPressed)
                    PressServerRpc(1);
                if (_input.InteractPressed)
                    PressServerRpc(2);
                if (_input.CombinePressed)
                    GameHud.Instance?.ToggleCombine();
                if (_input.TransformPressed)
                    PressServerRpc(4);
                if (_input.UltimatePressed)
                    PressServerRpc(5);
                int pick = _input.PickPressed();
                if (pick >= 0)
                    ChooseUpgrade(pick);
            }

            if (!IsServer)
            {
                Vector3 dest = NetPos.Value;
                transform.position = Vector3.Lerp(transform.position, dest, 1f - Mathf.Exp(-18f * Time.deltaTime));
            }

            UpdateVisuals();
        }

        public void ServerTick(float dt)
        {
            if (!IsServer)
                return;

            InSteam.Value = GameSession.Instance != null && GameSession.Instance.InSteamField(Pos);
            Charging.Value = GameSession.Instance != null && GameSession.Instance.IsMicrowaveCharging(this);
            _hitIFrame -= dt;
            _meleeCd -= dt;
            if (SpecialCd.Value > 0f)
                SpecialCd.Value = Mathf.Max(0f, SpecialCd.Value - dt);

            if (Downed.Value)
            {
                _dashLeft = 0f;
                AttackLeft.Value = 0f;
                HopZ.Value = 0f;
                Invincible.Value = false;
                NetPos.Value = transform.position;
                return;
            }

            ReviveProgress.Value = 0f;

            Vector2 move = MoveInput.Value;
            if (move.sqrMagnitude > 1f)
                move.Normalize();
            if (move.sqrMagnitude > 0.01f)
                Facing.Value = move.normalized;

            if (DashCharges.Value < MaxDashCharges)
            {
                DashRechargeLeft.Value = Mathf.Max(0f, DashRechargeLeft.Value - dt);
                if (DashRechargeLeft.Value <= 0f)
                {
                    DashCharges.Value++;
                    DashRechargeLeft.Value = DashCharges.Value < MaxDashCharges ? GameConfig.DashRecharge : 0f;
                }
            }
            float dashStep = Mathf.Min(dt, _dashLeft);
            Vector2 travel = _dashDirection * GameConfig.DashSpeed * dashStep + move * MoveSpeedValue * (dt - dashStep);
            _dashLeft = Mathf.Max(0f, _dashLeft - dt);
            transform.position = FiniteBeach.Move(transform.position, (Vector2)transform.position + travel, GameConfig.PlayerRadius);
            NetPos.Value = transform.position;

            if (GameSession.Instance == null || !GameSession.Instance.CombatActive)
                return;

            // 변신 중에는 릴레이 공격(속성/요소)이 멈추고, 변신을 풀어야 다시 돌아간다.
            if (Transformed.Value)
            {
                CancelCasts();
                ComboWindow.Value = false;
            }
            else
            {
                TickAttributeCycle(dt);
                TickComboCycle(dt);
            }

            TickWhip(dt);
        }

        void TickWhip(float dt)
        {
            if (AttackLeft.Value <= 0f || GameSession.Instance == null)
                return;

            int frame = AttackFrame(AttackLeft.Value);
            AttackLeft.Value = Mathf.Max(0f, AttackLeft.Value - dt);
            if (!OctopusSheet.AttackHits(frame))
                return;

            float range = Mathf.Max(1f, MeleeRange());
            Vector2 dir = Facing.Value.sqrMagnitude > 0.01f ? Facing.Value.normalized : Vector2.right;
            _whipPoints.Clear();
            const int samples = 28;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (samples - 1f);
                float angle = Mathf.Lerp(-90f, 90f, t) * Mathf.Deg2Rad;
                Vector2 local = new Vector2(0.9f + 0.45f * Mathf.Cos(angle), 0.35f * Mathf.Sin(angle)) * range;
                Vector2 swept = new Vector2(dir.x * local.x - dir.y * local.y,
                    dir.y * local.x + dir.x * local.y);
                _whipPoints.Add(Pos + swept);
            }
            GameSession.Instance.StrikeWhip(this, _whipPoints, 0.18f, MeleeDamageValue, _whipHits);
        }

        public void StopCombat()
        {
            if (!IsServer) return;
            _dashLeft = 0f;
            CancelCasts();
            AttackLeft.Value = 0f;
            ComboWindow.Value = false;
        }

        public void GrantStageChoice()
        {
            if (!IsServer) return;
            PendingPicks.Value = 2;
            StageChoiceComplete.Value = false;
            RollOffers();
            if (Offer0.Value == 0 && Offer1.Value == 0 && Offer2.Value == 0) { PendingPicks.Value = 0; StageChoiceComplete.Value = true; }
        }

        void CancelCasts()
        {
            HopZ.Value = 0f;
            Invincible.Value = false;
            SetFireRing(0f);
        }

        readonly float[] attributeCooldowns = new float[4];
        int waterCastCount;
        public Vector2 NextWaterDirection(Vector2 facing) => AbilityRunner.Rotate(facing, (waterCastCount++ % 30) * 12f);
        readonly float[] comboCooldowns = new float[3];
        void TickAttributeCycle(float dt)
        {
            for (byte slot = 0; slot < 4; slot++)
            {
                var element = SlotElement(slot);
                if (element == ElementId.None) continue;
                attributeCooldowns[slot] -= dt;
                if (attributeCooldowns[slot] > 0f) continue;
                attributeCooldowns[slot] = GameConfig.AttributeCycle * GameConfig.AttributeCdMul(UpAttrCd.Value);
                if (element == ElementId.Fire) attributeCooldowns[slot] += UpgradeTuning.Data.fireOrbDuration;
                CastElement = element;
                CastLevel = EffectiveAttrLevel(slot);
                GameSession.Instance.CastBasicAbility(this, element, CastLevel);
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

        [ServerRpc]
        void PressServerRpc(byte action)
        {
            if (Downed.Value || GameSession.Instance == null || !GameSession.Instance.IsPlaying || GameSession.Instance.GameOver.Value)
                return;
            if (!GameSession.Instance.CombatActive && action != 2) return;

            switch (action)
            {
                case 1: DoMelee(); break;
                case 2: TryPickup(); break;
                case 3: break; // F opens the local recipe picker; selection uses an ownership-checked RPC.
                case 4: ToggleTransform(); break;
                case 5: CastSpecial(); break;
            }
        }

        [ServerRpc]
        void DashServerRpc(Vector2 direction)
        {
            if (GameSession.Instance == null || !GameSession.Instance.CombatActive || Downed.Value ||
                DashCharges.Value == 0 || _dashLeft > 0f || direction.sqrMagnitude < .01f ||
                float.IsNaN(direction.x) || float.IsNaN(direction.y) || float.IsInfinity(direction.x) || float.IsInfinity(direction.y)) return;
            _dashDirection = direction.normalized;
            Facing.Value = _dashDirection;
            _dashLeft = GameConfig.DashDuration;
            if (DashCharges.Value == MaxDashCharges) DashRechargeLeft.Value = GameConfig.DashRecharge;
            DashCharges.Value--;
        }

        void DoMelee()
        {
            if (_meleeCd > 0f || AttackLeft.Value > 0f || GameSession.Instance == null)
                return;
            if (Facing.Value.sqrMagnitude < 0.01f)
                Facing.Value = Vector2.right;
            _whipHits.Clear();
            AttackLeft.Value = GameConfig.AttackDuration;
            _meleeCd = GameConfig.MeleeInterval;
        }

        static int AttackFrame(float left)
        {
            float t = 1f - Mathf.Clamp01(left / GameConfig.AttackDuration);
            return Mathf.Clamp(Mathf.FloorToInt(t * OctopusSheet.Columns), 0, OctopusSheet.Columns - 1);
        }

        public void Revive()
        {
            if (!IsServer || !Downed.Value)
                return;
            Downed.Value = false;
            ReviveProgress.Value = 0f;
            Hp.Value = MaxHp.Value * GameConfig.ReviveHpRatio;
            _hitIFrame = 1.2f;
        }

        void ToggleTransform()
        {
            if (Special.Value == 0)
                return;
            Transformed.Value = !Transformed.Value;
            if (Transformed.Value)
            {
                CastSpecial();
            }
        }

        void CastSpecial()
        {
            if (Special.Value == 0 || !Transformed.Value || SpecialCd.Value > 0f)
                return;

            SpecialAbilities.Cast(this, GameSession.Instance, (SpecialId)Special.Value, ElementLevel);
            SpecialCd.Value = GameConfig.SpecialCooldown * GameConfig.ElementCdMul(UpElemCd.Value);
        }

        void TryPickup()
        {
            var pickup = GameSession.Instance.NearestPickup(Pos, GameConfig.PickupRange, this);
            if (pickup == null)
                return;

            if (ElementRules.IsSpecialPickup(pickup.Kind))
            {
                if (Special.Value != 0 || !GameSession.Instance.SpecialUnlocked.Value)
                    return;
                Special.Value = (byte)ElementRules.SpecialFromPickup(pickup.Kind);
                GameSession.Instance.CollectPickup(pickup);
                return;
            }

            var element = ElementRules.FromPickup(pickup.Kind);
            if (element == ElementId.None || !GrantElement(element))
                return;
            GameSession.Instance.CollectPickup(pickup);
        }

        public bool HasElement(ElementId element)
        {
            for (byte i = 0; i < 4; i++) if (SlotElement(i) == element) return true;
            return false;
        }
        public bool GrantElement(ElementId element)
        {
            if (!IsServer || !GameplayRules.IsBasic(element) || HasElement(element)) return false;
            for (byte i = 0; i < 4; i++)
                if (SlotElement(i) == ElementId.None) { SetSlot(i, element); return true; }
            return false;
        }

        public ComboId ComboAt(byte slot) => (ComboId)(slot == 0 ? Combo.Value : slot == 1 ? Combo1.Value : slot == 2 ? Combo2.Value : 0);
        public int ComboCount => (Combo.Value != 0 ? 1 : 0) + (Combo1.Value != 0 ? 1 : 0) + (Combo2.Value != 0 ? 1 : 0);
        public bool HasCombo(ComboId combo) => ComboAt(0) == combo || ComboAt(1) == combo || ComboAt(2) == combo;
        public bool CanCombine(byte first, byte second, out ComboId result)
        {
            result = ComboId.None;
            var session = GameSession.Instance;
            return first < 4 && second < 4 && first != second && session != null && session.CombatActive &&
                !Downed.Value && ComboCount < 3 &&
                ElementRules.TryCombo(SlotElement(first), SlotElement(second), out result);
        }
        public void RequestCombine(byte first, byte second)
        {
            if (!IsOwner || Time.unscaledTime < nextLocalCombine) return;
            nextLocalCombine = Time.unscaledTime + .3f;
            CombineServerRpc(first, second, (byte)ComboCount);
        }
        [ServerRpc]
        void CombineServerRpc(byte first, byte second, byte expectedCount)
        { if (expectedCount == ComboCount) ServerCombine(first, second); }
        public bool ServerCombine(byte first, byte second)
        {
            if (!IsServer || !CanCombine(first, second, out var result)) return false;
            byte index = Combo.Value == 0 ? (byte)0 : Combo1.Value == 0 ? (byte)1 : (byte)2;
            if (index == 0) Combo.Value = (byte)result;
            else if (index == 1) Combo1.Value = (byte)result;
            else Combo2.Value = (byte)result;
            SetSlot(first, ElementId.None);
            SetSlot(second, ElementId.None);
            attributeCooldowns[first] = attributeCooldowns[second] = 0f;
            CastElement = ElementId.None;
            comboCooldowns[index] = 0f;
            return true;
        }
        void SetSlot(byte slot, ElementId element)
        {
            if (slot == 0) { Slot0.Value = (byte)element; }
            else if (slot == 1) { Slot1.Value = (byte)element; }
            else if (slot == 2) { Slot2.Value = (byte)element; }
            else if (slot == 3) { Slot3.Value = (byte)element; }
        }

        public void ApplyDamage(float amount, DamageSource source = DamageSource.NormalEnemy)
        {
            if (!IsServer || GameSession.Instance == null || !GameSession.Instance.CombatActive || Downed.Value || Invincible.Value || _hitIFrame > 0f)
                return;
            if (source == DamageSource.NormalEnemy && GameSession.Instance != null && GameSession.Instance.InSteamField(Pos))
                return;
            if (GameSession.Instance != null && GameSession.Instance.IsMicrowaveCharging(this)) amount *= 0.5f;
            Hp.Value = Mathf.Max(0f, Hp.Value - amount);
            _hitIFrame = GameConfig.PlayerHitIFrame;
            if (Hp.Value <= 0f)
            {
                Downed.Value = true;
                ReviveProgress.Value = 0f;
            }
        }

        public void Heal(float amount)
        {
            if (!IsServer || Downed.Value || amount <= 0f)
                return;
            Hp.Value = Mathf.Min(MaxHp.Value, Hp.Value + amount);
        }

        public void ClearArtifact()
        {
            if (IsServer)
                HasArtifact.Value = false;
        }

        public void GiveArtifact()
        {
            if (IsServer && !HasArtifact.Value)
                HasArtifact.Value = true;
        }

        public void DevSetSpecial(byte specialId)
        {
            if (!IsServer || (specialId != 0 && (GameSession.Instance == null || !GameSession.Instance.SpecialUnlocked.Value)))
                return;
            Special.Value = specialId;
            SpecialCd.Value = 0f;
            if (specialId == 0)
                Transformed.Value = false;
        }

        public void DevSetElement(byte slot, byte elementId)
        {
            if (!IsServer || slot > 3 || (elementId != 0 && !GameplayRules.IsBasic((ElementId)elementId))) return;
            for (byte i = 0; i < 4; i++)
                if (i != slot && elementId != 0 && SlotElement(i) == (ElementId)elementId) return;
            SetSlot(slot, (ElementId)elementId);
        }

        public void DevResetSpecialCooldown()
        {
            if (!IsServer)
                return;
            SpecialCd.Value = 0f;
        }

        void RollOffers()
        {
            ChoiceRevision.Value++;
            var regular = new List<UpgradeId>();
            var hidden = new List<UpgradeId>();
            foreach (var id in Upgrades.All)
                if (CanOffer(id)) (Upgrades.IsHidden(id) ? hidden : regular).Add(id);
            var offers = new byte[3];
            for (int i = 0; i < offers.Length; i++)
            {
                bool hiddenRoll = Random.value < UpgradeTuning.Data.hiddenOfferChance;
                var pool = hiddenRoll && hidden.Count > 0 ? hidden : regular;
                if (i == 2 && pool == regular && CanOffer(UpgradeId.ExtraDash) && Random.value < GameConfig.ExtraDashOfferChance)
                { offers[i] = (byte)UpgradeId.ExtraDash; continue; }
                // Never bypass the hidden roll when the ordinary pool is exhausted.
                if (pool.Count == 0) continue;
                int pick = Random.Range(0, pool.Count);
                offers[i] = (byte)pool[pick];
                pool.RemoveAt(pick);
            }
            Offer0.Value = offers[0]; Offer1.Value = offers[1]; Offer2.Value = offers[2];
            if (offers[0] == 0 && offers[1] == 0 && offers[2] == 0)
            { PendingPicks.Value = 0; StageChoiceComplete.Value = true; }
        }

        bool CanOffer(UpgradeId id) => UpgradeCount(id) < Upgrades.MaxPicks(id) &&
            (Upgrades.RequiredElement(id) == ElementId.None || HasElement(Upgrades.RequiredElement(id))) &&
            (Upgrades.RequiredCombo(id) == ComboId.None || HasCombo(Upgrades.RequiredCombo(id)));

        public UpgradeId Offer(int index)
        {
            switch (index)
            {
                case 0: return (UpgradeId)Offer0.Value;
                case 1: return (UpgradeId)Offer1.Value;
                case 2: return (UpgradeId)Offer2.Value;
                default: return UpgradeId.None;
            }
        }

        public int UpgradeCount(UpgradeId id)
        {
            if ((int)id >= 10 && (int)id <= 27) return (int)((SkillLevels.Value >> (((int)id - 10) * 3)) & 7UL);
            switch (id)
            {
                case UpgradeId.ExtraDash: return ExtraDash.Value;
                case UpgradeId.Attack: return UpAttack.Value;
                case UpgradeId.Range: return UpRange.Value;
                case UpgradeId.MoveSpeed: return UpMoveSpeed.Value;
                case UpgradeId.MaxHp: return UpMaxHp.Value;
                case UpgradeId.AttributeRank: return UpAttrRank.Value;
                case UpgradeId.ElementRank: return UpElemRank.Value;
                case UpgradeId.AttributeCooldown: return UpAttrCd.Value;
                case UpgradeId.ElementCooldown: return UpElemCd.Value;
                default: return 0;
            }
        }

        public void ChooseUpgrade(int index)
        {
            if (!IsOwner || Time.unscaledTime < nextLocalPick || PendingPicks.Value == 0 || Offer(index) == UpgradeId.None)
                return;
            nextLocalPick = Time.unscaledTime + .3f;
            ChooseUpgradeServerRpc((byte)index, ChoiceRevision.Value);
        }

        [ServerRpc]
        void ChooseUpgradeServerRpc(byte index, uint revision)
        {
            if (PendingPicks.Value == 0 || revision != ChoiceRevision.Value || GameSession.Instance.StageFlow.Value != (byte)StageState.Choosing)
                return;
            var id = Offer(index);
            if (id == UpgradeId.None || !CanOffer(id))
                return;

            ApplyUpgrade(id);
            PendingPicks.Value--;
            if (PendingPicks.Value > 0)
            {
                RollOffers();
            }
            else
            {
                Offer0.Value = Offer1.Value = Offer2.Value = 0;
                StageChoiceComplete.Value = true;
            }
        }

        void ApplyUpgrade(UpgradeId id)
        {
            if (!IsServer || !CanOffer(id)) return;
            if ((int)id >= 10 && (int)id <= 27)
            {
                SkillLevels.Value += 1UL << (((int)id - 10) * 3);
                return;
            }
            switch (id)
            {
                case UpgradeId.ExtraDash:
                    if (ExtraDash.Value == 0) { ExtraDash.Value = 1; DashCharges.Value++; }
                    break;
                case UpgradeId.Attack: UpAttack.Value++; break;
                case UpgradeId.Range: UpRange.Value++; break;
                case UpgradeId.MoveSpeed: break; // Retain the ID for network compatibility; no speed investment.
                case UpgradeId.AttributeRank: UpAttrRank.Value++; break;
                case UpgradeId.ElementRank: UpElemRank.Value++; break;
                case UpgradeId.AttributeCooldown: UpAttrCd.Value++; break;
                case UpgradeId.ElementCooldown: UpElemCd.Value++; break;
                case UpgradeId.MaxHp:
                    UpMaxHp.Value++;
                    float previous = MaxHp.Value;
                    MaxHp.Value = GameConfig.MaxHp(UpMaxHp.Value);
                    Hp.Value = Mathf.Min(MaxHp.Value, Hp.Value + (MaxHp.Value - previous));
                    break;
            }
        }

        public float MoveSpeedValue => GameConfig.BaseMoveSpeed;

        public float MeleeDamageValue => GameConfig.MeleeDamage(UpAttack.Value);

        public int ElementLevel => Mathf.Clamp(1 + UpElemRank.Value, 1, 5);

        public float SpecialCooldownLeft => SpecialCd.Value;

        public ElementId ActiveElement() => CastElement;

        public int EffectiveAttrLevel(byte slot)
        {
            if (SlotElement(slot) == ElementId.None)
                return 1;
            return 1 + UpgradeCount(Upgrades.ForElement(SlotElement(slot)));
        }

        public ElementId SlotElement(byte slot) => (ElementId)(slot == 0 ? Slot0.Value : slot == 1 ? Slot1.Value : slot == 2 ? Slot2.Value : slot == 3 ? Slot3.Value : 0);

        public float MeleeRange() => GameConfig.MeleeRange(UpRange.Value);

        public float MeleeDamageMul()
        {
            float mul = 1f;
            if (ActiveElement() == ElementId.Stone)
                mul += 0.18f * Mathf.Max(1, CastLevel);
            return mul;
        }

        public StatusMask MeleeStatus(out float burnDps, out float spicyChance)
        {
            burnDps = 0f;
            spicyChance = 0f;
            StatusMask mask = StatusMask.None;

            var active = ActiveElement();
            if (active == ElementId.Fire)
            {
                mask |= StatusMask.Burn;
                burnDps = 7f + 2f * Mathf.Max(1, CastLevel);
            }

            if (active == ElementId.Water)
                mask |= StatusMask.Slow;

            if (GameSession.Instance != null && GameSession.Instance.HasSteamField(this))
            {
                mask |= StatusMask.Burn;
                burnDps = Mathf.Max(burnDps, 9f + 3f * ElementLevel);
            }
            if (HasCombo(ComboId.Chili))
            {
                mask |= StatusMask.Spicy;
                spicyChance = 0.25f;
            }

            return mask;
        }

        Sprite PoseSprite(bool moving, int attackFrame)
        {
            int frame = (int)(_walkClock * 10f) % OctopusSheet.Columns;
            if (Downed.Value)
            {
                if (ReviveProgress.Value > 0.01f)
                {
                    int revive = Mathf.Clamp(Mathf.FloorToInt(ReviveProgress.Value * OctopusSheet.Columns), 0, OctopusSheet.Columns - 1);
                    return OctopusSheet.Frame(OctopusAnim.Revive, revive);
                }

                int death = Mathf.Min(OctopusSheet.Columns - 1, (int)(_downedClock * 10f));
                return OctopusSheet.Frame(OctopusAnim.Death, death);
            }

            if (attackFrame >= 0)
                return OctopusSheet.AttackFrame(Facing.Value, attackFrame);
            if (_hurtLeft > 0f)
                return OctopusSheet.Frame(OctopusAnim.Hit, frame);
            if (moving)
                return OctopusSheet.Frame(OctopusAnim.Move, frame);
            return OctopusSheet.Frame(OctopusAnim.Idle, frame);
        }

        public void SetFireRing(float radius)
        {
            if (_ringSr == null)
                return;
            if (radius <= 0.01f)
            {
                _ringSr.color = new Color(1f, 0.4f, 0.1f, 0f);
                return;
            }

            _ringSr.color = new Color(1f, 0.4f, 0.1f, 0.55f);
            _ringSr.transform.localScale = Vector3.one * (radius * 2.1f);
        }

        void UpdateVisuals()
        {
            if (!IsSpawned || _body == null)
                return;

            float z = HopZ.Value;
            _body.localPosition = new Vector3(0f, _bodyGroundOffset + z * 0.35f, 0f);
            _hurtLeft = Mathf.Max(0f, _hurtLeft - Time.deltaTime);
            if (Downed.Value)
                _downedClock += Time.deltaTime;
            else
                _downedClock = 0f;
            int attackFrame = AttackLeft.Value > 0f ? AttackFrame(AttackLeft.Value) : -1;
            float s = 1f + z * 0.12f;
            _body.localScale = new Vector3(_bodyBaseScale.x * s, _bodyBaseScale.y * s, 1f);
            if (_armMat != null)
                _armMat.SetFloat("_Range", 1f);
            if (_bodySr != null)
            {
                bool moving = MoveInput.Value.sqrMagnitude > 0.01f && !Downed.Value;
                _walkClock += Time.deltaTime;
                _bodySr.sprite = PoseSprite(moving, attackFrame);
                _bodySr.flipX = false;
            }

            if (_slashSr != null)
            {
                bool visible = attackFrame >= 0 && OctopusSheet.AttackHits(attackFrame) && !Downed.Value;
                _slashSr.sprite = RuntimeArt.Whip(Mathf.Max(0, attackFrame));
                _slashSr.color = visible ? Color.white : new Color(1f, 1f, 1f, 0f);
                Vector2 facing = Facing.Value.sqrMagnitude > 0.01f ? Facing.Value.normalized : Vector2.right;
                float range = Mathf.Max(1f, MeleeRange());
                _slashSr.transform.localPosition = (Vector3)(facing * (range * 1.125f));
                _slashSr.transform.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg);
                Vector2 bounds = _slashSr.sprite.bounds.size;
                _slashSr.transform.localScale = new Vector3(range * 0.45f / bounds.x, range * 0.7f / bounds.y, 1f);
            }

            if (_bodySr != null)
            {
                bool powered = GameSession.Instance != null && GameSession.Instance.SpecialPassive.Value && Special.Value != 0;
                if (Downed.Value)
                    _bodySr.color = Color.gray;
                else if (Transformed.Value)
                    _bodySr.color = powered
                        ? Color.Lerp(ElementRules.Of((SpecialId)Special.Value), new Color(1f, 0.86f, 0.2f), 0.65f)
                        : ElementRules.Of((SpecialId)Special.Value);
                else if (powered)
                    _bodySr.color = Color.Lerp(_baseColor, new Color(1f, 0.82f, 0.15f), 0.7f);
                else if (Invincible.Value)
                    _bodySr.color = Color.Lerp(_baseColor, Color.white, 0.6f);
                else
                    _bodySr.color = _baseColor;
                if (InSteam.Value && !Downed.Value)
                {
                    var c = _bodySr.color; c.a = 0.28f; _bodySr.color = c;
                }
            }

            if (_auraSr != null)
            {
                bool powered = GameSession.Instance != null && GameSession.Instance.SpecialPassive.Value && Special.Value != 0;
                bool aura = (Transformed.Value || powered) && !Downed.Value;
                var tint = powered
                    ? new Color(1f, 0.84f, 0.15f)
                    : ElementRules.Of((SpecialId)Special.Value);
                tint.a = aura ? 0.45f + 0.12f * Mathf.Sin(Time.time * 6f) : 0f;
                _auraSr.color = tint;
            }

            if (_ringSr != null)
            {
                if (Downed.Value)
                {
                    float pulse = 0.45f + 0.2f * Mathf.Sin(Time.time * 4f);
                    float fill = Mathf.Clamp01(ReviveProgress.Value);
                    _ringSr.color = new Color(0.3f, 0.95f, 1f, pulse);
                    _ringSr.transform.localScale = Vector3.one * (GameConfig.ReviveRadius * 2.2f * (0.85f + 0.25f * fill));
                }
                else SetFireRing(0f);
            }
        }
    }
}


