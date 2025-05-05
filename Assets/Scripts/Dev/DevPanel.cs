using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ClickClick.Manager;
using System.Collections;
namespace ClickClick.Dev
{
    public class DevPanel : SingletonManager<DevPanel>
    {
        [SerializeField] private GameObject _devPanel;
        [Header("Buttons")]
        [SerializeField] private Button ScreenSaverButton;
        [SerializeField] private Button MenuButton;
        [SerializeField] private Button ResetButton;

        [Header("Goal Score")]
        [SerializeField] private TMP_InputField _goalScoreInput;
        [SerializeField] private Button _updateGoalScoreButton;
        [SerializeField] private TextMeshProUGUI _currentGoalScoreText;

        [Header("Message")]
        [SerializeField] private GameObject _messagePanel;
        [SerializeField] private TextMeshProUGUI _messageText;

        [Header("Audio Settings")]
        [SerializeField] private Button _muteBgmButton;
        [SerializeField] private Button _unmuteBgmButton;

        bool _enableClose = true;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && _enableClose)
            {
                RevertPnaelStatus();
            }
        }

        void Start()
        {
            ScreenSaverButton.onClick.AddListener(OnScreenSaverButtonClick);
            MenuButton.onClick.AddListener(OnMenuButtonClick);
            ResetButton.onClick.AddListener(OnResetButtonClick);
            _muteBgmButton.onClick.AddListener(OnMuteBgmButtonClick);
            _unmuteBgmButton.onClick.AddListener(OnUnmuteBgmButtonClick);
            _updateGoalScoreButton.onClick.AddListener(OnUpdateGoalScoreButtonClick);

            // Initialize goal score input with current value
            _goalScoreInput.text = DataManager.Instance.GoalScore.ToString();
            _currentGoalScoreText.text = $"目前目標分數: {DataManager.Instance.GoalScore}";
        }

        private void OnMuteBgmButtonClick()
        {
            AudioManager.UpdateMusicVolume(0);
        }

        private void OnUnmuteBgmButtonClick()
        {
            AudioManager.UpdateMusicVolume(AudioManager.Instance.bgmVolume);
        }

        private void OnScreenSaverButtonClick()
        {
            StartCoroutine(ChangeSceneCoroutine("Standby"));
        }

        private void OnMenuButtonClick()
        {
            StartCoroutine(ChangeSceneCoroutine("Menu"));
        }

        private IEnumerator ChangeSceneCoroutine(string sceneName)
        {
            _enableClose = false;

            _messageText.text = "場景轉換中...";
            _messagePanel.SetActive(true);

            yield return new WaitForSeconds(1f);

            SceneTransition.Instance.TransitionToScene(sceneName);

            RevertPnaelStatus();

            _enableClose = true;
        }

        private void OnResetButtonClick()
        {
            StartCoroutine(ResetCoroutine());
        }

        private IEnumerator ResetCoroutine()
        {
            _enableClose = false;

            _messageText.text = "資料重置中...";
            _messagePanel.SetActive(true);

            // Clear all PlayerPrefs data
            PlayerPrefs.DeleteAll();

            // Reset DataManager
            DataManager.Instance.ResetAllData();

            yield return new WaitForSeconds(3f);

            SceneTransition.Instance.TransitionToScene("Menu");

            RevertPnaelStatus();

            _enableClose = true;
        }

        private void OnUpdateGoalScoreButtonClick()
        {
            if (int.TryParse(_goalScoreInput.text, out int newGoalScore))
            {
                DataManager.Instance.GoalScore = newGoalScore;
                _messageText.text = $"目標分數已更新為: {newGoalScore}";
                _messagePanel.SetActive(true);
                StartCoroutine(HideMessageAfterDelay(2f));
            }
            else
            {
                _messageText.text = "請輸入有效的數字";
                _messagePanel.SetActive(true);
                StartCoroutine(HideMessageAfterDelay(2f));
            }

            _goalScoreInput.text = "";
            _currentGoalScoreText.text = $"目前目標分數: {DataManager.Instance.GoalScore}";
        }

        private IEnumerator HideMessageAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _messagePanel.SetActive(false);
            _messageText.text = "";
        }

        private void RevertPnaelStatus()
        {
            _messagePanel.SetActive(false);
            _messageText.text = "";
            _devPanel.SetActive(!_devPanel.activeSelf);
        }
    }
}