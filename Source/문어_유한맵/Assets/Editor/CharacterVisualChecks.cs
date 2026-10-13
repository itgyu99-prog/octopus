using System;
using UnityEditor;
using UnityEngine;

namespace OctopusSurvivors.EditorTools
{
    public static class CharacterVisualChecks
    {
        [MenuItem("문어/캐릭터 및 진주 표시 검증")]
        public static void Verify()
        {
            CheckSheet("Basic", 6, (r, c) => RuntimeArt.Basic(r, c));
            Vector2[] directions = { Vector2.down, Vector2.left, Vector2.right, Vector2.up };
            CheckSheet("Attack", 4, (r, c) => RuntimeArt.Attack(directions[r], c));
            for (int i = 0; i < 8; i++)
                if (RuntimeArt.Whip(i).bounds.size.x <= 0f) throw new Exception("Invalid whip bounds");
            var root = new GameObject("PearlCheck");
            var stack = root.AddComponent<PearlStackVisual>();
            foreach (int count in new[] { 0, 1, 2, 7, 21, 30, 2, 0 })
            {
                stack.Refresh(count, 1);
                if (root.GetComponentInChildren<TextMesh>().text != count + " / 2")
                    throw new Exception("Pearl counter mismatch");
                if (root.GetComponentsInChildren<SpriteRenderer>().Length != Mathf.Min(count, 21))
                    throw new Exception("Pearl stack size mismatch");
            }
            stack.Refresh(5, 2);
            stack.Refresh(0, 3);
            if (root.GetComponentsInChildren<SpriteRenderer>().Length != 0)
                throw new Exception("Pearl stack was not reset for the next stage");
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log("CHARACTER_VISUAL_CHECKS_PASSED: all 80 character frames retain every opaque pixel, padding is intact, pearl counter handles reset and overflow.");
        }

        static void CheckSheet(string name, int rows, Func<int, int, Sprite> frame)
        {
            var original = new Texture2D(2, 2);
            ImageConversion.LoadImage(original, Resources.Load<TextAsset>("Art/" + name).bytes);
            int before = 0, after = 0;
            foreach (var p in original.GetPixels32()) if (p.a != 0) before++;
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < 8; c++)
            {
                var sprite = frame(r, c);
                var texture = sprite.texture;
                var pixels = texture.GetPixels32();
                for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                {
                    if (pixels[y * texture.width + x].a == 0) continue;
                    after++;
                    if (x == 0 || y == 0 || x == texture.width - 1 || y == texture.height - 1)
                        throw new Exception(sprite.name + ": clipped edge");
                }
            }
            UnityEngine.Object.DestroyImmediate(original);
            if (before != after) throw new Exception(name + ": lost or duplicated pixels " + before + " != " + after);
        }
    }
}
