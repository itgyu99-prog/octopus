using UnityEngine;
namespace OctopusSurvivors
{
    public sealed class RiftMarkVisual : MonoBehaviour
    {
        SpriteRenderer left, right;
        public void Configure(bool marked)
        {
            if (left == null)
            {
                left = Stroke(-35); right = Stroke(35);
            }
            left.enabled = right.enabled = marked;
        }
        SpriteRenderer Stroke(float angle)
        {
            var go = new GameObject("Execution sigil"); go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector2.up * .8f;
            go.transform.localScale = new Vector3(.035f, .25f, 1);
            go.transform.localRotation = Quaternion.Euler(0, 0, angle);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = SpriteFactory.Square(Color.white);
            sr.color = new Color(.85f, .83f, .88f, .8f); sr.sortingOrder = 61; return sr;
        }
    }
}
