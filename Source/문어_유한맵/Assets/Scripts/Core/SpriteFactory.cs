using System.Collections.Generic;
using UnityEngine;

namespace OctopusSurvivors
{
    public static class SpriteFactory
    {
        static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Cache.Clear();
        }

        public static Sprite Circle(Color color, int size = 64)
        {
            return Get(1, color, size);
        }

        public static Sprite Square(Color color, int size = 32)
        {
            return Get(2, color, size);
        }

        public static Sprite Ring(Color color, int size = 96)
        {
            return Get(3, color, size);
        }

        public static Sprite Fan(Color color, int size = 96)
        {
            return Get(4, color, size);
        }

        static Sprite Get(int shape, Color color, int size)
        {
            int key = Hash(shape, color, size);
            if (Cache.TryGetValue(key, out var sprite) && sprite != null)
                return sprite;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[size * size];
            float r = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r;
                    float dy = y - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                    Color px = Color.clear;
                    if (shape == 1)
                    {
                        if (d <= 1f)
                        {
                            float edge = Mathf.SmoothStep(1f, 0.82f, d);
                            px = color;
                            px.a = color.a * edge;
                        }
                    }
                    else if (shape == 2)
                    {
                        px = color;
                    }
                    else if (shape == 3)
                    {
                        if (d <= 1f && d >= 0.72f)
                        {
                            px = color;
                            px.a = color.a * Mathf.SmoothStep(0.72f, 0.82f, d) * Mathf.SmoothStep(1f, 0.9f, d);
                        }
                    }
                    else
                    {
                        // 사진처럼 캐릭터 앞쪽에 붙는 활꼴(원에서 한 조각을 자른 모양).
                        float cx = size * 0.16f;
                        float cy = size * 0.5f;
                        float cr = size * 0.82f;
                        float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        if (x >= size * 0.20f && dist <= cr)
                        {
                            float edge = Mathf.SmoothStep(cr, cr * 0.86f, dist);
                            px = color;
                            px.a = color.a * edge;
                        }
                    }

                    pixels[y * size + x] = px;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            var pivot = shape == 4 ? new Vector2(0.22f, 0.5f) : new Vector2(0.5f, 0.5f);
            sprite = Sprite.Create(tex, new Rect(0, 0, size, size), pivot, size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[key] = sprite;
            return sprite;
        }

        static int Hash(int shape, Color color, int size)
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + shape;
                h = h * 31 + size;
                h = h * 31 + (int)(color.r * 255);
                h = h * 31 + (int)(color.g * 255);
                h = h * 31 + (int)(color.b * 255);
                h = h * 31 + (int)(color.a * 255);
                return h;
            }
        }
    }
}
