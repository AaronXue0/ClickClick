using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ClickClick.Manager;
namespace ClickClick.Dev
{
    public class DevPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _devPanel;
        [Header("Buttons")]
        [SerializeField] private Button ScreenSaverButton;
        [SerializeField] private Button MenuButton;
        [SerializeField] private Button ResetButton;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _devPanel.SetActive(!_devPanel.activeSelf);
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
            SceneTransition.Instance.TransitionToScene("Standby");
        }

        private void OnMenuButtonClick()
        {
            SceneTransition.Instance.TransitionToScene("Menu");
        }

        private void OnResetButtonClick()
        {
            PlayerPrefs.DeleteAll();
        }
    }
}