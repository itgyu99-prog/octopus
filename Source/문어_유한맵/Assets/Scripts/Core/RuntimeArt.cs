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
            if (_basic == null) _basic = LoadCharacterSheet("Basic", 8, 6);
            return _basic[Mathf.Clamp(row, 0, 5), Mathf.Clamp(frame, 0, 7)];
        }

        public static Sprite Attack(Vector2 facing, int frame)
        {
            if (_attack == null) _attack = LoadCharacterSheet("Attack", 8, 4);
            int row;
            if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y)) row = facing.x < 0f ? 1 : 2;
            else row = facing.y < 0f ? 0 : 3;
            return _attack[row, Mathf.Clamp(frame, 0, 7)];
        }

        public static Sprite Whip(int frame)
        {
            if (_whip == null)
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
            return _whip[Mathf.Clamp(frame, 0, 7)];
        }

        public static Texture2D BeachTexture() => LoadTexture("Beach");

        public static Sprite Icon(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (Singles.TryGetValue(name, out var cached)) return cached;
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
            if (Textures.TryGetValue(name, out var cached)) return cached;
            var data = Resources.Load<TextAsset>("Art/" + name);
            if (data == null) { Debug.LogError("Art/" + name + ".bytes 를 찾을 수 없습니다."); return null; }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            if (!ImageConversion.LoadImage(texture, data.bytes, false)) { Object.Destroy(texture); return null; }
            Textures[name] = texture; return texture;
        }
    }
}
