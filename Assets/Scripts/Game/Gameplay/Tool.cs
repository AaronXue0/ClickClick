using UnityEngine;
using UnityEngine.UI;
using ClickClick.GestureTracking;
using DG.Tweening;

namespace ClickClick.Gameplay
{
    public class Tool : MonoBehaviour
    {
        [SerializeField] HandGesture _gesture;
        [SerializeField] Sprite _originalSprite;
        [SerializeField] Sprite _fixingSprite;
        [SerializeField] float _spriteSwapInterval = 0.15f;
        [SerializeField] float _scaleDownDuration = 1f;
        [SerializeField] float _scaleDownAmount = 0.7f;

        private Image _image;
        private RectTransform _rectTransform;
        private FixObject _currentFixObject;
        private Sequence _spriteSwapSequence;
        private Vector3 _originalScale;
        private bool _isAnimating = false;

        void Awake()
        {
            _image = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();
            _originalScale = transform.localScale;
        }

        void Update()
        {
            CheckOverlap();
        }

        private void CheckOverlap()
        {
            bool foundOverlap = false;
            // Find all FixObjects in the scene
            var fixObjects = GameManager.Instance.GetFixObjects;

            foreach (var fixObject in fixObjects)
            {
                if (!fixObject.HasGesture) continue;

                // Get the RectTransform of the FixObject
                var fixObjectRect = fixObject.GetComponent<RectTransform>();

                // Check if rectangles overlap
                if (RectTransformUtility.RectangleContainsScreenPoint(fixObjectRect, _rectTransform.position))
                {
                    foundOverlap = true;

                    // If we're not currently fixing this object, start fixing it
                    if (_currentFixObject != fixObject)
                    {
                        // If we were fixing another object, cancel that first
                        if (_currentFixObject != null)
                        {
                            _currentFixObject.CancelFixing();
                            StopFixingAnimation();
                        }

                        _currentFixObject = fixObject;
                        fixObject.TryFix(_gesture, this);

                        if (fixObject.IsBeingFixed)
                        {
                            PlayFixingAnimation();
                        }
                    }

                    // Continue fixing the current object
                    if (_currentFixObject != null && _currentFixObject.IsBeingFixed)
                    {
                        _currentFixObject.ContinueFixing(Time.deltaTime);
                    }

                    break;
                }
            }

            // If we're not overlapping with any fixObjects but we have a current one, cancel fixing
            if (!foundOverlap && _currentFixObject != null)
            {
                _currentFixObject.CancelFixing();
                _currentFixObject = null;
                StopFixingAnimation();
            }
        }

        private void PlayFixingAnimation()
        {
            if (_isAnimating) return;

            _isAnimating = true;

            // Kill any existing sequences
            if (_spriteSwapSequence != null)
            {
                _spriteSwapSequence.Kill();
                _spriteSwapSequence = null;
            }

            // Create a new sequence for sprite swapping
            _spriteSwapSequence = DOTween.Sequence();

            // Add sprite swap callbacks
            _spriteSwapSequence.AppendCallback(() => _image.sprite = _fixingSprite)
                .AppendInterval(_spriteSwapInterval)
                .AppendCallback(() => _image.sprite = _originalSprite)
                .AppendInterval(_spriteSwapInterval)
                .SetLoops(-1); // Infinite loop
        }

        private void StopFixingAnimation()
        {
            _isAnimating = false;

            // Kill the sequence if it exists
            if (_spriteSwapSequence != null)
            {
                _spriteSwapSequence.Kill();
                _spriteSwapSequence = null;
            }

            // Reset to original sprite
            _image.sprite = _originalSprite;
        }

        public void ObjectFixed()
        {
            StopFixingAnimation();

            // Scale down animation
            transform.DOScale(_originalScale * _scaleDownAmount, _scaleDownDuration)
                .OnComplete(() =>
                {
                    // Return to original scale
                    transform.DOScale(_originalScale, 0.3f);
                });
        }

        public void SetGesture(HandGesture gesture)
        {
            _gesture = gesture;
        }

        private void OnDestroy()
        {
            // Clean up any active tweens when the object is destroyed
            if (_spriteSwapSequence != null)
            {
                _spriteSwapSequence.Kill();
                _spriteSwapSequence = null;
            }
            DOTween.Kill(transform);
        }
    }
}