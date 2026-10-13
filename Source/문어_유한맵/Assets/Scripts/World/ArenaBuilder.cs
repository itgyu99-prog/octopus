using UnityEngine;

namespace OctopusSurvivors
{
    public static class ArenaBuilder
    {
        static GameObject root;
        static Sprite sprite;
        public static void Build()
        {
            Destroy();
            root = new GameObject("FiniteBeachArena");
            var texture = RuntimeArt.BeachTexture();
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -20;
            root.transform.localScale = new Vector3(FiniteBeach.Width / sprite.bounds.size.x,
                FiniteBeach.Height / sprite.bounds.size.y, 1);
        }
        public static void Destroy()
        {
            if (root != null) Object.Destroy(root);
            if (sprite != null) Object.Destroy(sprite);
            root = null; sprite = null;
        }
    }

    // Shared world dimensions keep host/client terrain identical at every window size.
    // Collision outlines use legacy 1492 x 1054 normalized coordinates; the current HD texture is 5760 x 3240.
    public static class FiniteBeach
    {
        public const float Aspect = 16f / 9f;
        public const float Height = GameConfig.CameraSize * 2 * 2;
        public const float ArtworkAspect = 5760f / 3240f;
        public const float Width = Height * ArtworkAspect;
        static readonly Vector2[] Shore = {
            P(110,180), P(115,100), P(350,100), P(470,62), P(665,64),
            P(855,113), P(1020,91), P(1270,100), P(1380,170),
            P(1390,310), P(1380,420), P(1400,610), P(1370,735),
            P(1320,845), P(1190,890), P(1010,935), P(920,900),
            P(775,915), P(700,940), P(545,910), P(400,884),
            P(285,869), P(185,829), P(119,710), P(99,610),
            P(97,460), P(112,310)
        };
        // Conservative footprints include rock edges/shadows; shells and starfish are decoration.
        static readonly Vector4[] Rocks = {
            R(198,240,68,55), R(97,352,53,48), R(61,567,56,51),
            R(155,790,78,72), R(1284,239,76,54), R(1400,480,47,63),
            R(1260,707,51,42), R(1350,790,54,48), R(468,933,77,63),
            R(862,951,85,61), R(220,308,24,19), R(1318,305,24,21),
            R(1231,292,24,20), R(1357,332,22,18), R(1298,662,23,20),
            R(1385,706,23,22), R(114,416,23,19), R(105,623,21,18),
            R(132,579,23,19), R(243,774,23,22), R(110,694,23,19),
            R(246,188,20,17), R(113,129,22,19), R(1355,132,21,19),
            R(1404,431,21,19), R(1285,839,24,21), R(240,842,25,22)
        };
        static Vector2 P(float x, float y) => new Vector2((x / 1492f - .5f) * Width, (.5f - y / 1054f) * Height);
        static Vector4 R(float x, float y, float rx, float ry)
        { var p = P(x,y); return new Vector4(p.x,p.y,rx / 1492f * Width,ry / 1054f * Height); }
        public static bool Walkable(Vector2 p, float radius)
        {
            bool inside = false;
            for (int i=0,j=Shore.Length-1;i<Shore.Length;j=i++)
            {
                var a=Shore[j]; var b=Shore[i];
                if ((a.y>p.y)!=(b.y>p.y) && p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x) inside=!inside;
                var ab=b-a;
                if ((p-(a+ab*Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude))).sqrMagnitude < radius*radius) return false;
            }
            if (!inside) return false;
            foreach(var r in Rocks)
            {
                float x=(p.x-r.x)/(r.z+radius), y=(p.y-r.y)/(r.w+radius);
                if (x*x+y*y<=1) return false;
            }
            return true;
        }
        public static Vector2 Clamp(Vector2 p, float radius)
        {
            if (Walkable(p,radius)) return p;
            p = new Vector2(Mathf.Clamp(p.x,-Width*.5f,Width*.5f),Mathf.Clamp(p.y,-Height*.5f,Height*.5f));
            // Deterministic nearest valid placement for spawns and displacement effects.
            for(float d=.25f;d<Width;d+=.25f)
                for(int i=0;i<32;i++)
                {
                    float a=i*Mathf.PI/16;
                    var q=p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*d;
                    if(Walkable(q,radius)) return q;
                }
            return Vector2.zero;
        }
        public static Vector2 Move(Vector2 from, Vector2 to, float radius)
        {
            var p=Clamp(from,radius);
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(from,to)/.12f));
            var step=(to-from)/steps;
            for(int i=0;i<steps;i++)
            {
                if(Walkable(p+step,radius)) p+=step;
                else {
                    var x=p+new Vector2(step.x,0); if(Walkable(x,radius)) p=x;
                    var y=p+new Vector2(0,step.y); if(Walkable(y,radius)) p=y;
                }
            }
            return p;
        }
        public static Vector3 ClampCamera(Vector3 p, Camera camera)
        {
            float y=camera.orthographicSize, x=y*camera.aspect;
            p.x=Mathf.Clamp(p.x,-Mathf.Max(0,Width*.5f-x),Mathf.Max(0,Width*.5f-x));
            p.y=Mathf.Clamp(p.y,-Mathf.Max(0,Height*.5f-y),Mathf.Max(0,Height*.5f-y));
            return p;
        }
    }
}
