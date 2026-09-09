using System;
using UnityEngine;

namespace Game.Input
{
    public enum InputGestureType
    {
        None,
        Tap,
        LongPress,
        Drag,
        PinchZoom
    }

    public class MobileInputManager : MonoBehaviour
    {
        public static MobileInputManager Instance { get; private set; }

        public event Action<Vector2> OnTap;
        public event Action<Vector2> OnLongPress;
        public event Action<Vector2> OnDrag;
        public event Action<float> OnPinchZoom;

        [SerializeField] private float tapThresholdDistance = 10f;
        [SerializeField] private float longPressTime = 0.5f;

        private Vector2 _touchStartPosition;
        private float _touchStartTime;
        private bool _isDragging;
        private bool _hasTriggeredLongPress;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Update()
        {
            ProcessTouchInput();
#if UNITY_EDITOR || !UNITY_ANDROID && !UNITY_IOS
            ProcessMouseFallbackInput();
#endif
        }

        private void ProcessTouchInput()
        {
            if (UnityEngine.Input.touchCount == 1)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    _touchStartPosition = touch.position;
                    _touchStartTime = Time.time;
                    _isDragging = false;
                    _hasTriggeredLongPress = false;
                }
                else if (touch.phase == TouchPhase.Moved)
                {
                    float deltaDist = (touch.position - _touchStartPosition).magnitude;
                    if (deltaDist > tapThresholdDistance)
                    {
                        _isDragging = true;
                        OnDrag?.Invoke(touch.deltaPosition);
                    }
                }
                else if (touch.phase == TouchPhase.Stationary)
                {
                    if (!_isDragging && !_hasTriggeredLongPress && Time.time - _touchStartTime >= longPressTime)
                    {
                        _hasTriggeredLongPress = true;
                        OnLongPress?.Invoke(touch.position);
                    }
                }
                else if (touch.phase == TouchPhase.Ended)
                {
                    if (!_isDragging && !_hasTriggeredLongPress)
                    {
                        OnTap?.Invoke(touch.position);
                    }
                }
            }
            else if (UnityEngine.Input.touchCount == 2)
            {
                Touch touch0 = UnityEngine.Input.GetTouch(0);
                Touch touch1 = UnityEngine.Input.GetTouch(1);

                Vector2 prevPos0 = touch0.position - touch0.deltaPosition;
                Vector2 prevPos1 = touch1.position - touch1.deltaPosition;

                float prevTouchDeltaMag = (prevPos0 - prevPos1).magnitude;
                float touchDeltaMag = (touch0.position - touch1.position).magnitude;

                float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;
                OnPinchZoom?.Invoke(deltaMagnitudeDiff);
            }
        }

        private void ProcessMouseFallbackInput()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                _touchStartPosition = UnityEngine.Input.mousePosition;
                _touchStartTime = Time.time;
                _isDragging = false;
                _hasTriggeredLongPress = false;
            }
            else if (UnityEngine.Input.GetMouseButton(0))
            {
                Vector2 currentMousePos = UnityEngine.Input.mousePosition;
                float dist = (currentMousePos - _touchStartPosition).magnitude;

                if (dist > tapThresholdDistance)
                {
                    _isDragging = true;
                    Vector2 delta = new Vector2(UnityEngine.Input.GetAxis("Mouse X") * 20f, UnityEngine.Input.GetAxis("Mouse Y") * 20f);
                    OnDrag?.Invoke(delta);
                }
                else if (!_hasTriggeredLongPress && Time.time - _touchStartTime >= longPressTime)
                {
                    _hasTriggeredLongPress = true;
                    OnLongPress?.Invoke(currentMousePos);
                }
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                if (!_isDragging && !_hasTriggeredLongPress)
                {
                    OnTap?.Invoke(UnityEngine.Input.mousePosition);
                }
            }

            float scroll = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                OnPinchZoom?.Invoke(scroll * 100f);
            }
        }
    }
}
