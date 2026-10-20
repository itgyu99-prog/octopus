using UnityEngine;

namespace OctopusSurvivors
{
    [DefaultExecutionOrder(10000)]
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
        float _lastLoggedSize;
        OctopusBossVisual _bossVisual;
        RectTransform _bossHud;
        readonly Vector3[] _hudCorners = new Vector3[4];
        Color _normalBackgroundColor;
        bool _bossBackgroundApplied;
        public bool BossMapCameraActive { get; private set; }
        bool _hasLoggedState, _loggedAlive, _loggedBossMode;
        int _loggedStage;
        byte _loggedPattern;

        public static bool ShouldUseBossMap(GameSession session) => session != null && session.IsPlaying &&
            session.Stage.Value == 5 && session.BossAlive.Value;
        public static float BossOrthographicSize => GameConfig.CameraSize * 2f;
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

        void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera != null) _normalBackgroundColor = _camera.backgroundColor;
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
            // Stage selects the encounter; BossAlive controls its lifetime. Pattern is
            // attack state and must not switch the camera or map when it becomes zero.
            bool bossMode = ShouldUseBossMap(session);
            int stage = session != null ? session.Stage.Value : 0;
            bool alive = session != null && session.BossAlive.Value;
            byte pattern = session != null ? session.OctopusBoss.Value.Pattern : (byte)0;
            bool stateChanged = !_hasLoggedState || _loggedStage != stage || _loggedAlive != alive ||
                _loggedPattern != pattern || _loggedBossMode != bossMode;
            if (stateChanged) LogCameraState("before", stage, alive, pattern);
            BossMapCameraActive = bossMode;
            ArenaBuilder.UseBossArtwork(bossMode);
            if (bossMode)
            {
                var map = ArenaBuilder.BeachRenderer;
                if (map != null)
                {
                    _hasLoggedMissingMapRenderer = false;
                    FitBossMap(map);
                }
                else if (!_hasLoggedMissingMapRenderer)
                {
                    Debug.LogError("[Stage5MapCamera] 보스전 맵 SpriteRenderer를 찾지 못했습니다.");
                    _hasLoggedMissingMapRenderer = true;
                }
                // Keep boss mode even if the renderer is temporarily unavailable.
                // Falling through would resume player tracking during the encounter.
            }
            else FollowPlayer();

            if (stateChanged)
            {
                LogCameraState("after", stage, alive, pattern);
                _hasLoggedState = true; _loggedStage = stage; _loggedAlive = alive;
                _loggedPattern = pattern; _loggedBossMode = bossMode;
            }
        }

        void LogCameraState(string phase, int stage, bool alive, byte pattern)
        {
            var map = ArenaBuilder.BeachRenderer;
            string image = map != null && map.sprite != null ? map.sprite.texture.name : "none";
            Debug.Log($"[Stage5CameraState:{phase}] Stage={stage}; BossAlive={alive}; Pattern={pattern}; " +
                $"mode={(BossMapCameraActive ? "BossMap" : "PlayerFollow")}; size={_camera.orthographicSize:F4}; " +
                $"center={transform.position}; map={image}; bounds={(map != null ? map.bounds.ToString() : "none")}.");
        }

        void FollowPlayer()
        {
            _hasLoggedBossMapFit = false;
            if (_bossBackgroundApplied)
            {
                _camera.backgroundColor = _normalBackgroundColor;
                _bossBackgroundApplied = false;
            }
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
            if (!_bossBackgroundApplied)
            {
                _camera.backgroundColor = map.sprite.texture.GetPixel(0, 0);
                _bossBackgroundApplied = true;
            }
            if (!_camera.orthographic)
            {
                _camera.orthographic = true;
                Debug.LogWarning("[Stage5MapCamera] Main Camera를 Orthographic으로 전환했습니다.");
            }

            Bounds bounds = map.bounds;
            float aspect = Mathf.Max(0.01f, _camera.aspect);
            // User-selected fixed 2x view. Never zoom farther out to fit the artwork or HUD.
            _camera.orthographicSize = BossOrthographicSize;
            float centerY = Mathf.Max(OctopusBossPattern.OverviewCenter.y, BossHudMinimumCenterY());
            transform.position = new Vector3(bounds.center.x, centerY, transform.position.z);
            shakeOffset = Vector3.zero;

            float viewWidth = 2f * _camera.orthographicSize * aspect;
            float viewHeight = 2f * _camera.orthographicSize;
            bool fullyVisible = viewWidth >= bounds.size.x && viewHeight >= bounds.size.y &&
                Mathf.Abs(transform.position.x - bounds.center.x) <= (viewWidth - bounds.size.x) * .5f + .001f &&
                Mathf.Abs(transform.position.y - bounds.center.y) <= (viewHeight - bounds.size.y) * .5f + .001f;
            bool fitChanged = !_hasLoggedBossMapFit || !Mathf.Approximately(_lastLoggedAspect, aspect) ||
                !Mathf.Approximately(_lastLoggedSize, _camera.orthographicSize) ||
                (_lastLoggedBounds.center - bounds.center).sqrMagnitude > 0.0001f ||
                (_lastLoggedBounds.size - bounds.size).sqrMagnitude > 0.0001f;
            if (fitChanged)
            {
                Debug.Log($"[Stage5MapCamera] mapBounds={bounds.size.x:F2}x{bounds.size.y:F2} " +
                    $"mapCenter=({bounds.center.x:F2},{bounds.center.y:F2}); cameraCenter={transform.position}; " +
                    $"cameraView={viewWidth:F2}x{viewHeight:F2}; aspect={aspect:F3}; " +
                    $"fullyVisible={fullyVisible}; fixedZoom=2x; size={_camera.orthographicSize:F2}; actor positions fixed.");
                _lastLoggedAspect = aspect;
                _lastLoggedSize = _camera.orthographicSize;
                _lastLoggedBounds = bounds;
                _hasLoggedBossMapFit = true;
            }
        }
        float BossHudMinimumCenterY()
        {
            if (_bossVisual == null) _bossVisual = FindAnyObjectByType<OctopusBossVisual>();
            if (_bossHud == null)
            {
                var hud = GameObject.Find("BossBar");
                if (hud != null) _bossHud = hud.GetComponent<RectTransform>();
            }
            if (_bossVisual == null || _bossVisual.BodyRenderer == null || _bossHud == null ||
                !_bossHud.gameObject.activeInHierarchy || _camera.pixelHeight <= 0) return OctopusBossPattern.OverviewCenter.y;
            var canvas = _bossHud.GetComponentInParent<Canvas>();
            if (canvas == null) return OctopusBossPattern.OverviewCenter.y;
            _bossHud.GetWorldCorners(_hudCorners);
            float hudBottom = float.PositiveInfinity;
            foreach (var corner in _hudCorners)
                hudBottom = Mathf.Min(hudBottom, RectTransformUtility.WorldToScreenPoint(
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, corner).y);
            float safeTop = (hudBottom - 12f - _camera.pixelRect.yMin) / _camera.pixelHeight;
            if (safeTop <= .5f) return OctopusBossPattern.OverviewCenter.y;
            safeTop = Mathf.Min(1f, safeTop);
            // Use the resting portrait top, so vulnerability poses do not move the camera.
            float bodyTop = OctopusBossPattern.Head.y - FiniteBeach.Height * .14f +
                _bossVisual.BodyRenderer.bounds.extents.y;
            return bodyTop - (2f * safeTop - 1f) * BossOrthographicSize;
        }
    }
}



