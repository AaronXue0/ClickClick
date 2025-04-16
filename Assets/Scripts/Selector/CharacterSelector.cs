using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using DG.Tweening;
using ClickClick.Data;
using Mediapipe.Unity.Sample.HandLandmarkDetection;

namespace ClickClick.Tool
{
    [System.Serializable]
    public class CharacterButtonData
    {
        public int id;
        public Button characterButton;
        public Image characterImage;
        public Image progressImage;
        public TMP_Text characterNameText;
    }

    public class CharacterSelector : CircularProgressOnHold
    {
        [Header("Character Selection")]
        [SerializeField] private Sprite defaultPreviewSprite;
        [SerializeField] private List<CharacterButtonData> characters = new List<CharacterButtonData>();
        [SerializeField] private Image previewImage;
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private RectTransform screenshotArea;
        [SerializeField] private CharacterGroup characterGroup;
        [SerializeField] private TextMeshProUGUI hintText;

        [Header("Story")]
        [SerializeField] private BeginningStory beginningStory;
        [SerializeField] private HandLandmarkerSelector handLandmarkSelector;

        private CharacterButtonData currentTarget;
        private CharacterButtonData selectedCharacter;
        private Dictionary<Button, Vector3> originalButtonScales = new Dictionary<Button, Vector3>();

        protected override float ProgressFillAmount
        {
            get => currentTarget?.progressImage.fillAmount ?? 0f;
            set
            {
                if (currentTarget != null)
                {
                    currentTarget.progressImage.fillAmount = value;
                }
            }
        }

        protected override void InitializeTargetButton()
        {
            UpdatePreview(null);
            foreach (var character in characters)
            {
                originalButtonScales[character.characterButton] = character.characterButton.transform.localScale;
                var characterData = characterGroup.GetCharacterData(character.id);
                character.characterImage.sprite = characterData.characterSprite;
                character.characterNameText.text = characterData.characterName;
            }
        }

        protected override void HandleProgressComplete()
        {
            if (currentTarget != null)
            {
                // Fetch CharacterData using ID
                var characterData = characterGroup.GetCharacterData(currentTarget.id);
                selectedCharacter = currentTarget;

                // Update preview
                previewImage.sprite = characterData.characterSprite;
                characterNameText.text = characterData.characterName;

                // Save the selected character to DataManager
                if (Manager.DataManager.Instance != null &&
                    Manager.DataManager.Instance.GetCurrentPlayer() != null)
                {
                    Manager.DataManager.Instance.GetCurrentPlayer().CharacterId = characterData.characterId;
                    Debug.Log("Selected character: " + characterData.characterName);
                }

                // Trigger the button click
                currentTarget.characterButton.onClick.Invoke();

                // Determine which scene to transition to based on character ID
                string targetScene = characterData.characterId == 0 ? "Tutorial" : sceneToTransitionTo;

                // Transition to the appropriate scene after a short delay
                StartCoroutine(TransitionAfterDelay(targetScene));

                hintText.gameObject.SetActive(true);
            }
        }

        private IEnumerator TransitionAfterDelay(string sceneName)
        {
            yield return new WaitForSeconds(0f);

            _sceneName = sceneName;

            UpdatePreview(selectedCharacter);
            ResetProgress();
            isCompleted = false;
        }

        private string _sceneName;

        private void FixedUpdate()
        {
            if (selectedCharacter != null && Input.GetKey(KeyCode.Space))
            {
                isCompleted = true;
                ResetProgress();
                SceneTransition.Instance.TransitionToScene(_sceneName);
            }
        }

        protected override bool IsOverlappingTargetButton(GameObject gestureObject)
        {
            if (gestureObject == null || gameObject.activeSelf == false)
            {
                return false;
            }

            RectTransform gestureRect = gestureObject.GetComponent<RectTransform>();
            CharacterButtonData previousTarget = currentTarget;

            foreach (var character in characters)
            {
                RectTransform buttonRect = character.characterButton.GetComponent<RectTransform>();

                if (gestureRect != null && buttonRect != null)
                {
                    Vector3[] gestureCorners = new Vector3[4];
                    Vector3[] buttonCorners = new Vector3[4];

                    gestureRect.GetWorldCorners(gestureCorners);
                    buttonRect.GetWorldCorners(buttonCorners);

                    Rect gestureRectangle = new Rect(gestureCorners[0].x, gestureCorners[0].y,
                        gestureCorners[2].x - gestureCorners[0].x, gestureCorners[2].y - gestureCorners[0].y);

                    Rect buttonRectangle = new Rect(buttonCorners[0].x, buttonCorners[0].y,
                        buttonCorners[2].x - buttonCorners[0].x, buttonCorners[2].y - buttonCorners[0].y);

                    if (gestureRectangle.Overlaps(buttonRectangle))
                    {
                        Debug.Log("IsOverlappingTargetButton");
                        if (previousTarget != null && previousTarget != character)
                        {
                            ResetProgress();
                        }

                        if (character.id != selectedCharacter?.id)
                        {
                            currentTarget = character;
                            return true;
                        }
                        else if (selectedCharacter == null)
                        {
                            currentTarget = character;
                            return true;
                        }

                        // UpdatePreview(character);
                    }
                }
            }

            currentTarget = null;
            // UpdatePreview(null);
            return false;
        }

        protected override void UpdateTargetButtonScale(bool isOverlapping)
        {
            // Reset all buttons to original scale except the current target
            foreach (var character in characters)
            {
                if (character != currentTarget)
                {
                    character.characterButton.transform.DOScale(
                        originalButtonScales[character.characterButton],
                        stateChangeDuration
                    );
                    character.progressImage.fillAmount = 0f;
                }
            }

            // Scale the current target button if there is one
            if (currentTarget != null && currentTarget.id != selectedCharacter?.id)
            {
                Debug.Log("UpdateTargetButtonScale: " + currentTarget.id + " " + selectedCharacter?.id);
                Vector3 originalScale = originalButtonScales[currentTarget.characterButton];
                Vector3 targetScale = isOverlapping ?
                    originalScale * targetButtonScaleDownFactor :
                    originalScale;

                Debug.Log("UpdateTargetButtonScale: " + targetScale);

                currentTarget.characterButton.transform.DOScale(targetScale, stateChangeDuration);
            }
        }

        protected override void ResetProgress()
        {
            base.ResetProgress();
            currentTarget = null;
            if (selectedCharacter == null)
            {
                UpdatePreview(null);
            }
        }

        private void UpdatePreview(CharacterButtonData character)
        {
            if (character != null)
            {
                var characterData = characterGroup.GetCharacterData(character.id);
                previewImage.enabled = true;
                previewImage.sprite = characterData.characterSprite;
                characterNameText.text = characterData.characterName;
            }
            else
            {
                previewImage.sprite = defaultPreviewSprite;
                characterNameText.text = "";
            }
        }

        protected override void Start()
        {
            // If you want to set it in code, uncomment the line below
            allowHandVisibilityChange = false;

            hintText.gameObject.SetActive(false);

            base.Start();

            // Initialize button images at start
            foreach (var character in characters)
            {
                var characterData = characterGroup.GetCharacterData(character.id);
                character.characterImage.sprite = characterData.characterSprite;
            }

            // Play story
            PlayStory();
        }

        private void PlayStory()
        {
            StartCoroutine(beginningStory.PlayStoryCoroutine(() =>
            {
                handLandmarkSelector.enabled = true;
            }));
        }
    }
}
