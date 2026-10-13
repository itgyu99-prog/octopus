using System.Collections.Generic;
using UnityEngine;

namespace OctopusSurvivors
{
    // Presentation only: the server's existing delivered counter remains authoritative.
    public sealed class PearlStackVisual : MonoBehaviour
    {
        const int MaxVisible = 21;
        readonly List<Transform> _pearls = new List<Transform>();
        readonly List<float> _landTimes = new List<float>();
        TextMesh _label;
        int _lastCount = -1;
        int _lastStage = -1;

        void Awake() => EnsureLabel();

        void EnsureLabel()
        {
            if (_label != null) return;
            var go = new GameObject("PearlCount");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            _label = go.AddComponent<TextMesh>();
            _label.anchor = TextAnchor.MiddleCenter;
            _label.fontSize = 48;
            _label.characterSize = 0.065f;
            _label.color = new Color(1f, 0.96f, 0.76f);
            go.GetComponent<MeshRenderer>().sortingOrder = 11;
        }

        void Update()
        {
            var session = GameSession.Instance;
            if (session == null) return;
            Refresh(session.ArtifactDelivered.Value, session.Stage.Value);
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = session.StatueActivated.Value ? new Color(1f,.85f,.3f) : Color.white;
            if (session.StatueActivated.Value) _label.text = "CLEAR";
            else if (session.Stage.Value % 5 == 0) _label.text = "BOSS";
            Animate(Time.time);
        }

        public void Refresh(int count, int stage)
        {
            EnsureLabel();
            count = Mathf.Max(0, count);
            if (_lastCount == count && _lastStage == stage) return;
            bool animate = _lastCount >= 0 && stage == _lastStage && count > _lastCount;
            if (_lastStage != stage)
            {
                foreach (var pearl in _pearls) Release(pearl.gameObject);
                _pearls.Clear(); _landTimes.Clear();
            }
            int visible = Mathf.Min(count, MaxVisible);
            while (_pearls.Count > visible)
            {
                int i = _pearls.Count - 1;
                Release(_pearls[i].gameObject); _pearls.RemoveAt(i); _landTimes.RemoveAt(i);
            }
            while (_pearls.Count < visible)
            {
                int i = _pearls.Count;
                var go = new GameObject("DepositedPearl_" + (i + 1));
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.one * 0.2f;
                go.transform.localPosition = PositionFor(i);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = RuntimeArt.Icon("RelicPearl");
                sr.sortingOrder = 9;
                _pearls.Add(go.transform);
                _landTimes.Add(animate ? Time.time : Time.time - 1f);
            }
            _label.text = count + " / " + GameConfig.RequiredPieces(stage);
            _lastCount = count; _lastStage = stage;
        }

        public static Vector3 PositionFor(int index)
        {
            int row = 0, width = 6;
            while (index >= width && width > 1) { index -= width; width--; row++; }
            return new Vector3((index - (width - 1) * 0.5f) * 0.19f, -0.26f + row * 0.16f, 0f);
        }

        static void Release(GameObject go)
        {
            go.SetActive(false);
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        void Animate(float now)
        {
            for (int i = 0; i < _pearls.Count; i++)
            {
                float t = Mathf.Clamp01((now - _landTimes[i]) / 0.28f);
                _pearls[i].localPosition = PositionFor(i) + Vector3.up * (0.38f * (1f - t) * (1f - t));
            }
        }
    }
}
