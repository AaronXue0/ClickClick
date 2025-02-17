using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ClickClick.Manager;
using System.Collections;
namespace ClickClick.Dev
{
    public class DevPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _devPanel;
        [Header("Buttons")]
        [SerializeField] private Button ScreenSaverButton;
        [SerializeField] private Button MenuButton;
        [SerializeField] private Button ResetButton;

        [Header("Message")]
        [SerializeField] private GameObject _messagePanel;
        [SerializeField] private TextMeshProUGUI _messageText;

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

        private void RevertPnaelStatus()
        {
            _messagePanel.SetActive(false);
            _messageText.text = "";
            _devPanel.SetActive(!_devPanel.activeSelf);
        }
    }
}