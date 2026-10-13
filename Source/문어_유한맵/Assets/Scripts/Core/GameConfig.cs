using UnityEngine;

namespace OctopusSurvivors
{
    public static class GameConfig
    {
        public const ushort Port = 7777;
        public const int MaxPlayers = 2;

        // 기본값(60초)은 접속 실패를 너무 늦게 알려준다. 8초 안에 결론을 낸다.
        public const int ConnectTimeoutMs = 500;
        public const int ConnectAttempts = 16;
        public const float ConnectGiveUpSeconds = ConnectTimeoutMs * ConnectAttempts / 1000f;

        // 유한 필드 실제 치수는 FiniteBeach에서 기준 시야의 가로 2배 × 세로 2배로 정의한다.
        public const float MapSize = 72f;
        public const float CameraSize = 11f;

        public const float PlayerScreenHeight = .04f;
        public const float DashRecharge = 3f;
        public const float DashDuration = .18f;
        public const float DashSpeed = 22f;
        public const float ExtraDashOfferChance = .05f;

        public const float PlayerRadius = 0.42f;
        public const float EnemyRadius = 0.32f;

        public const float BaseMoveSpeed = 5.875f;
        public const float MoveSpeedPerPick = 0.8f;

        public const float BaseMaxHp = 100f;
        public const float MaxHpPerPick = 60f;

        public const float BaseMeleeDamage = 14f;
        public const float MeleeDamagePerPick = 13f;

        public const float BaseMeleeRange = 1.25f;
        public const float MeleeRangePerPick = 0.25f;
        public const float MeleeInterval = 0.48f;
        public const float AttackDuration = 0.48f;
        public const float MeleeArc = 100f;

        public const float PlayerHitIFrame = 0.7f;
        public const float EnemyContactDamage = 6.3f;

        public const float ReviveRadius = 7.4f;
        public const float ReviveHold = 3f;
        public const float ReviveHpRatio = 0.5f;

        public const float PickupRange = 1.7f;
        public const float XpMagnetRange = 6.8f;
        public const float XpCollectRange = 0.7f;
        public const int EnemyXp = 4;
        public const int BossXp = 120;

        // 보유한 최대 4개 속성이 3초 주기에 한 개씩 순서대로 발동한다.
        public const float AttributeCycle = 3f;
        public const float ComboActiveDuration = 3f;
        public const float ComboCooldown = 10f;
        public const float SpecialCooldown = 30f;
        public const float TeamUltimateCooldown = 30f;

        public const float PotionDropChance = 0.01f;
        public const float PotionHeal = 20f;

        // 아래는 기획에 확정 숫자가 없는 항목이다. 이미 코드에 있는 값을 그대로 연결했다.
        public const float BossChargeWindup = 3f;
        public const float BossChargeTravelSeconds = 0.45f;
        public const float BossBulletInterval = AttributeCycle;
        public const float BossBulletSpeed = 9.5f;
        public const float BossBulletLife = 2.2f;
        public const float BossAreaRadius = 9f;
        public const float BossAreaGaugeSeconds = 3f;
        public const float BossAreaGap = AttributeCycle;
        public const int TeamSpiritCount = 6;
        public const float TeamSpiritHitInterval = ComboActiveDuration;
        public const float BlackFlameSpread = 3.4f;
        public const float PiercingMeteorSpeed = 22f;

        // 업그레이드 상한. 단계는 기본 1에서 시작하므로 4번까지만 올릴 수 있다.
        public const int MaxRankPicks = 4;
        public const int MaxCooldownPicks = 5;
        public const float AttributeCdPerPick = 0.08f;
        public const float ElementCdPerPick = 0.1f;

        public const int UpgradeChoices = 3;

        public const int MaxEnemies = 72;
        public const int MaxActors = 512; // 2인 지속 효과. MaxEnemies는 72 유지

        // UDP 한 방에 다 넣으면 몬스터가 늘 때 패킷이 잘려 클라이언트만 멈춘다.
        public const int ActorSyncChunk = 32;
        public const int ActorBytes = 25; // 위치+크기 + 이펙트 경과/지속시간 + 체력 비율

        public const int AttributePickupCount = 12;
        public const float PickupMinSpacing = 11f;
        public const float SpecialMinSpacing = 24f;

        public const float StageDuration = 15f;
        public const int MaxStage = 15;
        public const int BossEvery = 5;
        public const float ArtifactDropChance = 0.05f;
        public const float ArtifactPickupRange = 1.35f;
        public const float StatueDeliverRange = 1.9f;
        public const float PortalEnterRange = 1.8f;
        public const float LevelUpSeconds = 10f;
        public const float SpecialPassiveMul = 1.35f;
        public const float HitFlashSeconds = 0.18f;
        public const float Knockback = 7.5f;
        public const float BossKnockback = 2.6f;

        public static float MoveSpeed(int picks) => BaseMoveSpeed;

        public static float MaxHp(int picks) => BaseMaxHp + picks * MaxHpPerPick;

        public static float MeleeDamage(int picks) => BaseMeleeDamage + picks * MeleeDamagePerPick;

        public static float MeleeRange(int picks) => BaseMeleeRange + picks * MeleeRangePerPick;

        public static float AttributeCdMul(int picks) =>
            Mathf.Max(0.5f, 1f - picks * AttributeCdPerPick);

        public static float ElementCdMul(int picks) =>
            Mathf.Max(0.4f, 1f - picks * ElementCdPerPick);

        public static int XpToNext(int level) => Mathf.RoundToInt(10f * Mathf.Pow(1.28f, level - 1));

        public static Vector2 ClampToMap(Vector2 pos, float radius)
        {
            return FiniteBeach.Clamp(pos, radius);
        }

        public static int Stage(float elapsed) => Mathf.FloorToInt(elapsed / StageDuration) + 1;

        public static int WaveBurst(float elapsed)
        {
            if (elapsed < 7f) return 1;
            if (elapsed < 15f) return 2;
            if (elapsed < 22f) return 6;
            if (elapsed < 32f) return 8;
            if (elapsed < 45f) return 10;
            return 12;
        }

        public static int RequiredPieces(int stage) => GameplayRules.Pieces(stage);

        public static float WaveInterval(float elapsed)
        {
            if (elapsed < 7f) return 0.95f;
            if (elapsed < 15f) return 0.85f;
            if (elapsed < 22f) return 1.15f;
            return 0.9f;
        }

        public static float EnemyHp(float elapsed) => (28f + elapsed * 0.65f) * 0.7f;

        public static float EnemySpeed(float elapsed) => (2.15f + elapsed * 0.025f) * 0.7f;

        public static float BossHp(float elapsed) => (2600f + elapsed * 6f) * 0.7f;
    }
}

