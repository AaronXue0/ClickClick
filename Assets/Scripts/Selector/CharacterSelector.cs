using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using DG.Tweening;
using ClickClick.Data;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using Mediapipe.Tasks.Vision.HandLandmarker;
using ClickClick.GestureTracking;
using Mediapipe.Unity;

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

    public class CharacterSelector : MonoBehaviour
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

        [Header("Hand Gesture")]
        [SerializeField] private GameObject rightHandGameObject;
        [SerializeField] private GameObject leftHandGameObject;
        [SerializeField] private MultiHandLandmarkListAnnotation handLandmarkAnnotation;
        [SerializeField] private string sceneToTransitionTo;
        [SerializeField] private float waveDetectionThreshold = 0.5f;
        [SerializeField] private int requiredWaveCount = 1;
        [SerializeField] private float waveDetectionCooldown = 0.5f;
        [SerializeField] private float selectionCooldownDuration = 2.0f;

        [Header("Audio")]
        [SerializeField] private AudioController audioController;

        [Header("UI")]
        [SerializeField] private float targetButtonScaleDownFactor = 0.9f;
        [SerializeField] private float stateChangeDuration = 0.3f;

        private CharacterButtonData currentTarget;
        private CharacterButtonData selectedCharacter;
        private Dictionary<Button, Vector3> originalButtonScales = new Dictionary<Button, Vector3>();

        private HandGestureDetector gestureDetector;
        private HandLandmarkerResult currentResult;
        private bool needsUpdate = false;
        private bool isOverlapping = false;
        private bool isCompleted = false;
        private float lastStateChangeTime;
        private string _sceneName;

        // Hand waving detection variables
        private Vector3 previousHandPosition;
        private int waveCounter = 0;
        private float lastWaveTime;
        private bool isWaving = false;
        private List<Vector3> recentHandPositions = new List<Vector3>();
        private float lastWaveDirection = 0f;

        private float lastSelectionTime;
        private bool isInSelectionCooldown = false;

        private void Start()
        {
            gestureDetector = new HandGestureDetector();
            lastStateChangeTime = -waveDetectionCooldown;
            lastWaveTime = -waveDetectionCooldown;
            lastSelectionTime = -selectionCooldownDuration;
            InitializeTargetButton();

            hintText.gameObject.SetActive(false);

            // Initialize button images at start
            foreach (var character in characters)
            {
                var characterData = characterGroup.GetCharacterData(character.id);
                character.characterImage.sprite = characterData.characterSprite;
            }

            // Play story
            PlayStory();
        }

        private void InitializeTargetButton()
        {
            UpdatePreview(null);
            foreach (var character in characters)
            {
                originalButtonScales[character.characterButton] = character.characterButton.transform.localScale;
                var characterData = characterGroup.GetCharacterData(character.id);
                character.characterImage.sprite = characterData.characterSprite;
                character.characterNameText.text = characterData.characterName;
                // Hide progress images as they're not needed for wave detection
                if (character.progressImage != null)
                {
                    character.progressImage.gameObject.SetActive(false);
                }
            }
        }

        private void HandleCharacterSelection()
        {
            if (currentTarget != null)
            {
                // Set cooldown flag and timestamp
                isInSelectionCooldown = true;
                lastSelectionTime = Time.time;

                // Reset all movement tracking
                ResetWaveDetection();
                previousHandPosition = Vector3.zero;

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

                // Play selection sound
                audioController.DoAction();
            }
        }

        private void Update()
        {
            if (needsUpdate)
            {
                UpdateGestureObjectsInternal(currentResult);
                needsUpdate = false;
            }

            // Check if we should exit the cooldown period
            if (isInSelectionCooldown && Time.time - lastSelectionTime > selectionCooldownDuration)
            {
                isInSelectionCooldown = false;
            }
        }

        private void ResetWaveDetection()
        {
            waveCounter = 0;
            isWaving = false;
            lastWaveDirection = 0f;
            recentHandPositions.Clear();
            // Reset previous hand position to prevent false detection after reset
            previousHandPosition = Vector3.zero;
        }

        private IEnumerator TransitionAfterDelay(string sceneName)
        {
            yield return new WaitForSeconds(0f);

            _sceneName = sceneName;

            UpdatePreview(selectedCharacter);
            ResetWaveDetection();
            isCompleted = false;
        }

        private void FixedUpdate()
        {
            if (selectedCharacter != null && Input.GetKey(KeyCode.Space))
            {
                isCompleted = true;
                SceneTransition.Instance.TransitionToScene(_sceneName);
            }
        }

        private bool IsOverlappingTargetButton(GameObject gestureObject)
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
                        if (previousTarget != null && previousTarget != character)
                        {
                            ResetWaveDetection();
                            // Force reset previous hand position to prevent false wave detection
                            previousHandPosition = Vector3.zero;
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
                    }
                }
            }

            // If we lost targeting completely, reset wave detection
            if (previousTarget != null && currentTarget == null)
            {
                ResetWaveDetection();
                previousHandPosition = Vector3.zero;
            }

            currentTarget = null;
            return false;
        }

        private void UpdateTargetButtonScale(bool isOverlapping)
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
                }
            }

            // Scale the current target button if there is one
            if (currentTarget != null && currentTarget.id != selectedCharacter?.id)
            {
                Vector3 originalScale = originalButtonScales[currentTarget.characterButton];
                Vector3 targetScale = isOverlapping ?
                    originalScale * targetButtonScaleDownFactor :
                    originalScale;

                currentTarget.characterButton.transform.DOScale(targetScale, stateChangeDuration);
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

        public void UpdateGestureObjects(HandLandmarkerResult result)
        {
            currentResult = result;
            needsUpdate = true;
        }

        private void UpdateGestureObjectsInternal(HandLandmarkerResult result)
        {
            DisableAllObjects(rightHandGameObject);
            DisableAllObjects(leftHandGameObject);

            if (ReferenceEquals(result, null) || result.handLandmarks == null || result.handLandmarks.Count == 0)
            {
                return;
            }

            for (int i = 0; i < result.handLandmarks.Count; i++)
            {
                var landmarks = result.handLandmarks[i];
                var handedness = result.handedness[i];
                bool isRightHand = handedness.categories[0].categoryName.ToLower().Contains("left");
                var gestureGroup = isRightHand ? rightHandGameObject : leftHandGameObject;

                if (gestureGroup == null)
                    continue;

                var gesture = gestureDetector.DetectGesture(landmarks);
                UpdateHandGestureObject(gestureGroup, gesture, i);

                // Get the palm position for wave detection (using wrist landmark)
                if (landmarks.landmarks != null && landmarks.landmarks.Count > 0)
                {
                    Vector3 handPosition = new Vector3(landmarks.landmarks[0].x, landmarks.landmarks[0].y, landmarks.landmarks[0].z);
                    DetectWaving(handPosition, gestureGroup);
                }
            }
        }

        private void DetectWaving(Vector3 currentHandPosition, GameObject gestureGroup)
        {
            // Skip wave detection during cooldown period
            if (isInSelectionCooldown)
            {
                return;
            }

            // Only process waving if overlapping a button and not completed
            if (!isOverlapping || isCompleted || currentTarget == null)
            {
                previousHandPosition = currentHandPosition;
                return;
            }

            // If this is the first position tracked, just store it without calculating movement
            if (previousHandPosition == Vector3.zero)
            {
                previousHandPosition = currentHandPosition;
                return;
            }

            // Calculate the x-axis movement (side to side)
            float xMovement = currentHandPosition.x - previousHandPosition.x;

            // Debug log to track movement
            if (Mathf.Abs(xMovement) > 0.01f)
            {
                Debug.Log($"Hand movement: {xMovement}, threshold: {waveDetectionThreshold}");
            }

            // Detect movement with enough magnitude
            if (Mathf.Abs(xMovement) > waveDetectionThreshold)
            {
                Debug.Log($"Wave detected with magnitude: {Mathf.Abs(xMovement)}");

                // Increment wave counter
                waveCounter++;
                lastWaveTime = Time.time;

                // Visual feedback
                gestureGroup.transform.DOScale(1.2f, 0.1f).OnComplete(() =>
                {
                    gestureGroup.transform.DOScale(1f, 0.1f);
                });

                // If we've detected enough waves, select the character
                if (waveCounter >= requiredWaveCount && Time.time - lastStateChangeTime > waveDetectionCooldown)
                {
                    Debug.Log("Character selected by wave gesture!");
                    HandleCharacterSelection();
                    lastStateChangeTime = Time.time;
                }
            }

            // Reset wave counter if no movement detected for a while
            if (Time.time - lastWaveTime > waveDetectionCooldown * 2)
            {
                ResetWaveDetection();
            }

            previousHandPosition = currentHandPosition;
        }

        private void UpdateHandGestureObject(GameObject gestureGroup, HandGesture gesture, int handIndex)
        {
            gestureGroup.SetActive(true);
            UpdateObjectPosition(gestureGroup, handIndex);

            // Check if rightHandGameObject or leftHandGameObject is overlapping targetButton
            bool newOverlappingState = IsOverlappingTargetButton(gestureGroup);
            if (newOverlappingState)
            {
                isOverlapping = newOverlappingState;
                // Update the visual feedback of the buttons
                UpdateTargetButtonScale(true);
            }
            else if (Time.time - lastStateChangeTime >= waveDetectionCooldown)
            {
                isOverlapping = newOverlappingState;
                lastStateChangeTime = Time.time;
                // Reset the visual feedback of the buttons
                UpdateTargetButtonScale(false);
            }
        }

        private void UpdateObjectPosition(GameObject targetObject, int handIndex)
        {
            if (handLandmarkAnnotation == null || handLandmarkAnnotation.transform.childCount <= handIndex)
            {
                return;
            }

            var handAnnotation = handLandmarkAnnotation.transform.GetChild(handIndex);
            var pointListAnnotation = handAnnotation?.GetChild(0);

            if (pointListAnnotation != null && pointListAnnotation.childCount > 9)
            {
                var middleFingerBase = pointListAnnotation.GetChild(9);
                if (middleFingerBase != null)
                {
                    targetObject.transform.SetParent(middleFingerBase, false);
                    targetObject.transform.localPosition = Vector3.zero;
                }
            }
        }

        private void DisableAllObjects(GameObject gameObject)
        {
            gameObject.SetActive(false);
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
