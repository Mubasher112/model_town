using UnityEngine;

namespace Game.Camera
{
    using Game.Input;
    using Game.World;

    public class MobileCameraController : MonoBehaviour
    {
        [Header("Zoom Settings")]
        [SerializeField] private float minZoom = 3f;
        [SerializeField] private float maxZoom = 15f;
        [SerializeField] private float currentZoom = 8f;
        [SerializeField] private float zoomSpeed = 0.05f;

        [Header("Pan Settings")]
        [SerializeField] private float panSpeed = 0.01f;
        [SerializeField] private Vector2 minBounds = new Vector2(-20f, -20f);
        [SerializeField] private Vector2 maxBounds = new Vector2(20f, 20f);

        [Header("Damping")]
        [SerializeField] private float smoothTime = 0.1f;

        private Vector3 _targetPosition;

        public void SetBoundsFromGrid(WorldGrid grid)
        {
            if (grid == null) return;
            Vector3 minWorld = grid.GridToWorld(new Vector2Int(0, 0));
            Vector3 maxWorld = grid.GridToWorld(new Vector2Int(grid.Width - 1, grid.Height - 1));

            minBounds = new Vector2(Mathf.Min(minWorld.x, maxWorld.x) - 5f, Mathf.Min(minWorld.y, maxWorld.y) - 5f);
            maxBounds = new Vector2(Mathf.Max(minWorld.x, maxWorld.x) + 5f, Mathf.Max(minWorld.y, maxWorld.y) + 5f);
        }

        public void SetCustomBounds(Vector2 min, Vector2 max)
        {
            minBounds = min;
            maxBounds = max;
            ClampTargetPosition();
        }

        private void Start()
        {
            _targetPosition = transform.position;
            if (MobileInputManager.Instance != null)
            {
                MobileInputManager.Instance.OnDrag += HandleDrag;
                MobileInputManager.Instance.OnPinchZoom += HandleZoom;
            }
        }

        private void OnDestroy()
        {
            if (MobileInputManager.Instance != null)
            {
                MobileInputManager.Instance.OnDrag -= HandleDrag;
                MobileInputManager.Instance.OnPinchZoom -= HandleZoom;
            }
        }

        public void HandleDrag(Vector2 delta)
        {
            Vector3 move = new Vector3(-delta.x * panSpeed * (currentZoom / 5f), -delta.y * panSpeed * (currentZoom / 5f), 0f);
            _targetPosition += move;
            ClampTargetPosition();
        }

        public void HandleZoom(float delta)
        {
            currentZoom -= delta * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            ClampTargetPosition();
        }

        private void ClampTargetPosition()
        {
            _targetPosition.x = Mathf.Clamp(_targetPosition.x, minBounds.x, maxBounds.x);
            _targetPosition.y = Mathf.Clamp(_targetPosition.y, minBounds.y, maxBounds.y);
        }

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * (1f / smoothTime));
        }

        public float GetCurrentZoom() => currentZoom;
        public Vector3 GetTargetPosition() => _targetPosition;
    }
}
