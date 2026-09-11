using UnityEngine;

namespace Game.UI
{
    public class SafeAreaHandler : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea = new Rect(0, 0, 0, 0);

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            RefreshSafeArea();
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea)
            {
                RefreshSafeArea();
            }
        }

        private void RefreshSafeArea()
        {
            _lastSafeArea = Screen.safeArea;
            if (_rectTransform == null) return;

            Vector2 anchorMin = _lastSafeArea.position;
            Vector2 anchorMax = _lastSafeArea.position + _lastSafeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
        }
    }
}
