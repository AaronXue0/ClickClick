using UnityEngine;
using UnityEngine.UI;
using ClickClick.GestureTracking;

namespace ClickClick.Gameplay
{
    public class FixObject : MonoBehaviour
    {
        [SerializeField] private Sprite _emptySprite;

        private HandGesture _gesture;
        private Image image;

        private Sprite _fixedSprite;
        private bool _isBeingFixed = false;
        private float _fixingTimer = 0f;
        private const float _fixingDuration = 1.5f;
        private Tool _currentTool; // Reference to the tool that's fixing this object

        private void Awake()
        {
            image = GetComponentInChildren<Image>();
        }

        public void AssignGesture(HandGesture gesture, Sprite sprite, Sprite fixedSprite)
        {
            if (_gesture != HandGesture.None)
                return;

            _gesture = gesture;
            image.sprite = sprite;
            _fixedSprite = fixedSprite;
        }

        public void TryFix(HandGesture gesture, Tool tool)
        {
            if (_gesture == HandGesture.None)
                return;

            if (_gesture == gesture)
            {
                if (!_isBeingFixed)
                {
                    _currentTool = tool;
                    StartFixing();
                }
            }
        }

        public void StartFixing()
        {
            _isBeingFixed = true;
            _fixingTimer = 0f;
        }

        public void ContinueFixing(float deltaTime)
        {
            if (!_isBeingFixed) return;

            _fixingTimer += deltaTime;

            // Complete fixing if timer exceeds duration
            if (_fixingTimer >= _fixingDuration)
            {
                ObjectFixed();
            }
        }

        public void CancelFixing()
        {
            if (_isBeingFixed)
            {
                _isBeingFixed = false;
                _fixingTimer = 0f;
                _currentTool = null;
            }
        }

        public void ObjectFixed()
        {
            GameManager.Instance.FixedGesture(_gesture);

            _isBeingFixed = false;
            _fixingTimer = 0f;

            image.sprite = _fixedSprite;

            // Notify the tool that the object is fixed
            if (_currentTool != null)
            {
                _currentTool.ObjectFixed();
                _currentTool = null;
            }

            Invoke(nameof(Reset), 1.25f);
        }

        public void Reset()
        {
            image.sprite = _emptySprite;
            _gesture = HandGesture.None;
            _isBeingFixed = false;
            _fixingTimer = 0f;
        }

        public bool HasGesture => _gesture != HandGesture.None;
        public bool IsBeingFixed => _isBeingFixed;
    }
}