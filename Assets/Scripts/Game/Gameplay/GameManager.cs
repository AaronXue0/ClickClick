using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClickClick.GestureTracking;
using TMPro;
using Mediapipe;
using UnityEngine.Events;
using DG.Tweening;
using ClickClick.Manager;

namespace ClickClick.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Time")]
        [SerializeField] private float _time = 60f;
        [SerializeField] private UnityEngine.UI.Image _timeImage;

        [Header("Score")]
        [SerializeField] private int _scoreObtained = 100;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private List<int> _scoreMultipliers = new List<int> { 2, 3, 4 };

        [Header("Fix Objects")]
        [SerializeField] private AudioController _scoreUpSound;
        [SerializeField] private List<FixObject> _fixObjects = new List<FixObject>();

        [Header("Level Generation")]
        private float _initialSpawnInterval = 2.5f;
        private float _minimumSpawnInterval = 0.5f;
        private float _speedUpInterval = 10f;
        private float _speedUpMultiplier = 0.7f;

        [Header("Gesture Sprites")]
        [SerializeField] private Sprite _rockSprite;
        [SerializeField] private Sprite _paperSprite;
        [SerializeField] private Sprite _scissorsSprite;
        [SerializeField] private Sprite _rockSpriteFixed;
        [SerializeField] private Sprite _paperSpriteFixed;
        [SerializeField] private Sprite _scissorsSpriteFixed;

        [Header("Countdown")]
        [SerializeField] private TextMeshProUGUI _countdownText;
        [SerializeField] private float _countdownDuration = 3f;
        [SerializeField] private GameObject _countdownPanel;
        [SerializeField] private AudioSource _countdownSound;
        [SerializeField] private AudioSource _gameStartSound;

        [Header("Countdown Animation")]
        [SerializeField] private float _numberScaleMultiplier = 1.5f;
        [SerializeField] private float _numberScaleDuration = 0.5f;
        [SerializeField] private float _numberFadeDuration = 0.3f;
        [SerializeField] private Ease _numberScaleEase = Ease.OutBack;

        [Header("Game Over")]
        [SerializeField] private GameObject _gameOverPanel;

        public List<FixObject> GetFixObjects => _fixObjects;

        public System.Action onGameStart;
        public System.Action onGameOver;

        private float _timeLeft;
        private int _scores = 0;

        private int _strikes = 0;
        private HandGesture _lastFixedGesture = HandGesture.None;

        private float _currentSpawnInterval;
        private float _nextSpawnTime;

        private bool _isGameStarted = false;
        private bool _isGameOver = false;

        public bool IsGameOver => _isGameOver;

        private Sequence _countdownSequence;

        private float _nextSpeedUpTime;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(Instance);
            }

            Instance = this;
        }

        private void Start()
        {
            // Initialize game state
            _timeLeft = _time;
            _currentSpawnInterval = _initialSpawnInterval;
            _timeImage.fillAmount = 1;

            // Disable game elements during countdown
            _scoreText.gameObject.SetActive(false);
            _countdownPanel.SetActive(true);

            // Reset all fix objects
            foreach (var fixObject in _fixObjects)
            {
                fixObject.gameObject.SetActive(false);
            }

            // Start countdown
            Invoke(nameof(StartCountdown), 1f);
        }

        private void StartCountdown()
        {
            // Kill any existing sequence
            if (_countdownSequence != null)
            {
                _countdownSequence.Kill();
            }

            _countdownSequence = DOTween.Sequence();

            // Reset countdown text
            _countdownText.transform.localScale = Vector3.one;
            _countdownText.alpha = 1f;
            _countdownText.text = "";

            // Create countdown sequence
            for (int i = (int)_countdownDuration; i >= 1; i--)
            {
                int numberToShow = i;

                // Add number animation
                _countdownSequence
                    .AppendCallback(() =>
                    {
                        _countdownText.text = numberToShow.ToString();
                        _countdownText.transform.localScale = Vector3.one;
                        _countdownText.alpha = 1f;

                        if (_countdownSound != null)
                        {
                            _countdownSound.Play();
                        }
                    })
                    .Append(_countdownText.transform
                        .DOScale(Vector3.one * _numberScaleMultiplier, _numberScaleDuration * 0.4f)
                        .SetEase(_numberScaleEase))
                    .Append(_countdownText.transform
                        .DOScale(Vector3.one, _numberScaleDuration * 0.6f)
                        .SetEase(Ease.InBack))
                    .Join(_countdownText
                        .DOFade(0, _numberFadeDuration)
                        .SetEase(Ease.InQuad))
                    .AppendInterval(0.1f); // Small pause between numbers
            }

            // Add "GO!" animation
            _countdownSequence
                .AppendCallback(() =>
                {
                    _countdownText.text = "GO!";
                    _countdownText.transform.localScale = Vector3.one * 0.5f;
                    _countdownText.alpha = 1f;

                    if (_gameStartSound != null)
                    {
                        _gameStartSound.Play();
                    }
                })
                .Append(_countdownText.transform
                    .DOScale(Vector3.one * _numberScaleMultiplier * 1.2f, _numberScaleDuration * 0.3f)
                    .SetEase(_numberScaleEase))
                .Join(_countdownText
                    .DOFade(1f, _numberScaleDuration * 0.3f))
                .AppendInterval(0.2f)
                .Append(_countdownText.transform
                    .DOScale(Vector3.zero, _numberScaleDuration * 0.2f)
                    .SetEase(Ease.InBack))
                .Join(_countdownText
                    .DOFade(0f, _numberFadeDuration * 0.5f))
                .AppendCallback(() => StartGame());
        }

        private void StartGame()
        {
            // return;
            _isGameStarted = true;
            _countdownPanel.SetActive(false);
            _timeImage.gameObject.SetActive(true);
            _scoreText.gameObject.SetActive(true);

            // Enable all fix objects
            foreach (var fixObject in _fixObjects)
            {
                fixObject.gameObject.SetActive(true);
            }

            // Set initial spawn time
            _nextSpeedUpTime = _time - _speedUpInterval;
            _currentSpawnInterval = _initialSpawnInterval;
            _nextSpawnTime = Time.time + _currentSpawnInterval;

            SpawnRandomGesture();

            // Invoke start event
            onGameStart?.Invoke();
        }

        private void OnDestroy()
        {
            if (_countdownSequence != null)
            {
                _countdownSequence.Kill();
                _countdownSequence = null;
            }
        }

        private void Update()
        {
            if (!_isGameStarted || _isGameOver)
            {
                return;
            }

            // Original game update logic
            _timeLeft -= Time.deltaTime;
            _timeImage.fillAmount = _timeLeft / _time;

            // Check if it's time to speed up
            if (_timeLeft <= _nextSpeedUpTime)
            {
                _currentSpawnInterval = Mathf.Max(_minimumSpawnInterval, _currentSpawnInterval * _speedUpMultiplier);
                _nextSpeedUpTime -= _speedUpInterval;
                Debug.Log($"Speed up! New spawn interval: {_currentSpawnInterval}");
            }

            if (Time.time >= _nextSpawnTime)
            {
                SpawnRandomGesture();
                _nextSpawnTime = Time.time + _currentSpawnInterval;
            }

            if (_timeLeft <= 0)
            {
                GameOver();
            }
        }

        private void SpawnRandomGesture()
        {            // Find available fix objects
            List<FixObject> availableObjects = _fixObjects.FindAll(obj => !obj.HasGesture);

            if (availableObjects.Count == 0)
                return;

            // Select random object
            FixObject selectedObject = availableObjects[Random.Range(0, availableObjects.Count)];

            // Generate random gesture (1 = Rock, 2 = Paper, 3 = Scissors)
            HandGesture randomGesture = (HandGesture)Random.Range(1, 4);

            // Get corresponding sprite
            Sprite gestureSprite = randomGesture switch
            {
                HandGesture.Rock => _rockSprite,
                HandGesture.Paper => _paperSprite,
                HandGesture.Scissors => _scissorsSprite,
                _ => null
            };

            Sprite fixedSprite;
            switch (randomGesture)
            {
                case HandGesture.Rock:
                    fixedSprite = _rockSpriteFixed;
                    break;
                case HandGesture.Paper:
                    fixedSprite = _paperSpriteFixed;
                    break;
                case HandGesture.Scissors:
                    fixedSprite = _scissorsSpriteFixed;
                    break;
                default:
                    fixedSprite = null;
                    break;
            }

            // Assign gesture and sprite
            selectedObject.AssignGesture(randomGesture, gestureSprite, fixedSprite);
        }

        public void FixedGesture(HandGesture gesture)
        {
            if (gesture == HandGesture.None)
                return;

            int score = _scoreObtained;

            if (gesture == _lastFixedGesture)
            {
                _strikes++;

                switch (_strikes)
                {
                    case 1:
                        break;
                    case 2:
                        score *= _scoreMultipliers[0];
                        break;
                    case 3:
                        score *= _scoreMultipliers[1];
                        break;
                    default:
                        score *= _scoreMultipliers[2];
                        break;
                }
            }
            else
            {
                _strikes = 0;
            }

            Debug.Log(_scores);

            _scores += score;
            _scoreUpSound.DoAction();
            _lastFixedGesture = gesture;

            _scoreText.text = "分數: " + _scores.ToString();
        }

        private void GameOver()
        {
            if (_isGameOver) return;

            _isGameOver = true;
            enabled = false;

            // Disable all fix objects
            foreach (var fixObject in _fixObjects)
            {
                fixObject.gameObject.SetActive(false);
            }

            // Show game over performance
            ShowGameOverPerformance();

            // Notify listeners that game is over
            onGameOver?.Invoke();

            DataManager.Instance.UpdateCurrentPlayerScore(_scores);
        }

        private void ShowGameOverPerformance()
        {
            // Kill any existing sequence
            if (_countdownSequence != null)
            {
                _countdownSequence.Kill();
            }

            DataManager.Instance.AddScoreToCurrentPlayer(_scores);

            _countdownSequence = DOTween.Sequence();

            // Show countdown panel
            _countdownPanel.SetActive(true);
            _countdownText.transform.localScale = Vector3.zero;
            _countdownText.alpha = 0f;

            // Create game over sequence
            _countdownSequence
                .AppendCallback(() =>
                {
                    _countdownText.text = "GAME OVER";
                    if (_gameStartSound != null)
                    {
                        _gameStartSound.Play();
                    }
                })
                .Append(_countdownText.transform
                    .DOScale(Vector3.one * _numberScaleMultiplier, _numberScaleDuration)
                    .SetEase(_numberScaleEase))
                .Join(_countdownText
                    .DOFade(1f, _numberFadeDuration))
                .AppendInterval(1f)
                .Append(_countdownText.transform
                    .DOScale(Vector3.one * 0.8f, _numberScaleDuration)
                    .SetEase(Ease.InOutBack))
                .AppendCallback(() =>
                {
                    _countdownText.text = $"分數: {_scores}";
                    if (_countdownSound != null)
                    {
                        _countdownSound.Play();
                    }
                })
                .Append(_countdownText.transform
                    .DOScale(Vector3.one * _numberScaleMultiplier, _numberScaleDuration)
                    .SetEase(_numberScaleEase))
                .AppendInterval(0.5f)
                .OnComplete(() =>
                {
                    // Show game over UI after the performance
                    if (_gameOverPanel != null)
                    {
                        _gameOverPanel.SetActive(true);
                    }

                    // Wait 1.5 seconds then transition to RankList scene
                    StartCoroutine(TransitionToRankList());
                });
        }

        private IEnumerator TransitionToRankList()
        {
            yield return new WaitForSeconds(1.5f);
            SceneTransition.Instance.TransitionToScene("RankList");
        }

        public void RestartGame()
        {
            // Kill any existing sequence first
            if (_countdownSequence != null)
            {
                _countdownSequence.Kill();
            }

            _isGameOver = false;
            enabled = true;

            // Reset game state
            _timeLeft = _time;
            _scores = 0;
            _strikes = 0;
            _lastFixedGesture = HandGesture.None;
            _currentSpawnInterval = _initialSpawnInterval;

            // Hide UI panels
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }
            _countdownPanel.SetActive(false);

            // Reset score display
            if (_scoreText != null)
            {
                _scoreText.text = "0";
            }

            // Start new game
            StartCountdown();
        }
    }
}