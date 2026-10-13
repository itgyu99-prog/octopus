using UnityEngine;

namespace OctopusSurvivors
{
    public sealed class PlayerCameraFollow : MonoBehaviour
    {
        public Transform Target;
        public float Smooth = 10f;

        Camera _camera;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera != null && _camera.orthographic)
                _camera.orthographicSize = GameConfig.CameraSize;
        }

        void LateUpdate()
        {
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
            if (Target == null)
                return;

            Vector3 dest = FiniteBeach.ClampCamera(Target.position, _camera);
            dest.z = transform.position.z;
            float t = 1f - Mathf.Exp(-Smooth * Time.deltaTime);
            transform.position = FiniteBeach.ClampCamera(Vector3.Lerp(transform.position, dest, t), _camera);
        }
    }
}



