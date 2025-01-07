using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity;
using ClickClick.Gameplay;

namespace ClickClick.GestureTracking
{
    public class HandGestureObjectManager : MonoBehaviour
    {
        [SerializeField] private HandGestureMappingGroup leftHandGestures;
        [SerializeField] private HandGestureMappingGroup rightHandGestures;
        [SerializeField] private UnityEngine.UI.Image rightHandDisplayImage;
        [SerializeField] private Sprite _defaultSprite;
        [SerializeField] private MultiHandLandmarkListAnnotation handLandmarkAnnotation;

        private HandGestureDetector gestureDetector;
        private HandLandmarkerResult currentResult;
        private bool needsUpdate = false;
        private bool _isGameActive = false;

        private void Awake()
        {
            rightHandDisplayImage.sprite = _defaultSprite;
            rightHandDisplayImage.transform.parent.gameObject.SetActive(false);
            needsUpdate = false;
        }

        private void Start()
        {
            ValidateComponents();
            gestureDetector = new HandGestureDetector();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.onGameStart += EnableUpdates;
                GameManager.Instance.onGameOver += DisableUpdates;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.onGameStart -= EnableUpdates;
                GameManager.Instance.onGameOver -= DisableUpdates;
            }
        }

        private void Update()
        {
            // Only update if game is active and there's something to update
            if (!IsGameActive() || !needsUpdate) return;

            UpdateGestureObjectsInternal(currentResult);
            needsUpdate = false;
        }

        private bool IsGameActive()
        {
            return GameManager.Instance != null &&
                   GameManager.Instance.enabled &&
                   !GameManager.Instance.IsGameOver;
        }

        private void EnableUpdates()
        {
            _isGameActive = true;
            needsUpdate = true;
            rightHandDisplayImage.transform.parent.gameObject.SetActive(true);
        }

        private void DisableUpdates()
        {
            _isGameActive = false;
            needsUpdate = false;
            rightHandDisplayImage.sprite = _defaultSprite;
            DisableAllObjects(leftHandGestures);
            DisableAllObjects(rightHandGestures);
            rightHandDisplayImage.transform.parent.gameObject.SetActive(false);
        }

        public void UpdateGestureObjects(HandLandmarkerResult result)
        {
            currentResult = result;
            needsUpdate = true;
        }

        private void UpdateGestureObjectsInternal(HandLandmarkerResult result)
        {
            DisableAllObjects(leftHandGestures);
            DisableAllObjects(rightHandGestures);

            if (ReferenceEquals(result, null) || result.handLandmarks == null || result.handLandmarks.Count == 0)
            {
                return;
            }

            HandGesture rightHandGesture = HandGesture.None;
            int leftHandIndex = -1;

            // First pass: detect right hand gesture
            for (int i = 0; i < result.handLandmarks.Count; i++)
            {
                if (!ValidateHandIndex(i, result)) continue;

                var handedness = result.handedness[i];
                bool isRightHand = handedness.categories[0].categoryName.ToLower().Contains("left");

                if (isRightHand)
                {
                    rightHandGesture = gestureDetector.DetectGesture(result.handLandmarks[i]);
                    UpdateHandGestureSprite(rightHandGestures, rightHandGesture);
                }
                else
                {
                    leftHandIndex = i;
                }
            }

            // Second pass: update left hand object based on right hand gesture
            if (leftHandIndex != -1)
            {
                UpdateHandGestureObject(leftHandGestures, rightHandGesture, leftHandIndex);
            }
        }

        private void UpdateHandGestureObject(HandGestureMappingGroup gestureGroup, HandGesture gesture, int handIndex)
        {
            var matchingMapping = System.Array.Find(gestureGroup.gestureMappings, m => m.gestureType == gesture);
            if (matchingMapping?.targetObject == null) return;

            if (!_isGameActive)
                return;

            matchingMapping.targetObject.SetActive(true);
            UpdateObjectPosition(matchingMapping.targetObject, handIndex);
        }

        private void UpdateHandGestureSprite(HandGestureMappingGroup gestureGroup, HandGesture gesture)
        {
            var matchingMapping = System.Array.Find(gestureGroup.gestureMappings, m => m.gestureType == gesture);
            if (matchingMapping?.displayImage == null)
            {
                rightHandDisplayImage.transform.parent.gameObject.SetActive(false);
                return;
            }

            rightHandDisplayImage.sprite = matchingMapping.displayImage;
        }

        private void UpdateObjectPosition(GameObject targetObject, int handIndex)
        {
            if (handLandmarkAnnotation.transform.childCount <= handIndex) return;

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

        private void DisableAllObjects(HandGestureMappingGroup group)
        {
            if (group?.gestureMappings == null) return;

            foreach (var mapping in group.gestureMappings)
            {
                if (mapping.targetObject != null)
                {
                    mapping.targetObject.SetActive(false);
                }
            }
        }

        private bool ValidateHandIndex(int index, HandLandmarkerResult result)
        {
            return index < result.handedness.Count && index < result.handLandmarks.Count;
        }

        private void ValidateComponents()
        {
            if (handLandmarkAnnotation == null)
            {
                Debug.LogError("HandLandmarkAnnotation is not assigned! Please assign it in the inspector.");
            }

            if (leftHandGestures == null || rightHandGestures == null)
            {
                Debug.LogError("Hand gesture groups are not assigned! Please assign them in the inspector.");
            }
        }
    }
}