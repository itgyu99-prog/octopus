using UnityEngine;

namespace OctopusSurvivors
{
    // Sampled from replicated actor age. No per-frame random state, accumulating rotation,
    // TrailRenderer, or orbit line: host, client and preview use exactly the same poses.
    public sealed class YinYangVisual : MonoBehaviour
    {
        public const float OrbitDuration = 2.8f;
        public const float MergeDuration = .28f;
        public const int BurstHits = 10;
        public const float HitInterval = .12f, BurstDuration = 1.4f;
        public const float SymbolScale = .065f;
        public const int MistColumns = 10, MistRows = 8;
        public static Bounds MistFieldBounds => ArenaBuilder.BeachRenderer!=null
            ? ArenaBuilder.BeachRenderer.bounds : new Bounds(Vector3.zero,new Vector3(FiniteBeach.Width,FiniteBeach.Height,0));
        const int EchoCount = 3, ShardCount = 34;
        SpriteRenderer[] parts;
        ActorKind currentKind;
        float age, duration, size, receivedAt;

        public static bool Supports(ActorKind kind) => kind >= ActorKind.YinYangOrbit && kind <= ActorKind.YinYangHitBlack;

        public static float OrbitAngle(float t)
        {
            t = Mathf.Clamp01(t);
            return -.65f + Mathf.PI * 2 * (.72f * t + 1.38f * t * t * t);
        }
        public static Vector2 OrbitPosition(float t, float size, int side)
        {
            float collapse = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.48f, 1, t));
            float r = size * .79f * (1 - collapse);
            float a = OrbitAngle(t) + side * Mathf.PI;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }

        public void Configure(ActorKind kind, float actorAge, float actorDuration, float actorSize)
        {
            if (parts == null || currentKind != kind)
            {
                if (parts != null) foreach (var p in parts) { p.enabled = false; Destroy(p.gameObject); }
                currentKind = kind;
                int count = kind == ActorKind.YinYangOrbit ? 2 * (EchoCount + 1)+48 :
                    kind == ActorKind.YinYangCollision ? 3 : kind == ActorKind.YinYangBurst ? ShardCount + 3 :
                    kind == ActorKind.YinYangMist ? MistColumns*MistRows :
                    kind == ActorKind.YinYangMark || kind == ActorKind.YinYangHitWhite || kind == ActorKind.YinYangHitBlack ? 1 : 2;
                parts = new SpriteRenderer[count];
                for (int i = 0; i < count; i++)
                {
                    var go = new GameObject("YinYang_" + kind + "_" + i);
                    go.transform.SetParent(transform, false); parts[i] = go.AddComponent<SpriteRenderer>();
                }
            }
            age = actorAge; duration = Mathf.Max(.001f, actorDuration); size = actorSize;
            receivedAt = Time.time; enabled = true;
            Sample(age);
        }

        void LateUpdate()
        {
            // Interpolate only between network updates, never run ahead through another phase.
            Sample(Mathf.Min(duration, age + Mathf.Clamp(Time.time - receivedAt, 0, .1f)));
        }
        void OnDisable() { if (parts != null) foreach (var part in parts) if (part != null) part.enabled = false; }

        void Draw(int i, Sprite sprite, Vector2 position, float degrees, float scale, Color color, int order, float yScale = 1)
        {
            var sr = parts[i]; sr.enabled = sprite != null && color.a > .002f;
            sr.sprite = sprite; sr.color = color; sr.sortingOrder = order;
            sr.transform.localPosition = position;
            sr.transform.localRotation = Quaternion.Euler(0, 0, degrees);
            sr.transform.localScale = new Vector3(scale, scale * yScale, 1);
        }

        void Sample(float time)
        {
            if (time < 0) { foreach (var p in parts) p.enabled = false; return; }
            float u = Mathf.Clamp01(time / duration);
            if (currentKind == ActorKind.YinYangOrbit)
            {
                float visibility = Mathf.SmoothStep(0, 1, time / .16f);
                for (int side = 0; side < 2; side++)
                {
                    for (int echo = EchoCount; echo >= 0; echo--)
                    {
                        // Fewer, separated silhouettes; echoes shrink and die before the lock.
                        float lag = echo * Mathf.Lerp(.045f, .07f, u);
                        float t = Mathf.Clamp01((time - lag) / duration);
                        float alpha = echo == 0 ? visibility : visibility * .19f * (1 - (echo - 1) * .26f) *
                            Mathf.InverseLerp(.2f, .55f, u) * (1 - Mathf.InverseLerp(.84f, .97f, u));
                        float angle = OrbitAngle(t) + side * Mathf.PI;
                        Draw(side * 4 + echo, YinYangArt.Comma(side == 1), OrbitPosition(t, size, side),
                            angle * Mathf.Rad2Deg - 45, size * .5f * (echo == 0 ? 1 : .62f), new Color(1, 1, 1, alpha), echo == 0 ? 48 : 44 - echo);
                    }
                }
                for(int i=0;i<16;i++)
                {
                    float t=time-(.08f+i*.155f);
                    int start=2*(EchoCount+1)+i*3;
                    if(t<0 || t>.4f){for(int j=0;j<3;j++)parts[start+j].enabled=false;continue;}
                    float angle=Hash(i+81)*Mathf.PI*2, radius=size*(.35f+Hash(i+35)*1.1f);
                    Vector2 floor=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                    bool white=i%2==0;Color ink=white?Color.white:Color.black;
                    float rise=Mathf.SmoothStep(0,1,t/.2f),burst=Mathf.Clamp01((t-.2f)/.2f);
                    Draw(start,YinYangArt.Fog,floor,0,.95f,new Color(0,0,0,.3f*(1-burst)),39,.3f);
                    ink.a=t<.2f?Mathf.Clamp01(t/.04f):0;
                    Draw(start+1,YinYangArt.Comma(white),floor+Vector2.up*Mathf.Lerp(-.25f,.65f,rise),i*47,.7f*(.5f+.5f*rise),ink,45);
                    ink.a=t>=.2f?1-burst:0;
                    Draw(start+2,YinYangArt.Contact,floor+Vector2.up*.65f,i*31,Mathf.Lerp(.65f,1.6f,burst),ink,48);
                }
            }
            else if (currentKind == ActorKind.YinYangCollision)
            {
                float lockTurn = OrbitAngle(1) + .65f * Mathf.SmoothStep(0, 1, u);
                float shrink = 1 - .82f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.40f, .93f, u));
                float alpha = 1 - Mathf.InverseLerp(.78f, .94f, u);
                for (int side = 0; side < 2; side++)
                    Draw(side, YinYangArt.Comma(side == 1), Vector2.zero, lockTurn * Mathf.Rad2Deg - 45 + side * 180,
                        size * .5f * shrink, new Color(1, 1, 1, alpha * (1 - Mathf.InverseLerp(.15f, .5f, u))), 48);
                Draw(2, YinYangArt.Symbol, Vector2.zero, lockTurn * Mathf.Rad2Deg - 45,
                    size * .58f * shrink, new Color(1, 1, 1, alpha * Mathf.InverseLerp(.15f, .5f, u)), 49);
            }
            else if (currentKind == ActorKind.YinYangBurst)
            {
                int frame = Mathf.Min(YinYangArt.BurstFrames - 1, Mathf.FloorToInt(u * YinYangArt.BurstFrames));
                float impactSize=size*.8f;
                Draw(0, YinYangArt.Burst(frame), Vector2.zero, 0, impactSize, Color.white, 50);
                float flash = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.025f, .11f, time));
                Draw(1, YinYangArt.Flash, Vector2.zero, 0, impactSize * Mathf.Lerp(.34f, .6f, Mathf.Clamp01(time / .08f)),
                    new Color(1, 1, 1, flash), 53);
                for (int i = 0; i < ShardCount; i++)
                {
                    float seed = Hash(i * 11 + 5), angle = Hash(i * 3 + 9) * Mathf.PI * 2;
                    float flight = Mathf.Max(0, time - Hash(i + 51) * .035f);
                    float distance = impactSize * (.05f + (.37f + seed * .19f) * (1 - Mathf.Exp(-flight * 4.4f)));
                    Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                    pos.y -= impactSize * .1f * flight * flight;
                    float fade = Mathf.InverseLerp(0, .035f, flight) * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.29f + seed * .2f, .88f, time)));
                    bool white = i % 5 == 0;
                    Draw(i + 2, YinYangArt.Shard(i), pos, angle * Mathf.Rad2Deg + flight * (seed - .5f) * 540,
                        impactSize * (.016f + seed * .024f), white ? new Color(1, 1, 1, fade) : new Color(.025f, .022f, .035f, fade), 52);
                }
                float beat = 1-Mathf.Repeat(time,HitInterval)/HitInterval;
                Draw(ShardCount+2,YinYangArt.Symbol,Vector2.zero,time*12,size*SymbolScale,
                    new Color(1,1,1,.32f*beat*(1-u)),51);
            }
            else if (currentKind == ActorKind.YinYangMark)
            {
                Draw(0,YinYangArt.Symbol,Vector2.up*.18f,0,size*(1+.06f*Mathf.Sin(u*Mathf.PI)),
                    new Color(1,1,1,0),60);
            }
            else if (currentKind == ActorKind.YinYangMist)
            {
                float fade = Mathf.SmoothStep(0,1,time/.8f) *
                    (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(OrbitDuration+MergeDuration,duration,time)));
                float height=MistFieldBounds.size.y;
                float cloudWidth=Mathf.Max(size/MistColumns*2.8f,height/MistRows*3.6f);
                for(int i=0;i<parts.Length;i++)
                {
                    // A fixed world-space grid covers the entire field, including
                    // shores far from the caster. Overlapping soft edges hide the grid.
                    Vector2 p=new Vector2((i%MistColumns/(MistColumns-1f)-.5f)*size*1.12f,
                        (i/MistColumns/(MistRows-1f)-.5f)*height*1.12f);
                    p+=new Vector2((Hash(i*7+13)-.5f)*size/MistColumns*.3f,
                        (Hash(i*11+7)-.5f)*height/MistRows*.3f);
                    p+=new Vector2(Mathf.Sin(time*.17f+i)*.65f,time*.22f);
                    Draw(i,YinYangArt.Fog,p,Hash(i+19)*360+time*(i%2==0?1:-1),
                        cloudWidth*(.9f+Hash(i+3)*.2f),new Color(.8f,.83f,.88f,.23f*fade),35, .55f+Hash(i+6)*.2f);
                }
            }
            else if(currentKind==ActorKind.YinYangHitWhite || currentKind==ActorKind.YinYangHitBlack)
            {
                bool white=currentKind==ActorKind.YinYangHitWhite;
                Color tint=white?Color.white:Color.black;
                tint.a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,1,u));
                Draw(0,YinYangArt.Contact,Vector2.zero,(white?18:-18)+u*25,
                    size*Mathf.Lerp(.65f,1.15f,Mathf.SmoothStep(0,1,u)),tint,65);
            }
            else
            {
                // A delayed impact wave only; its two broken textured rims expand once.
                float t = Mathf.Clamp01((time - .055f) / Mathf.Max(.01f, duration - .055f));
                float expansion = 1 - Mathf.Pow(1 - t, 3);
                float alpha = time < .055f ? 0 : Mathf.Pow(1 - t, 1.6f);
                Draw(0, YinYangArt.Ring, Vector2.zero, -9, size * .8f * Mathf.Lerp(.18f, 1, expansion), new Color(1, 1, 1, alpha * .9f), 49, .82f);
                Draw(1, YinYangArt.Ring, Vector2.zero, 12, size * .8f * Mathf.Lerp(.13f, .94f, expansion), new Color(.025f, .02f, .03f, alpha * .68f), 47, .82f);
            }
        }

        static float Hash(int n) => Mathf.Repeat(Mathf.Sin(n * 127.1f + 311.7f) * 43758.5453f, 1);
    }
}
