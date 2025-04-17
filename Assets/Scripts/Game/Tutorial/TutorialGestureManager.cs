using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity;
using ClickClick.Gameplay;

namespace ClickClick.GestureTracking
{
    public class TutorialGestureManager : MonoBehaviour
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

        private HandGesture rightHandGesture;

        private bool EnableGestureDetection = false;

        public void GameStarted()
        {
            _isGameActive = true;
            EnableGestureDetection = true;
        }

        public void GameEnded()
        {
            _isGameActive = false;
            EnableGestureDetection = false;
        }

        private void Start()
        {
            gestureDetector = new HandGestureDetector();
        }

        private void Update()
        {
            UpdateGestureObjectsInternal(currentResult);
            needsUpdate = false;
        }

        public void UpdateGestureObjects(HandLandmarkerResult result)
        {
            currentResult = result;
            needsUpdate = true;
        }

        public HandGesture GetRightHandGesture()
        {
            return rightHandGesture;
        }

        private void UpdateGestureObjectsInternal(HandLandmarkerResult result)
        {
            DisableAllObjects(leftHandGestures);
            DisableAllObjects(rightHandGestures);

            if (ReferenceEquals(result, null) || result.handLandmarks == null || result.handLandmarks.Count == 0)
            {
                return;
            }

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
            if (!EnableGestureDetection) return;

            var matchingMapping = System.Array.Find(gestureGroup.gestureMappings, m => m.gestureType == gesture);
            if (matchingMapping?.targetObject == null) return;

            if (!_isGameActive)
                return;

            matchingMapping.targetObject.SetActive(true);
            UpdateObjectPosition(matchingMapping.targetObject, handIndex);
        }

        private void UpdateHandGestureSprite(HandGestureMappingGroup gestureGroup, HandGesture gesture)
        {
            if (!EnableGestureDetection) return;

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
            if (!EnableGestureDetection) return;

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
    }
}