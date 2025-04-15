using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity;
using ClickClick.GestureTracking;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using ClickClick.Tool;
using System.IO;
using ClickClick.Manager;
using Mediapipe.Unity.Sample.HandLandmarkDetection;

namespace ClickClick.Photograph
{
    public class Photograph : MonoBehaviour
    {
        [SerializeField] private MultiHandLandmarkListAnnotation handLandmarkAnnotation;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private float countdownDuration = 3f;
        [SerializeField] private UnityEvent onCountdownComplete;
        [Header("Photography")]
        [SerializeField] private GameObject photographContainer;
        [SerializeField] private Image flashEffect;
        [SerializeField] private RawImage screenshotSource;
        [SerializeField] private float flashDuration = 0.5f;
        [Header("Photo Display")]
        [SerializeField] private Image capturedPhotoImage;
        [SerializeField] private float photoDisplayDuration = 1f;

        [Header("Photo Mask")]
        [SerializeField] private RectTransform photoMaskRectTransform;
        [SerializeField] private Image photoMask;
        [SerializeField] private Image playerCharacterIcon;
        [SerializeField] private Image avatarImage;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private CanvasGroup photoMaskCanvasGroup;

        [Header("Scene Transition")]
        [SerializeField] private float delayBeforeTransition = 1f;
        [SerializeField] private GameObject transitionButtonContainer;
        [SerializeField] private SingleButtonProgress transitionButton;

        [Header("Audio")]
        [SerializeField] private AudioController _takePhotoAudio;

        [Header("Story")]
        [SerializeField] private PhotoGraphStory photoGraphStory;
        [SerializeField] private HandLandmarkerRunnerPhotograph handLandmarkSelector;


        private HandGestureDetector gestureDetector;
        private HandLandmarkerResult currentResult;
        private bool needsUpdate = false;
        private float currentCountdown;
        private bool isCountingDown;
        private HandGesture? leftHandGesture;
        private HandGesture? rightHandGesture;
        private bool hasPhotoBeenTaken = false;
        private bool photoSaved = false;

        private void Start()
        {
            gestureDetector = new HandGestureDetector();
            if (flashEffect != null)
            {
                flashEffect.gameObject.SetActive(false);
                var color = flashEffect.color;
                color.a = 0;
                flashEffect.color = color;
            }
            if (transitionButton != null)
            {
                transitionButton.progressApproval = false;
            }

            LoadPlayerInfo();

            PlayStory();
        }

        private void PlayStory()
        {
            StartCoroutine(photoGraphStory.PlayStoryCoroutine(() =>
            {
                handLandmarkSelector.enabled = true;
            }));
        }

        private void Update()
        {
            if (needsUpdate)
            {
                UpdateGestureObjectsInternal(currentResult);
                needsUpdate = false;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                CaptureAndSaveMaskedPhoto();
            }

            UpdateCountdown();
        }

        public void UpdateGestureObjects(HandLandmarkerResult result)
        {
            currentResult = result;
            needsUpdate = true;
        }

        private void UpdateGestureObjectsInternal(HandLandmarkerResult result)
        {
            if (hasPhotoBeenTaken) return;

            leftHandGesture = null;
            rightHandGesture = null;

            if (ReferenceEquals(result, null) || result.handLandmarks == null || result.handLandmarks.Count == 0)
            {
                // ResetCountdown();
                return;
            }

            for (int i = 0; i < result.handLandmarks.Count; i++)
            {
                if (!ValidateHandIndex(i, result)) continue;

                var landmarks = result.handLandmarks[i];
                var handedness = result.handedness[i];
                bool isRightHand = handedness.categories[0].categoryName.ToLower().Contains("right");

                var gesture = gestureDetector.DetectGesture(landmarks);

                if (isRightHand)
                {
                    rightHandGesture = gesture;
                }
                else
                {
                    leftHandGesture = gesture;
                }
            }

            if (leftHandGesture == HandGesture.Scissors && rightHandGesture == HandGesture.Scissors)
            {
                OnBothHandsScissors();
            }
            // else
            // {
            //     ResetCountdown();
            // }
        }

        private bool ValidateHandIndex(int index, HandLandmarkerResult result)
        {
            return result.handedness != null &&
                   index < result.handedness.Count &&
                   result.handedness[index].categories != null &&
                   result.handedness[index].categories.Count > 0;
        }

        private void OnBothHandsScissors()
        {
            if (!isCountingDown)
            {
                isCountingDown = true;
                currentCountdown = countdownDuration;
            }
        }

        private void ResetCountdown()
        {
            isCountingDown = false;
            currentCountdown = countdownDuration;
            countdownText.text = "";
        }

        private void UpdateCountdown()
        {
            if (!isCountingDown) return;

            currentCountdown -= Time.deltaTime;

            if (currentCountdown <= 0)
            {
                isCountingDown = false;
                countdownText.text = "";
                TakePhotograph();
                return;
            }

            countdownText.text = Mathf.Ceil(currentCountdown).ToString();
        }

        private void TakePhotograph()
        {
            if (hasPhotoBeenTaken) return;

            hasPhotoBeenTaken = true;
            StartCoroutine(PhotoEffect());
        }

        private IEnumerator PhotoEffect()
        {
            flashEffect.gameObject.SetActive(true);

            _takePhotoAudio.DoAction();

            // Flash in
            float elapsed = 0;
            while (elapsed < flashDuration / 2)
            {
                elapsed += Time.deltaTime;
                float alpha = elapsed / (flashDuration / 2);
                var color = flashEffect.color;
                color.a = alpha;
                flashEffect.color = color;
                yield return null;
            }

            photographContainer.SetActive(false);
            CaptureScreenshot();

            // Flash out
            elapsed = 0;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1 - (elapsed / (flashDuration / 2));
                var color = flashEffect.color;
                color.a = alpha;
                flashEffect.color = color;
                yield return null;
            }

            flashEffect.gameObject.SetActive(false);
        }

        private void CaptureScreenshot()
        {
            if (screenshotSource == null || capturedPhotoImage == null) return;

            Texture sourceTexture = screenshotSource.texture;
            if (sourceTexture == null)
            {
                Debug.LogError("Screenshot source has no texture");
                return;
            }

            // Create a temporary RenderTexture
            RenderTexture tempRT = RenderTexture.GetTemporary(
                sourceTexture.width,
                sourceTexture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear
            );

            // Copy the source texture to the temporary RenderTexture with flipping
            Graphics.Blit(sourceTexture, tempRT, new Vector2(-1, 1), new Vector2(1, 0));

            // Create a new Texture2D and read the RenderTexture content
            Texture2D texture = new Texture2D(
                tempRT.width,
                tempRT.height,
                TextureFormat.RGB24,
                false
            );

            // Store the current active RenderTexture
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = tempRT;

            // Read the pixels
            texture.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
            texture.Apply();

            // Restore the previous active RenderTexture
            RenderTexture.active = previousActive;

            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tempRT);

            // Convert to sprite
            Sprite photoSprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            // Save sprite to project folder
            string fileName = $"player_photo_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            string folderPath = Path.Combine(Application.persistentDataPath, "PlayerPhotos");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, fileName);
            File.WriteAllBytes(filePath, texture.EncodeToPNG());

            // Also save to Photos folder (same as masked photo)
            string photosFileName = $"raw_photo_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            string photosFolderPath = Path.Combine(Application.persistentDataPath, "Photos");

            if (!Directory.Exists(photosFolderPath))
            {
                Directory.CreateDirectory(photosFolderPath);
            }

            string photosFilePath = Path.Combine(photosFolderPath, photosFileName);
            File.WriteAllBytes(photosFilePath, texture.EncodeToPNG());
            Debug.Log($"Raw photo saved to: {photosFilePath}");

            // Update player data in DataManager
            if (DataManager.Instance != null)
            {
                DataManager.Instance.SetCurrentPlayerPhotoPath(filePath);
            }

            // Display the photo and start animation
            StartCoroutine(DisplayPhotoAnimation(photoSprite));
        }

        private IEnumerator DisplayPhotoAnimation(Sprite photoSprite)
        {
            // Setup initial state
            capturedPhotoImage.sprite = photoSprite;
            capturedPhotoImage.gameObject.SetActive(true);

            // Set to native size
            capturedPhotoImage.SetNativeSize();

            // Center the image
            capturedPhotoImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            capturedPhotoImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            capturedPhotoImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            capturedPhotoImage.rectTransform.anchoredPosition = Vector2.zero;

            // Calculate scale to fit screen while maintaining aspect ratio
            float screenWidth = UnityEngine.Screen.width;
            float screenHeight = UnityEngine.Screen.height;
            float imageWidth = capturedPhotoImage.rectTransform.rect.width;
            float imageHeight = capturedPhotoImage.rectTransform.rect.height;

            float scaleRatio = Mathf.Min(
                screenWidth / imageWidth,
                screenHeight / imageHeight
            ) * 0.8f; // 80% of screen size

            capturedPhotoImage.rectTransform.localScale = new Vector3(1, 1, 1);

            // Wait for display duration
            yield return new WaitForSeconds(photoDisplayDuration);

            // Fade in photo mask
            photoMaskCanvasGroup.gameObject.SetActive(true);
            float elapsed = 0;
            float fadeDuration = 0.5f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = elapsed / fadeDuration;
                photoMaskCanvasGroup.alpha = alpha;
                yield return null;
            }

            photoMaskCanvasGroup.alpha = 1;

            // Set photo as child of mask
            capturedPhotoImage.transform.SetParent(photoMask.transform, true);
            capturedPhotoImage.transform.SetSiblingIndex(0);

            // Ensure final scale and size
            capturedPhotoImage.rectTransform.localScale = Vector3.one;
            capturedPhotoImage.rectTransform.sizeDelta = new Vector2(1920, 1080);

            onCountdownComplete?.Invoke();

            // Add delay before showing transition button
            yield return new WaitForSeconds(delayBeforeTransition);

            // Remove the direct scene transition code since it will now be handled by SingleButtonProgress

            // After transition button is shown, capture the masked photo
            yield return new WaitForSeconds(1f);

            yield return StartCoroutine(CaptureAndSaveMaskedPhoto());

            yield return new WaitUntil(() => photoSaved);

            // Activate the transition button
            yield return new WaitForSeconds(1f);

            if (transitionButton != null)
            {
                transitionButton.progressApproval = true;
                transitionButtonContainer.SetActive(true);
            }
        }

        private IEnumerator CaptureAndSaveMaskedPhoto()
        {
            Canvas canvas = photoMaskRectTransform.GetComponentInParent<Canvas>();
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogError("Canvas must be in ScreenSpaceOverlay mode for this to work");
                yield break;
            }

            // Get the rect in screen space
            Vector3[] corners = new Vector3[4];
            photoMaskRectTransform.GetWorldCorners(corners);

            // Convert corners to screen space
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

            // Calculate dimensions
            int width = (int)(max.x - min.x);
            int height = (int)(max.y - min.y);

            // Wait for end of frame to ensure all rendering is complete
            yield return new WaitForEndOfFrame();

            // Create a new Texture2D and read the screen pixels
            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGBA32, false);
            screenshot.ReadPixels(new Rect(min.x, min.y, width, height), 0, 0);
            screenshot.Apply();

            // Save the screenshot asynchronously
            byte[] bytes = screenshot.EncodeToPNG();
            string fileName = $"photo_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            string folderPath = Path.Combine(Application.persistentDataPath, "Photos");

            Debug.Log($"Saving photo to: {folderPath}");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, fileName);

            yield return new WaitForEndOfFrame();
            File.WriteAllBytes(filePath, bytes);

            if (DataManager.Instance != null)
            {
                DataManager.Instance.CurrentPhotoPath = filePath;
            }

            Debug.Log($"Screenshot saved to: {filePath}");

            // Clean up
            Destroy(screenshot);

            photoSaved = true;
        }

        private void LoadPlayerInfo()
        {
            if (DataManager.Instance != null && DataManager.Instance.GetCurrentPlayer() != null)
            {
                string playerName = DataManager.Instance.GetCurrentPlayerName();
                playerNameText.text = playerName;

                Sprite characterSprite = DataManager.Instance.GetCharacterSprite();
                playerCharacterIcon.sprite = characterSprite;
                avatarImage.sprite = characterSprite;
            }
        }
    }
}