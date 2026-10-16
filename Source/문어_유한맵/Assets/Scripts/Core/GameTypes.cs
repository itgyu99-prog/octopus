namespace OctopusSurvivors
{
    public enum ElementId : byte
    {
        None = 0,
        Fire = 1,
        Water = 2,
        Grass = 3,
        Stone = 4,
        Electric = 5
    }

    public enum ComboId : byte
    {
        None = 0,
        Fog = 1,
        Chili = 2,
        Volcano = 3,
        Tree = 4,
        MossStone = 5,
        Seaweed = 6,
        Lightning = 7, // 기존 ID 유지: 전자레인지
        Mower = 8,
        Jellyfish = 9,
        Soda = 11, Lemon = 12, BlueFlame = 13,
        Magnet = 10
    }

    public enum SpecialId : byte
    {
        None = 0,
        Meteor = 1,
        Hellfire = 2,
        Spirit = 3,
        DarkMatter = 4
    }

    public enum GamePhase : byte
    {
        Waiting = 0,
        Playing = 1
    }

    public enum StageState : byte { Combat, Choosing, Countdown }

    public enum UpgradeId : byte
    {
        None = 0,
        Attack = 1,
        Range = 2,
        MoveSpeed = 3,
        MaxHp = 4,
        AttributeRank = 5,
        ElementRank = 6,
        AttributeCooldown = 7,
        ElementCooldown = 8,
        ExtraDash = 9,
        GrassTargets = 10, WaterRepeats = 11, ElectricRepeats = 12, FireRange = 13,
        LingeringThorns = 14, ThornBurst = 15, WaterSplit = 16, WaterBalloon = 17,
        FireTornado = 18, Fireworks = 19, SkyLightning = 20, Paralysis = 21,
        SodaMastery = 22, ChiliMastery = 23, TreeMastery = 24, LemonMastery = 25, FogMastery = 26, BlueFlameMastery = 27
    }

    public enum ActorKind : byte
    {
        Enemy = 1,
        XpOrb = 2,
        Boss = 3,

        PickupFire = 10,
        PickupWater = 11,
        PickupGrass = 12,
        PickupStone = 13,
        PickupElectric = 14,

        PickupMeteor = 15,
        PickupHellfire = 16,
        PickupSpirit = 17,
        PickupDarkMatter = 18,

        Droplet = 20,
        Pepper = 21,
        VolcanoRock = 22,
        BounceRock = 23,
        Fog = 24,
        Tree = 25,
        Fish = 26,
        LightningBolt = 27,
        DotZone = 28,
        Shockwave = 29,
        Vine = 30,
        Explosion = 31,

        ElectricWave = 32,
        Meteorite = 33,
        HellfireZone = 34,
        SpiritWave = 35,
        BlackHole = 36,
        TeamNova = 37,
        AutoBolt = 38,
        ReviveZone = 39,
        SteamField = 40, ChiliShot = 41, Volcano = 42, LavaWarning = 43,
        LavaImpact = 44, LavaPool = 45, SeaweedPlant = 46, HomingFish = 47,
        MossShot = 48, SeedEmitter = 49, Seed = 50, GrownTree = 51,
        MicrowaveCharge = 52, MicrowaveBlast = 53, MowerBlade = 54,
        Jellyfish = 55, CurrentFan = 56, MagnetPair = 57, MagnetBlast = 58,
        MeleeSlash = 60,
        ArtifactPiece = 61,
        Statue = 62,
        Portal = 63,
        HealthPotion = 64,
        BossLane = 65,
        BossArea = 66,
        BossAreaFill = 67,
        BossBullet = 68,
        AllySpirit = 69,
        SodaWave = 71, LemonShot = 72, FruitTree = 73, Fruit = 74,
        ChainEmitter = 75, ChainArc = 76, FlameAura = 77, ThornEmitter = 78, CurrentEmitter = 79,
        Mist = 80, WaterEmitter = 81, RadialCurrent = 82, WaterImpact = 83, GiantSpike = 84,
        ThornPatch = 85, ThornShot = 86, Balloon = 87, Tornado = 88, Firework = 89,
        LightningWarning = 90, ComboRepeater = 91, ChiliField = 92, LemonWheel = 93,
        WaterBalloonShot = 94, WaterBalloonBurst = 95,
        PiercingMeteor = 70
    }

    public enum DamageSource : byte { NormalEnemy, BossContact, BossSkill, Other }

    public enum EnemyFlag : byte
    {
        None = 0,
        Burning = 1 << 0,
        Slowed = 1 << 1,
        Stunned = 1 << 2,
        Spicy = 1 << 3,
        Boss = 1 << 4,
        Hit = 1 << 5,
        BlackFlame = 1 << 6
    }

    public static class NetHashes
    {
        public const uint Player = 81001501;
        public const uint Session = 81001502;
    }

    public static class NetChannels
    {
        public const string Actors = "octopus.actors";
    }
}
