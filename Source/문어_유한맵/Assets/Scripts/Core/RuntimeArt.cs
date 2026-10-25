using System.Collections.Generic;
using UnityEngine;

namespace OctopusSurvivors
{
    public static class RuntimeArt
    {
        static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Sprite> Singles = new Dictionary<string, Sprite>();
        static Sprite[,] _basic;
        static Sprite[,] _attack;
        static Sprite[] _whip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Textures.Clear(); Singles.Clear(); _basic = null; _attack = null; _whip = null;
        }

        public static Sprite Basic(int row, int frame)
        {
            int r = Mathf.Clamp(row, 0, 5), f = Mathf.Clamp(frame, 0, 7);
            if (_basic == null || _basic[r, f] == null) _basic = LoadCharacterSheet("Basic", 8, 6);
            return _basic[r, f];
        }

        public static Sprite Attack(Vector2 facing, int frame)
        {
            int row;
            if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y)) row = facing.x < 0f ? 1 : 2;
            else row = facing.y < 0f ? 0 : 3;
            int f = Mathf.Clamp(frame, 0, 7);
            if (_attack == null || _attack[row, f] == null) _attack = LoadCharacterSheet("Attack", 8, 4);
            return _attack[row, f];
        }

        public static Sprite Whip(int frame)
        {
            int f = Mathf.Clamp(frame, 0, 7);
            if (_whip == null || _whip[f] == null)
            {
                _whip = new Sprite[8];
                var texture = LoadTexture("Whip");
                var pixels = texture.GetPixels32();
                int cell = texture.width / 8;
                int minX = cell, minY = texture.height, maxX = 0, maxY = 0;
                for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                    if (pixels[y * texture.width + x].a != 0)
                    { minX = Mathf.Min(minX, x % cell); maxX = Mathf.Max(maxX, x % cell);
                      minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
                for (int i = 0; i < 8; i++)
                {
                    _whip[i] = Sprite.Create(texture, new Rect(i * cell + minX, minY,
                        maxX - minX + 1, maxY - minY + 1), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                }
            }
            return _whip[f];
        }

        // Original pixel art: ember imp, water slime, leaf beetle, spark bat.
        public static Sprite ElementMonster(ElementId element)
        {
            string key = "Monster_" + element;
            if (Singles.TryGetValue(key, out var cached) && cached != null) return cached;
            Singles.Remove(key);
            const int n = 32;
            var texture = new Texture2D(n,n,TextureFormat.RGBA32,false) { filterMode = FilterMode.Point };
            var pixels = new Color[n*n]; var mask = new bool[n*n];
            Color body = element == ElementId.Fire ? new Color(.94f,.25f,.12f) : element == ElementId.Water ? new Color(.15f,.65f,.95f) : element == ElementId.Grass ? new Color(.3f,.67f,.2f) : new Color(.85f,.65f,1f);
            for(int y=1;y<n-1;y++) for(int x=1;x<n-1;x++)
            {
                float dx=x-15.5f, dy=y-14f;
                bool shape;
                if(element==ElementId.Fire)
                    shape=dx*dx/80f+dy*dy/90f<1f || (y>20&&y<29&&Mathf.Abs(dx)>4&&Mathf.Abs(dx)<(30-y)*.8f) || (y<9&&y>3&&Mathf.Abs(dx)>3&&Mathf.Abs(dx)<8);
                else if(element==ElementId.Water)
                    shape=dx*dx/150f+(y-11f)*(y-11f)/65f<1f || (y>14&&y<28&&Mathf.Abs(dx)<(28-y)*.5f);
                else if(element==ElementId.Grass)
                    shape=dx*dx/85f+dy*dy/100f<1f || (Mathf.Abs(dx)>7&&Mathf.Abs(dx)<14&&(y==8||y==9||y==15||y==16)) || (y>22&&y<30&&Mathf.Abs(dx-(y-23)*.6f)<3);
                else
                    shape=dx*dx/32f+dy*dy/70f<1f || (Mathf.Abs(dx)>4&&Mathf.Abs(dx)<15&&y>12&&y<24-Mathf.Abs(dx)*.35f&&((x+y)%7>1||y>18));
                mask[y*n+x]=shape;
            }
            for(int y=1;y<n-1;y++) for(int x=1;x<n-1;x++)
            {
                int i=y*n+x; if(!mask[i]) continue;
                bool edge=!mask[i-1]||!mask[i+1]||!mask[i-n]||!mask[i+n];
                pixels[i]=edge?new Color(.12f,.12f,.19f):Color.Lerp(body,Color.white,y/128f);
                if(y>=15&&y<=18&&(x==11||x==12||x==19||x==20)) pixels[i]=new Color(1f,.97f,.7f);
                if(y>=15&&y<=17&&(x==12||x==20)) pixels[i]=new Color(.12f,.1f,.17f);
                if(y==10&&x>=14&&x<=17) pixels[i]=new Color(.2f,.1f,.15f);
            }
            texture.SetPixels(pixels);texture.Apply();
            var sprite=Sprite.Create(texture,new Rect(0,0,n,n),new Vector2(.5f,.5f),n,0,SpriteMeshType.FullRect);
            Singles[key]=sprite;return sprite;
        }

        public static Texture2D BeachTexture() => LoadTexture("Beach");
        public static Texture2D BossBeachTexture() => LoadTexture("BossBeach");

        public static Sprite Icon(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (Singles.TryGetValue(name, out var cached) && cached != null) return cached;
            Singles.Remove(name);
            var texture = LoadTexture(name);
            if (texture == null) return null;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), Mathf.Max(texture.width, texture.height), 0, SpriteMeshType.FullRect);
            sprite.name = name; Singles[name] = sprite; return sprite;
        }

        public static Sprite ElementIcon(ElementId id)
        {
            switch (id)
            {
                case ElementId.Fire: return Icon("Element_Fire");
                case ElementId.Water: return Icon("Element_Water");
                case ElementId.Grass: return Icon("Element_Grass");
                case ElementId.Stone: return Icon("Element_Stone");
                case ElementId.Electric: return Icon("Element_Electric");
                default: return null;
            }
        }

        public static Sprite SpecialIcon(SpecialId id)
        {
            switch (id)
            {
                case SpecialId.Meteor: return Icon("Special_Meteorite");
                case SpecialId.Hellfire: return Icon("Special_Hellfire");
                case SpecialId.Spirit: return Icon("Special_Spirit");
                case SpecialId.DarkMatter: return Icon("Special_DarkMatter");
                case SpecialId.YinYang: return YinYangArt.Symbol;
                default: return null;
            }
        }

        // The supplied artwork has uneven row spacing; a 256px grid cuts off feet.
        // Find transparent separators and place complete rows on identical canvases.
        static Sprite[,] LoadCharacterSheet(string name, int columns, int rows)
        {
            var source = LoadTexture(name);
            var pixels = source.GetPixels32();
            var bands = new List<Vector2Int>();
            int start = -1;
            for (int y = 0; y <= source.height; y++)
            {
                bool occupied = false;
                if (y < source.height)
                    for (int x = 0; x < source.width; x++)
                        if (pixels[y * source.width + x].a != 0) { occupied = true; break; }
                if (occupied && start < 0) start = y;
                if (!occupied && start >= 0) { bands.Add(new Vector2Int(start, y)); start = -1; }
            }
            if (bands.Count != rows)
                throw new System.InvalidOperationException(name + ": unexpected animation row count " + bands.Count);
            const int canvas = 256, footPadding = 12;
            int cell = source.width / columns;
            var result = new Sprite[rows, columns];
            for (int row = 0; row < rows; row++)
            {
                var band = bands[rows - 1 - row]; // Unity pixels start at the bottom.
                if (band.y - band.x + footPadding * 2 > canvas)
                    throw new System.InvalidOperationException(name + ": character exceeds padded canvas");
                for (int col = 0; col < columns; col++)
                {
                    int minX = cell, maxX = -1;
                    for (int y = band.x; y < band.y; y++)
                    for (int x = 0; x < cell; x++)
                        if (pixels[y * source.width + col * cell + x].a != 0)
                        { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); }
                    var output = new Color32[canvas * canvas];
                    int offsetX = (canvas - (maxX - minX + 1)) / 2;
                    for (int y = band.x; y < band.y; y++)
                    for (int x = minX; x <= maxX; x++)
                        output[(y - band.x + footPadding) * canvas + x - minX + offsetX] =
                            pixels[y * source.width + col * cell + x];
                    var texture = new Texture2D(canvas, canvas, TextureFormat.RGBA32, false)
                    { name = name + "_" + row + "_" + col, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                    texture.SetPixels32(output); texture.Apply();
                    result[row, col] = Sprite.Create(texture, new Rect(0, 0, canvas, canvas),
                        new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    result[row, col].name = texture.name;
                }
            }
            return result;
        }

        static Sprite[,] LoadSheet(string name, int columns, int rows, float ppu)
        {
            var texture = LoadTexture(name);
            var result = new Sprite[rows, columns];
            if (texture == null) return result;
            int cellW = texture.width / columns, cellH = texture.height / rows;
            for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                int y = texture.height - (row + 1) * cellH;
                var sprite = Sprite.Create(texture, new Rect(col * cellW, y, cellW, cellH),
                    new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
                sprite.name = name + "_" + row + "_" + col;
                result[row, col] = sprite;
            }
            return result;
        }

        static Texture2D LoadTexture(string name)
        {
            if (Textures.TryGetValue(name, out var cached))
            {
                if (cached != null) return cached;
                Textures.Remove(name);
                // Unity may unload runtime-created art after Play Mode ends. Sprites made
                // from the released sheet must be recreated with its replacement texture.
                if (name == "Basic") _basic = null;
                if (name == "Attack") _attack = null;
                if (name == "Whip") _whip = null;
                Singles.Remove(name);
            }
            var data = Resources.Load<TextAsset>("Art/" + name);
            if (data == null) { Debug.LogError("Art/" + name + ".bytes 를 찾을 수 없습니다."); return null; }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            if (!ImageConversion.LoadImage(texture, data.bytes, false)) { Object.Destroy(texture); return null; }
            Textures[name] = texture; return texture;
        }
    }
}
