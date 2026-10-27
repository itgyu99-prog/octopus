using UnityEngine;

namespace OctopusSurvivors
{
    // Both clients and the host sample the same atlas poses from replicated age.
    public sealed class DimensionRiftVisual : MonoBehaviour
    {
        SpriteRenderer body;
        SpriteRenderer blackWave;
        SpriteRenderer[] smoke;
        ActorKind kind;
        float age, duration, diameter, receivedAt, lastTime = -1;
        static readonly float[] Impacts = { .35f, .95f, 1.65f, 1.9f, 2.2f };
        public static bool Supports(ActorKind k) => k == ActorKind.DimensionRift || k == ActorKind.RiftWave;
        public void Configure(ActorKind actorKind, float actorAge, float actorDuration, float size)
        {
            if (body == null)
            {
                kind = actorKind;
                body = Part("Fractured space", 58);
                if(kind==ActorKind.RiftWave) blackWave=Part("Charcoal particle front",57);
                if (kind == ActorKind.DimensionRift)
                {
                    smoke = new SpriteRenderer[36];
                    for (int i = 0; i < smoke.Length; i++) smoke[i] = Part("Rift vapor " + i, i % 3 == 0 ? 59 : 57);
                }
            }
            age = actorAge; duration = Mathf.Max(.001f, actorDuration); diameter = size;
            receivedAt = Time.time;
            Sample(age);
        }
        SpriteRenderer Part(string title, int order)
        {
            var go = new GameObject(title); go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sortingOrder = order; return sr;
        }
        void LateUpdate()
        {
            bool running = GameSession.Instance != null && GameSession.Instance.CombatActive;
            Sample(Mathf.Min(duration, age + (running ? Mathf.Clamp(Time.time - receivedAt, 0, .1f) : 0)));
        }
        void Sample(float t)
        {
            if (body == null) return;
            body.enabled = t >= 0 && t < duration;
            body.transform.localPosition = Vector2.up * DimensionRiftSettings.Lift;
            body.color = Color.white;
            if (kind == ActorKind.RiftWave)
            {
                body.sprite = DimensionRiftArt.Wave(t / DimensionRiftSettings.WaveDuration);
                body.transform.localScale = Vector3.one * diameter;
                blackWave.enabled=body.enabled;
                blackWave.sprite=body.sprite;
                blackWave.transform.localPosition=body.transform.localPosition;
                blackWave.transform.localScale=Vector3.one*diameter*SpecialSkillTuning.Data.riftBlackScale;
                blackWave.transform.localRotation=Quaternion.Euler(0,0,3);
                blackWave.color=new Color(.025f,.02f,.035f,SpecialSkillTuning.Data.riftBlackOpacity);
                if (lastTime < 0 && t < .2f) PlayerCameraFollow.StrongImpact(transform.position);
            }
            else
            {
                float closeAt = Mathf.Min(8.05f, duration - 1);
                float close = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(closeAt, duration, t));
                int frame = t < 2.5f ? Mathf.Min(23, Mathf.FloorToInt(t / 2.5f * 24)) :
                    t >= closeAt ? Mathf.Clamp(Mathf.FloorToInt(close * 23), 0, 23) : 24 + Mathf.FloorToInt(t * 8) % 8;
                body.sprite = DimensionRiftArt.Tear(frame);
                body.transform.localScale = Vector3.one * diameter;
                body.color = new Color(1, 1, 1, close);
                foreach (float beat in Impacts)
                    if (lastTime < beat && t >= beat && t - beat < .2f) PlayerCameraFollow.StrongImpact(transform.position);
                for (int i = 0; i < smoke.Length; i++)
                {
                    var sr = smoke[i];
                    float life = 1.9f + Hash(i * 3) * 1.1f;
                    float shifted = t - 1.6f - Hash(i + 9) * 1.2f;
                    float u = Mathf.Repeat(shifted, life) / life;
                    bool visible = shifted > 0 && t < duration && close > .001f;
                    sr.enabled = visible;
                    if (!visible) continue;
                    float along = Hash(i + 71) * 2 - 1;
                    float drift = (Hash(i + 23) - .5f) * u * 1.4f;
                    Vector2 origin = new Vector2(along * diameter * .34f, -along * diameter * .18f);
                    sr.transform.localPosition = origin + new Vector2(drift + Mathf.Sin(u * 5 + i) * u * .24f,
                        DimensionRiftSettings.Lift + u * 2.1f);
                    sr.transform.localScale = Vector3.one * Mathf.Lerp(.6f, 2.5f, u);
                    sr.transform.localRotation = Quaternion.Euler(0, 0, Hash(i + 51) * 360 + u * 20);
                    sr.sprite = DimensionRiftArt.Smoke((int)(t * 8 + i) % 16);
                    sr.color = new Color(.085f, .08f, .10f, Mathf.Sin(Mathf.PI * u) * close * (i % 3 == 0 ? .28f : .56f));
                }
            }
            lastTime = t;
        }
        static float Hash(int value) => Mathf.Repeat(Mathf.Sin(value * 127.1f + 311.7f) * 43758.5453f, 1);
        void OnDisable()
        {
            if (body != null) body.enabled = false;
            if (blackWave != null) blackWave.enabled = false;
            if (smoke != null) foreach (var part in smoke) if (part != null) part.enabled = false;
        }
    }
}
