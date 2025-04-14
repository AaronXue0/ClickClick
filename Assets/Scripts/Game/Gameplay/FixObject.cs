using UnityEngine;
using UnityEngine.UI;
using ClickClick.GestureTracking;
using System.Collections;
using TMPro;
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
        private const float _fixingDuration = 0.1f;
        private Tool _currentTool; // Reference to the tool that's fixing this object
        private bool _isFixed = false;

        [SerializeField] private TextMeshProUGUI _scoreText;

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
            if (_gesture == HandGesture.None || _isFixed)
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

            // Verify that the tool still has the correct gesture
            if (_currentTool != null && _currentTool.GetGesture() != _gesture)
            {
                CancelFixing();
                return;
            }

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
            GameManager.Instance.FixedGesture(_gesture, this);

            _isBeingFixed = false;
            _fixingTimer = 0f;

            image.sprite = _fixedSprite;
            _isFixed = true;

            // Notify the tool that the object is fixed
            if (_currentTool != null)
            {
                _currentTool.ObjectFixed();
                _currentTool = null;
            }

            Invoke(nameof(Reset), 1.25f);
        }

        public void ShowScoreAnimation(int score)
        {
            _scoreText.text = "+" + score.ToString();
            _scoreText.gameObject.SetActive(true);
            _scoreText.color = new Color(_scoreText.color.r, _scoreText.color.g, _scoreText.color.b, 1f);
            _scoreText.rectTransform.anchoredPosition = Vector2.zero; // Start at center

            StartCoroutine(AnimateScoreText());
        }

        private IEnumerator AnimateScoreText()
        {
            float duration = 1.0f;
            float elapsedTime = 0f;
            Vector2 startPosition = _scoreText.rectTransform.anchoredPosition;
            Vector2 endPosition = startPosition + new Vector2(0, 100f); // Move up 100 units

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / duration;

                // Move up
                _scoreText.rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, normalizedTime);

                // Fade out in the second half of the animation
                if (normalizedTime > 0.5f)
                {
                    float alpha = 1 - ((normalizedTime - 0.5f) * 2); // 1 to 0 in the second half
                    _scoreText.color = new Color(_scoreText.color.r, _scoreText.color.g, _scoreText.color.b, alpha);
                }

                yield return null;
            }

            // Hide text after animation
            _scoreText.gameObject.SetActive(false);
        }

        public void Reset()
        {
            image.sprite = _emptySprite;
            _gesture = HandGesture.None;
            _isBeingFixed = false;
            _fixingTimer = 0f;
            _isFixed = false;
        }

        public bool HasGesture => _gesture != HandGesture.None;
        public bool IsBeingFixed => _isBeingFixed;
        public bool IsFixed => _isFixed;
    }
}