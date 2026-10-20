using UnityEngine;

namespace OctopusSurvivors
{
    public sealed class PlayerCameraFollow : MonoBehaviour
    {
        public Transform Target;
        public float Smooth = 10f;

        Camera _camera;
        static float shakeUntil;
        Vector3 shakeOffset;
        bool _hasLoggedBossMapFit;
        bool _hasLoggedMissingMapRenderer;
        float _lastLoggedAspect;
        Bounds _lastLoggedBounds;
        public static void StrongImpact(Vector2 point)
        {
            if (OctopusPlayer.Local != null && Vector2.Distance(OctopusPlayer.Local.Pos, point) < 10f)
                shakeUntil = Mathf.Max(shakeUntil, Time.time + .1f);
        }

        public static Rect AspectFitViewport(float screenAspect, float targetAspect)
        {
            screenAspect = Mathf.Max(0.01f, screenAspect);
            targetAspect = Mathf.Max(0.01f, targetAspect);
            if (screenAspect > targetAspect)
            {
                float width = targetAspect / screenAspect;
                return new Rect((1f - width) * .5f, 0f, width, 1f);
            }
            float height = screenAspect / targetAspect;
            return new Rect(0f, (1f - height) * .5f, 1f, height);
        }

        public static float MapFitOrthographicSize(Bounds bounds, float aspect)
        {
            aspect = Mathf.Max(0.01f, aspect);
            float verticalSize = bounds.size.y / 2f;
            float horizontalSize = bounds.size.x / (2f * aspect);
            return Mathf.Max(verticalSize, horizontalSize) * 1.05f;
        }

        void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera != null && _camera.orthographic)
                _camera.orthographicSize = GameConfig.CameraSize;
        }

        void LateUpdate()
        {
            transform.position -= shakeOffset;
            shakeOffset = Vector3.zero;
            // Fixed 16:9 gameplay viewport: the same 3 x 3 field on all clients.
            float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            _camera.rect = AspectFitViewport(screenAspect, FiniteBeach.Aspect);
            _camera.aspect = FiniteBeach.Aspect;
            var session = GameSession.Instance;
            bool octopusBattle = session != null && session.OctopusBossActive;
            if (octopusBattle)
            {
                var map = ArenaBuilder.BeachRenderer;
                if (map != null)
                {
                    _hasLoggedMissingMapRenderer = false;
                    FitBossMap(map);
                    return;
                }
                if (!_hasLoggedMissingMapRenderer)
                {
                    Debug.LogError("[Stage5MapCamera] 보스전 맵 SpriteRenderer를 찾지 못했습니다.");
                    _hasLoggedMissingMapRenderer = true;
                }
            }

            _hasLoggedBossMapFit = false;
            float zoom = GameConfig.CameraSize;
            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, zoom, 1f - Mathf.Exp(-5f * Time.deltaTime));
            if (Target == null)
                return;

            Vector3 dest = FiniteBeach.ClampCamera(Target.position, _camera);
            dest.z = transform.position.z;
            float t = 1f - Mathf.Exp(-Smooth * Time.deltaTime);
            transform.position = FiniteBeach.ClampCamera(Vector3.Lerp(transform.position, dest, t), _camera);
            if (Time.time < shakeUntil)
            {
                float strength = Mathf.Clamp01((shakeUntil-Time.time)/.1f)*.045f;
                shakeOffset = new Vector3(Mathf.Sin(Time.time*137),Mathf.Cos(Time.time*119),0)*strength;
                transform.position += shakeOffset;
            }
        }

        void FitBossMap(SpriteRenderer map)
        {
            if (!_camera.orthographic)
            {
                _camera.orthographic = true;
                Debug.LogWarning("[Stage5MapCamera] Main Camera를 Orthographic으로 전환했습니다.");
            }

            Bounds bounds = map.bounds;
            float aspect = Mathf.Max(0.01f, _camera.aspect);
            _camera.orthographicSize = MapFitOrthographicSize(bounds, aspect);
            transform.position = new Vector3(bounds.center.x, bounds.center.y, transform.position.z);
            shakeOffset = Vector3.zero;

            float viewWidth = 2f * _camera.orthographicSize * aspect;
            float viewHeight = 2f * _camera.orthographicSize;
            bool fullyVisible = viewWidth >= bounds.size.x && viewHeight >= bounds.size.y &&
                Mathf.Abs(transform.position.x - bounds.center.x) <= (viewWidth - bounds.size.x) * .5f + .001f &&
                Mathf.Abs(transform.position.y - bounds.center.y) <= (viewHeight - bounds.size.y) * .5f + .001f;
            bool fitChanged = !_hasLoggedBossMapFit || !Mathf.Approximately(_lastLoggedAspect, aspect) ||
                (_lastLoggedBounds.center - bounds.center).sqrMagnitude > 0.0001f ||
                (_lastLoggedBounds.size - bounds.size).sqrMagnitude > 0.0001f;
            if (fitChanged)
            {
                Debug.Log($"[Stage5MapCamera] mapBounds={bounds.size.x:F2}x{bounds.size.y:F2} " +
                    $"center=({bounds.center.x:F2},{bounds.center.y:F2}); " +
                    $"cameraView={viewWidth:F2}x{viewHeight:F2}; aspect={aspect:F3}; " +
                    $"fullyVisible={fullyVisible}; margin=5%.");
                _lastLoggedAspect = aspect;
                _lastLoggedBounds = bounds;
                _hasLoggedBossMapFit = true;
            }
        }
    }
}



