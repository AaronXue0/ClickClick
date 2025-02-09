using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using DG.Tweening;
using ClickClick.Data;

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

        private CharacterButtonData currentTarget;
        private Dictionary<Button, Vector3> originalButtonScales = new Dictionary<Button, Vector3>();
        private bool isSelectionLocked = false;

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
            if (currentTarget != null && !isSelectionLocked)
            {
                // Lock the selection
                isSelectionLocked = true;

                // Fetch CharacterData using ID
                var characterData = characterGroup.GetCharacterData(currentTarget.id);

                // Update preview
                previewImage.sprite = characterData.characterSprite;
                characterNameText.text = characterData.characterName;

                // Save the selected character to DataManager
                Manager.DataManager.Instance.GetCurrentPlayer().CharacterId = characterData.characterId;
                Debug.Log("Selected character: " + characterData.characterName);

                // Trigger the button click
                currentTarget.characterButton.onClick.Invoke();

                // Determine which scene to transition to based on character ID
                string targetScene = characterData.characterId == 0 ? "Tutorial" : sceneToTransitionTo;

                // Transition to the appropriate scene after a short delay
                StartCoroutine(TransitionAfterDelay(targetScene));
            }
        }

        private IEnumerator TransitionAfterDelay(string sceneName)
        {
            // Wait for a short moment to show the final state
            yield return new WaitForSeconds(0.5f);
            // Transition to the determined scene
            SceneTransition.Instance.TransitionToScene(sceneName);
        }

        protected override bool IsOverlappingTargetButton(GameObject gestureObject)
        {
            // If selection is locked, prevent any further changes
            if (isSelectionLocked)
            {
                return currentTarget != null;
            }

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
                        if (previousTarget != null && previousTarget != character)
                        {
                            ResetProgress();
                        }

                        currentTarget = character;
                        UpdatePreview(character);
                        return true;
                    }
                }
            }

            currentTarget = null;
            UpdatePreview(null);
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
            if (currentTarget != null)
            {
                Vector3 originalScale = originalButtonScales[currentTarget.characterButton];
                Vector3 targetScale = isOverlapping ?
                    originalScale * targetButtonScaleDownFactor :
                    originalScale;

                currentTarget.characterButton.transform.DOScale(targetScale, stateChangeDuration);
            }
        }

        protected override void ResetProgress()
        {
            // Only reset if selection is not locked
            if (!isSelectionLocked)
            {
                base.ResetProgress();
                currentTarget = null;
                UpdatePreview(null);
            }
        }

        private void UpdatePreview(CharacterButtonData character)
        {
            if (character != null)
            {
                var characterData = characterGroup.GetCharacterData(character.id);
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

            base.Start();

            // Initialize button images at start
            foreach (var character in characters)
            {
                var characterData = characterGroup.GetCharacterData(character.id);
                character.characterImage.sprite = characterData.characterSprite;
            }
        }
    }
}
