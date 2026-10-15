using System.Collections.Generic;
using UnityEngine;

namespace OctopusSurvivors
{
    public sealed class WorldView : MonoBehaviour
    {
        public static WorldView Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        readonly Dictionary<ushort, SpriteRenderer> _views = new Dictionary<ushort, SpriteRenderer>();
        readonly Dictionary<ushort, Vector3> _target = new Dictionary<ushort, Vector3>();
        readonly Dictionary<ushort, Vector3> _basePositions = new Dictionary<ushort, Vector3>();
        readonly HashSet<ushort> _floatingPickups = new HashSet<ushort>();
        readonly HashSet<ushort> _seen = new HashSet<ushort>();
        readonly Dictionary<ushort, Vector3> _enemyBodies = new Dictionary<ushort, Vector3>();
        bool _hasBoss;
        ushort _bossId;
        Vector2 _bossPos;

        public bool TryBossPosition(out Vector2 pos)
        {
            pos = _bossPos;
            return _hasBoss;
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void BeginFrame()
        {
            _seen.Clear();
        }

        public bool TryEnemyBodyDiameter(Vector2 position, out float diameter)
        {
            diameter = .7f;
            float nearest = float.PositiveInfinity;
            foreach (var body in _enemyBodies.Values)
            {
                float distance = ((Vector2)body-position).sqrMagnitude;
                float tolerance = Mathf.Max(.75f,body.z);
                if(distance>tolerance*tolerance || distance>=nearest)continue;
                nearest=distance;diameter=body.z;
            }
            return !float.IsPositiveInfinity(nearest);
        }

        public void Show(ushort id, ActorKind kind, Vector2 pos, float size, byte extra, float age = 0f, float duration = 0f, byte hp = 255)
        {
            _seen.Add(id);
            if (!_views.TryGetValue(id, out var sr) || sr == null)
            {
                var go = new GameObject("Actor_" + id);
                go.transform.SetParent(transform, false);
                sr = go.AddComponent<SpriteRenderer>();
                _views[id] = sr;
            }

            sr.sprite = kind == ActorKind.Enemy ? (duration < 1000 ? RuntimeArt.ElementMonster((ElementId)Mathf.RoundToInt(duration)) : null) : SpriteFor(kind);
            sr.color = kind == ActorKind.Enemy && extra == 0 ? Color.white : ColorFor(kind, extra);
            sr.sortingOrder = OrderFor(kind);
            float world = Mathf.Max(0.15f, size);
            bool floatingPickup = ElementRules.IsAttributePickup(kind) || ElementRules.IsSpecialPickup(kind);
            if (floatingPickup)
            {
                world *= 1.12f;
                _floatingPickups.Add(id);
            }
            else
                _floatingPickups.Remove(id);
            if (kind == ActorKind.GiantSpike) world *= 2.5f;
            if (kind == ActorKind.Statue)
                world *= 1.4f;
            bool hit = kind == ActorKind.Enemy || kind == ActorKind.Boss
                ? (extra & (byte)EnemyFlag.Hit) != 0
                : false;
            if (hit)
                world *= 1.16f;
            if (kind == ActorKind.BossLane)
            {
                sr.transform.localScale = new Vector3(Mathf.Max(0.3f, size), 0.42f, 1f);
                sr.transform.rotation = Quaternion.Euler(0f, 0f, extra / 255f * 360f);
            }
            else if (kind == ActorKind.BossArea || kind == ActorKind.BossAreaFill)
            {
                sr.transform.localScale = Vector3.one * Mathf.Max(0.15f, size * 2f);
                sr.transform.rotation = Quaternion.identity;
            }
            else
            {
                sr.transform.localScale = Vector3.one * world;
                if (kind == ActorKind.Shockwave || kind == ActorKind.GiantSpike || ElementEffectVisual.IsDirectional(kind))
                    sr.transform.rotation = Quaternion.Euler(0f, 0f, extra / 255f * 360f);
                else
                    sr.transform.rotation = Quaternion.identity;
            }

            sr.enabled = kind != ActorKind.ComboRepeater;
            if (kind == ActorKind.LightningWarning)
            {
                sr.sprite = SpriteFactory.Ring(new Color(.3f, .8f, 1f, .7f));
                sr.color = new Color(.3f, .8f, 1f, .7f);
                sr.transform.localScale = Vector3.one * size * 2f;
            }
            if (kind == ActorKind.Statue && sr.GetComponent<PearlStackVisual>() == null)
                sr.gameObject.AddComponent<PearlStackVisual>();
            if (kind == ActorKind.Boss)
            {
                _hasBoss = true;
                _bossId = id;
                _bossPos = pos;
            }

            if (!HdSpellVisual.Supports(kind) && (ElementEffectVisual.Supports(kind) || kind == ActorKind.Enemy || kind == ActorKind.Boss))
            {
                var effect = sr.GetComponent<ElementEffectVisual>() ?? sr.gameObject.AddComponent<ElementEffectVisual>();
                effect.Configure(kind, size, extra, age, duration);
                if (ElementEffectVisual.Supports(kind)) sr.enabled = false;
            }

            if (!_target.ContainsKey(id)) sr.transform.position = pos;
            if (kind == ActorKind.Enemy && duration >= 1000)
            {
                var giant = sr.GetComponent<GiantMonsterVisual>() ?? sr.gameObject.AddComponent<GiantMonsterVisual>();
                giant.Configure(duration, extra);
                sr.enabled = false;
            }
            if (kind == ActorKind.Enemy || kind == ActorKind.Boss)
            {
                float spriteDiameter=sr.sprite!=null?Mathf.Max(sr.sprite.bounds.size.x,sr.sprite.bounds.size.y):1f;
                _enemyBodies[id]=new Vector3(pos.x,pos.y,Mathf.Max(.15f,size)*spriteDiameter);
                var impact = sr.GetComponent<ElementHitVisual>() ?? sr.gameObject.AddComponent<ElementHitVisual>();
                impact.Configure(extra, duration < 1000 ? (ElementId)Mathf.RoundToInt(duration) : ElementId.None);
            }
            if (HdSpellVisual.Supports(kind))
            {
                var hd = sr.GetComponent<HdSpellVisual>() ?? sr.gameObject.AddComponent<HdSpellVisual>();
                hd.Configure(kind, age, duration);
                sr.enabled = false;
            }
            if (WaterVfx.Supports(kind))
            {
                var water = sr.GetComponent<WaterVfx>() ?? sr.gameObject.AddComponent<WaterVfx>();
                water.Configure(kind);
                sr.enabled = false;
            }

            UpdateHpBar(sr, kind == ActorKind.Enemy || kind == ActorKind.Boss, hp, kind == ActorKind.Boss);
            var worldPos = (Vector3)pos;
            if (!_target.ContainsKey(id))
            {
                sr.transform.position = worldPos;
                _basePositions[id] = worldPos;
            }
            _target[id] = worldPos;
        }

        void LateUpdate()
        {
            float t = 1f - Mathf.Exp(-18f * Time.deltaTime);
            foreach (var kv in _views)
            {
                if (kv.Value == null)
                    continue;
                if (!_target.TryGetValue(kv.Key, out var dest))
                    continue;
                // Smooth the ground position separately so the hover never accumulates
                // or changes the server's pickup / interaction position.
                Vector3 ground = Vector3.Lerp(_basePositions[kv.Key], dest, t);
                _basePositions[kv.Key] = ground;
                float hover = _floatingPickups.Contains(kv.Key)
                    ? 0.14f + Mathf.Sin(Time.time * (Mathf.PI * 2f / 2.4f) + kv.Key * 0.73f) * 0.12f
                    : 0f;
                kv.Value.transform.position = ground + Vector3.up * hover;
            }
        }

        public void EndFrame()
        {
            var dead = ListPool.Get();
            foreach (var kv in _views)
            {
                if (!_seen.Contains(kv.Key))
                    dead.Add(kv.Key);
            }

            for (int i = 0; i < dead.Count; i++)
            {
                ushort id = dead[i];
                if (_views.TryGetValue(id, out var sr) && sr != null)
                    Destroy(sr.gameObject);
                _views.Remove(id);
                _enemyBodies.Remove(id);
                _target.Remove(id);
                _basePositions.Remove(id);
                _floatingPickups.Remove(id);
            }

            ListPool.Release(dead);
            if (_hasBoss && !_seen.Contains(_bossId))
                _hasBoss = false;
        }

        public void Clear()
        {
            foreach (var kv in _views)
            {
                if (kv.Value != null)
                    Destroy(kv.Value.gameObject);
            }

            _views.Clear();
            _enemyBodies.Clear();
            _target.Clear();
            _basePositions.Clear();
            _floatingPickups.Clear();
            _hasBoss = false;
        }

        static Sprite SpriteFor(ActorKind kind)
        {
            switch (kind)
            {
                case ActorKind.GiantSpike: return GiantMonsterVisual.Spike;
                case ActorKind.PickupFire: return RuntimeArt.ElementIcon(ElementId.Fire);
                case ActorKind.PickupWater: return RuntimeArt.ElementIcon(ElementId.Water);
                case ActorKind.PickupGrass: return RuntimeArt.ElementIcon(ElementId.Grass);
                case ActorKind.PickupStone: return RuntimeArt.ElementIcon(ElementId.Stone);
                case ActorKind.PickupElectric: return RuntimeArt.ElementIcon(ElementId.Electric);
                case ActorKind.PickupMeteor: return RuntimeArt.SpecialIcon(SpecialId.Meteor);
                case ActorKind.PickupHellfire: return RuntimeArt.SpecialIcon(SpecialId.Hellfire);
                case ActorKind.PickupSpirit: return RuntimeArt.SpecialIcon(SpecialId.Spirit);
                case ActorKind.PickupDarkMatter: return RuntimeArt.SpecialIcon(SpecialId.DarkMatter);
                case ActorKind.ArtifactPiece: return RuntimeArt.Icon("RelicPearl");
                case ActorKind.Statue: return RuntimeArt.Icon("RelicSandGlyph");
                case ActorKind.Shockwave:
                case ActorKind.BossArea:
                    return SpriteFactory.Ring(Color.white);
                case ActorKind.Fog:
                case ActorKind.DotZone:
                case ActorKind.Explosion:
                case ActorKind.ElectricWave:
                case ActorKind.SpiritWave:
                case ActorKind.TeamNova:
                case ActorKind.HellfireZone:
                    return SpriteFactory.Ring(Color.white);
                case ActorKind.Tree:
                case ActorKind.VolcanoRock:
                case ActorKind.BounceRock:
                case ActorKind.Meteorite:
                case ActorKind.BossLane:
                    return SpriteFactory.Square(Color.white);
                case ActorKind.Portal:
                    return SpriteFactory.Ring(Color.white);
                default:
                    return SpriteFactory.Circle(Color.white);
            }
        }

        static Color ColorFor(ActorKind kind, byte extra)
        {
            switch (kind)
            {
                case ActorKind.Enemy:
                case ActorKind.Boss:
                    if ((extra & (byte)EnemyFlag.Hit) != 0) return new Color(1f, 0.92f, 0.82f);
                    if ((extra & (byte)EnemyFlag.BlackFlame) != 0) return new Color(0.22f, 0.04f, 0.08f);
                    if ((extra & (byte)EnemyFlag.Boss) != 0) return new Color(0.5f, 0.05f, 0.35f);
                    if ((extra & (byte)EnemyFlag.Spicy) != 0) return new Color(1f, 0.35f, 0.1f);
                    if ((extra & (byte)EnemyFlag.Stunned) != 0) return new Color(0.95f, 0.95f, 0.4f);
                    if ((extra & (byte)EnemyFlag.Burning) != 0) return new Color(1f, 0.45f, 0.15f);
                    return new Color(0.78f, 0.22f, 0.28f);
                case ActorKind.XpOrb: return new Color(0.45f, 0.95f, 1f);
                case ActorKind.ArtifactPiece:
                case ActorKind.Statue: return Color.white;
                case ActorKind.Portal: return new Color(0.35f, 0.95f, 1f, 0.9f);
                case ActorKind.PickupFire:
                case ActorKind.PickupWater:
                case ActorKind.PickupGrass:
                case ActorKind.PickupStone:
                case ActorKind.PickupElectric:
                case ActorKind.PickupMeteor:
                case ActorKind.PickupHellfire:
                case ActorKind.PickupSpirit:
                case ActorKind.PickupDarkMatter: return Color.white;
                case ActorKind.Droplet: return ElementRules.Water;
                case ActorKind.AutoBolt: return new Color(1f, 0.95f, 0.55f);
                case ActorKind.Pepper: return new Color(1f, 0.15f, 0.1f);
                case ActorKind.VolcanoRock: return new Color(0.7f, 0.22f, 0.08f);
                case ActorKind.BounceRock: return ElementRules.Of(ComboId.MossStone);
                case ActorKind.Fog: return ElementRules.Of(ComboId.Fog);
                case ActorKind.Tree: return ElementRules.Of(ComboId.Tree);
                case ActorKind.Fish: return new Color(0.2f, 0.6f, 0.75f);
                case ActorKind.LightningBolt: return ElementRules.Electric;
                case ActorKind.DotZone: return new Color(1f, 0.55f, 0.1f, 0.7f);
                case ActorKind.Shockwave: return new Color(1f, 0.92f, 0.45f, 0.7f);
                case ActorKind.Vine: return ElementRules.Grass;
                case ActorKind.Explosion: return new Color(1f, 0.6f, 0.2f, 0.8f);
                case ActorKind.ElectricWave: return new Color(1f, 0.95f, 0.35f, 0.7f);
                case ActorKind.Meteorite: return new Color(0.85f, 0.45f, 0.2f);
                case ActorKind.HellfireZone: return new Color(1f, 0.3f, 0.06f, 0.75f);
                case ActorKind.SpiritWave: return new Color(0.6f, 1f, 0.9f, 0.75f);
                case ActorKind.BlackHole: return new Color(0.3f, 0.1f, 0.5f, 0.9f);
                case ActorKind.TeamNova: return new Color(1f, 0.9f, 0.6f, 0.8f);
                case ActorKind.HealthPotion: return new Color(0.25f, 0.95f, 0.4f);
                case ActorKind.BossLane: return new Color(1f, 0.72f, 0.15f, 0.85f);
                case ActorKind.BossArea: return new Color(1f, 0.28f, 0.12f, 0.9f);
                case ActorKind.BossAreaFill: return new Color(1f, 0.2f, 0.08f, 0.28f);
                case ActorKind.BossBullet: return new Color(1f, 0.85f, 0.2f);
                case ActorKind.AllySpirit: return ElementRules.Of(SpecialId.Spirit);
                case ActorKind.PiercingMeteor: return new Color(0.95f, 0.55f, 0.2f);
                default: return Color.white;
            }
        }

        static void UpdateHpBar(SpriteRenderer owner, bool show, byte hp, bool boss)
        {
            var bar = owner.transform.Find("HpBar");
            if (!show)
            {
                if (bar != null)
                    bar.gameObject.SetActive(false);
                return;
            }

            if (bar == null)
            {
                var root = new GameObject("HpBar");
                root.transform.SetParent(owner.transform, false);
                root.transform.localPosition = new Vector3(0f, 0.72f, 0f);
                var bg = root.AddComponent<SpriteRenderer>();
                bg.sprite = SpriteFactory.Square(Color.white);
                bg.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);
                bg.sortingOrder = 20;
                root.transform.localScale = new Vector3(1.15f, 0.14f, 1f);

                var fillGo = new GameObject("Fill");
                fillGo.transform.SetParent(root.transform, false);
                var fill = fillGo.AddComponent<SpriteRenderer>();
                fill.sprite = SpriteFactory.Square(Color.white);
                fill.sortingOrder = 21;
                bar = root.transform;
            }

            bar.gameObject.SetActive(true);
            var fillSr = bar.Find("Fill").GetComponent<SpriteRenderer>();
            fillSr.color = boss ? new Color(0.95f, 0.25f, 0.55f) : new Color(0.35f, 0.95f, 0.4f);
            float ratio = hp / 255f;
            var fillT = fillSr.transform;
            fillT.localScale = new Vector3(Mathf.Max(0.04f, ratio), 0.72f, 1f);
            fillT.localPosition = new Vector3((ratio - 1f) * 0.5f, 0f, 0f);
        }

        static int OrderFor(ActorKind kind)
        {
            switch (kind)
            {
                case ActorKind.Fog:
                case ActorKind.DotZone:
                case ActorKind.HellfireZone:
                case ActorKind.BlackHole:
                    return -5;
                case ActorKind.XpOrb:
                    return 3;
                case ActorKind.ArtifactPiece:
                    return 8;
                case ActorKind.Statue:
                    return 2;
                case ActorKind.Portal:
                    return 2;
                case ActorKind.BossLane:
                case ActorKind.BossArea:
                case ActorKind.BossAreaFill:
                    return 1;
                case ActorKind.HealthPotion:
                    return 8;
                case ActorKind.Enemy:
                    return 4;
                case ActorKind.Boss:
                    return 5;
                default:
                    return 6;
            }
        }

        static class ListPool
        {
            static readonly Stack<List<ushort>> Pool = new Stack<List<ushort>>();

            public static List<ushort> Get()
            {
                return Pool.Count > 0 ? Pool.Pop() : new List<ushort>(16);
            }

            public static void Release(List<ushort> list)
            {
                list.Clear();
                Pool.Push(list);
            }
        }
    }
}
