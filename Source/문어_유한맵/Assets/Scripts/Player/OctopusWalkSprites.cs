using System.Collections.Generic;
using UnityEngine;

namespace OctopusSurvivors
{
    public static class OctopusWalkSprites
    {
        static Sprite[] _frames;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { _frames = null; }

        public static Sprite Frame(int index)
        {
            if (_frames == null) Load();
            return _frames[Mathf.Clamp(index, 0, 3)];
        }

        static void Load()
        {
            _frames = new Sprite[4];
            var source = Resources.Load<TextAsset>("OctopusWalk");
            if (source == null)
            {
                Debug.LogError("OctopusWalk.bytes 이동 원본이 없습니다.");
                for (int i = 0; i < 4; i++) _frames[i] = SpriteFactory.Circle(Color.white);
                return;
            }
            var sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(sheet, source.bytes))
            {
                Object.Destroy(sheet);
                for (int i = 0; i < 4; i++) _frames[i] = SpriteFactory.Circle(Color.white);
                return;
            }
            int cell = sheet.width / 4, height = sheet.height;
            var pixels = sheet.GetPixels32();
            const int frameSize = 352;
            for (int frame = 0; frame < 4; frame++)
            {
                var block = new Color32[cell * height];
                int minX = cell, maxX = 0, minY = height;
                for (int y = 0; y < height; y++) for (int x = 0; x < cell; x++)
                {
                    var c = pixels[y * sheet.width + frame * cell + x];
                    block[y * cell + x] = c;
                    if (c.r < 70 && c.g < 70 && c.b < 70)
                    { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); }
                }
                // Flood only neutral light background connected to the outside.
                // Eye highlights remain opaque because their black outlines enclose them.
                var queue = new Queue<int>();
                var visited = new bool[block.Length];
                for (int x = 0; x < cell; x++) { queue.Enqueue(x); queue.Enqueue((height - 1) * cell + x); }
                for (int y = 0; y < height; y++) { queue.Enqueue(y * cell); queue.Enqueue(y * cell + cell - 1); }
                while (queue.Count > 0)
                {
                    int i = queue.Dequeue();
                    if (visited[i]) continue;
                    visited[i] = true;
                    var c = block[i];
                    int high = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                    int low = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                    if (low < 180 || high - low > 28) continue;
                    block[i].a = 0;
                    int x = i % cell, y = i / cell;
                    if (x > 0) queue.Enqueue(i - 1);
                    if (x + 1 < cell) queue.Enqueue(i + 1);
                    if (y > 0) queue.Enqueue(i - cell);
                    if (y + 1 < height) queue.Enqueue(i + cell);
                }
                // Same pixel scale and aligned feet avoid frame-to-frame stretching/jitter.
                int left = (minX + maxX) / 2 - frameSize / 2;
                int bottom = minY - 28;
                var crop = new Color32[frameSize * frameSize];
                for (int y = 0; y < frameSize; y++) for (int x = 0; x < frameSize; x++)
                {
                    int sx = left + x, sy = bottom + y;
                    if (sx >= 0 && sx < cell && sy >= 0 && sy < height) crop[y * frameSize + x] = block[sy * cell + sx];
                }
                var tex = new Texture2D(frameSize, frameSize, TextureFormat.RGBA32, false)
                    { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "OctopusWalk_" + frame };
                tex.SetPixels32(crop); tex.Apply(false, true);
                _frames[frame] = Sprite.Create(tex, new Rect(0, 0, frameSize, frameSize), new Vector2(0.5f, 0.5f), frameSize);
            }
            Object.Destroy(sheet);
        }
    }
}
