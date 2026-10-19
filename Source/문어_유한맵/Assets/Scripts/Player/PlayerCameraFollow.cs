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
        public static void StrongImpact(Vector2 point)
        {
            if (OctopusPlayer.Local != null && Vector2.Distance(OctopusPlayer.Local.Pos, point) < 10f)
                shakeUntil = Mathf.Max(shakeUntil, Time.time + .1f);
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
            if (screenAspect > FiniteBeach.Aspect)
            {
                float w = FiniteBeach.Aspect / screenAspect;
                _camera.rect = new Rect((1-w)*.5f, 0, w, 1);
            }
            else
            {
                float h = screenAspect / FiniteBeach.Aspect;
                _camera.rect = new Rect(0, (1-h)*.5f, 1, h);
            }
            _camera.aspect = FiniteBeach.Aspect;
            var session = GameSession.Instance;
            bool octopusBattle = session != null && session.OctopusBossActive;
            float zoom = octopusBattle ? OctopusBossPattern.OverviewSize : GameConfig.CameraSize;
            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, zoom, 1f - Mathf.Exp(-5f * Time.deltaTime));
            if (octopusBattle)
            {
                var center = OctopusBossPattern.OverviewCenter;
                var overview = new Vector3(center.x, center.y, transform.position.z);
                transform.position = Vector3.Lerp(transform.position, overview, 1f - Mathf.Exp(-5f * Time.deltaTime));
                return;
            }
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
    }
}



