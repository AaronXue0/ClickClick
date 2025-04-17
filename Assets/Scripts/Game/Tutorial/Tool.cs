using UnityEngine;
using UnityEngine.UI;
using ClickClick.GestureTracking;
using DG.Tweening;
using ClickClick.Tutorial;

namespace ClickClick.Tutorial
{
    public class Tool : MonoBehaviour
    {
        [SerializeField] HandGesture _gesture;
        [SerializeField] Sprite _originalSprite;
        [SerializeField] Sprite _fixingSprite;
        [SerializeField] float _spriteSwapInterval = 0.15f;
        [SerializeField] float _scaleDownDuration = 1f;
        [SerializeField] float _scaleDownAmount = 0.7f;

        // [SerializeField] private AudioController _audioController;

        public bool ImageEnabled => _image.enabled;

        private Image _image;
        private RectTransform _rectTransform;
        private Sequence _spriteSwapSequence;
        private Vector3 _originalScale;
        private bool _isAnimating = false;

        void Awake()
        {
            _image = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();
            _originalScale = transform.localScale;
            Time.timeScale = 1;
        }

        public RectTransform GetRectTransform()
        {
            return _rectTransform;
        }

        public void PlayFixingAnimation()
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
            _spriteSwapSequence.AppendCallback(() =>
            {
                _image.sprite = _fixingSprite;
            })
                .AppendInterval(_spriteSwapInterval)
                .AppendCallback(() =>
                {
                    _image.sprite = _originalSprite;
                })
                .AppendInterval(_spriteSwapInterval)
                .SetLoops(-1); // Infinite loop
        }

        public void StopFixingAnimation()
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
                    // Hide the image
                    // _image.enabled = false;

                    // Wait for a second then restore original scale and show the image
                    DOVirtual.DelayedCall(1f, () =>
                    {
                        // _image.enabled = true;
                        transform.DOScale(_originalScale, 0f);
                    });
                });
        }

        public void SetGesture(HandGesture gesture)
        {
            _gesture = gesture;
        }

        public HandGesture GetGesture()
        {
            return _gesture;
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