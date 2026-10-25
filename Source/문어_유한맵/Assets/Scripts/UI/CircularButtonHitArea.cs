using UnityEngine;

namespace OctopusSurvivors
{
    // Geometric hit testing keeps circular buttons clickable without requiring
    // a readable copy of their generated texture on the CPU.
    public sealed class CircularButtonHitArea : MonoBehaviour, ICanvasRaycastFilter
    {
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            var rt = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPoint, eventCamera, out var local)) return false;
            var rect = rt.rect;
            var point = local - rect.center;
            float x = point.x / Mathf.Max(.001f, rect.width * .5f);
            float y = point.y / Mathf.Max(.001f, rect.height * .5f);
            return x * x + y * y <= 1;
        }
    }
}
