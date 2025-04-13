using UnityEngine;
using UnityEngine.UI;
using ClickClick.GestureTracking;

namespace ClickClick.Gameplay
{
    public class Tool : MonoBehaviour
    {
        [SerializeField] HandGesture _gesture;
        [SerializeField] private AnimationClip _fixingAnimation;
        [SerializeField] private AnimationClip _idleAnimation;

        private Image _image;
        private RectTransform _rectTransform;
        private Animator _animator;
        private FixObject _currentFixObject;

        void Awake()
        {
            _image = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();
            _animator = GetComponent<Animator>();
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
                            PlayIdleAnimation();
                        }

                        _currentFixObject = fixObject;
                        fixObject.TryFix(_gesture);

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
                PlayIdleAnimation();
            }
        }

        private void PlayFixingAnimation()
        {
            if (_animator != null && _fixingAnimation != null)
            {
                // Play the animation directly
                _animator.Play(_fixingAnimation.name);
            }
        }

        private void PlayIdleAnimation()
        {
            if (_animator != null && _idleAnimation != null)
            {
                // Play the animation directly
                _animator.Play(_idleAnimation.name);
            }
        }

        public void SetGesture(HandGesture gesture)
        {
            _gesture = gesture;
        }
    }
}