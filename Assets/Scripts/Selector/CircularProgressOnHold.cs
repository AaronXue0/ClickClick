using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Tasks.Vision.HandLandmarker;
using ClickClick.GestureTracking;
using Mediapipe.Unity;
using DG.Tweening;

namespace ClickClick.Tool
{
    public abstract class CircularProgressOnHold : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] protected float duration;
        [SerializeField] protected float targetButtonScaleDownFactor = 0.9f;

        [Header("Audio")]
        [SerializeField] protected AudioController audioController;

        [Header("Hand Gesture")]
        [SerializeField] protected GameObject rightHandGameObject;
        [SerializeField] protected GameObject leftHandGameObject;
        [SerializeField] protected MultiHandLandmarkListAnnotation handLandmarkAnnotation;
        [SerializeField] protected string sceneToTransitionTo;

        [Header("Hand Visibility")]
        [SerializeField] protected bool allowHandVisibilityChange = true;

        protected HandGestureDetector gestureDetector;
        protected HandLandmarkerResult currentResult;
        protected bool needsUpdate = false;

        protected float currentProgress = 0f;
        protected bool isOverlapping = false;
        protected bool isCompleted = false;

        [SerializeField] protected float stateChangeCooldown = 0.2f;
        protected float lastStateChangeTime;

        [SerializeField] protected float stateChangeDuration = 0.3f;

        protected abstract float ProgressFillAmount { get; set; }
        protected bool startOverlapping = false;


        protected virtual void Start()
        {
            gestureDetector = new HandGestureDetector();
            lastStateChangeTime = -stateChangeCooldown;
            InitializeTargetButton();
            startOverlapping = false;
        }

        // Abstract methods that derived classes must implement
        protected abstract void InitializeTargetButton();
        protected abstract void HandleProgressComplete();
        protected abstract bool IsOverlappingTargetButton(GameObject gestureObject);
        protected abstract void UpdateTargetButtonScale(bool isOverlapping);

        protected virtual void Update()
        {
            if (needsUpdate)
            {
                UpdateGestureObjectsInternal(currentResult);
                needsUpdate = false;
            }

            UpdateProgress();
        }

        protected virtual void UpdateProgress()
        {
            if (isOverlapping && !isCompleted)
            {
                if (startOverlapping == false)
                {
                    startOverlapping = true;
                    audioController.DoAction();
                }

                // Increase progress over time
                currentProgress += Time.deltaTime / duration;
                currentProgress = Mathf.Clamp01(currentProgress);
                ProgressFillAmount = currentProgress;

                // Trigger when progress reaches 1
                if (currentProgress >= 1f && !isCompleted)
                {
                    isCompleted = true;
                    HandleProgressComplete();
                    audioController.DoAction();
                }

                // Only fade out hand if allowed
                if (allowHandVisibilityChange)
                {
                    rightHandGameObject.GetComponent<Image>().DOFade(0f, stateChangeDuration);
                }

                // Update target button scale
                UpdateTargetButtonScale(true);
            }
            else if (!isCompleted && Time.time - lastStateChangeTime >= stateChangeCooldown)
            {
                ResetProgress();
            }
        }

        protected virtual void ResetProgress()
        {
            currentProgress = 0f;
            ProgressFillAmount = 0f;
            startOverlapping = false;
            // Only fade in hand if allowed
            if (allowHandVisibilityChange)
            {
                rightHandGameObject.GetComponent<Image>().DOFade(1f, stateChangeDuration);
            }

            // Reset target button scale
            UpdateTargetButtonScale(false);

            lastStateChangeTime = Time.time;
        }

        // Rest of the existing methods remain the same, but made protected...
        public void UpdateGestureObjects(HandLandmarkerResult result)
        {
            currentResult = result;
            needsUpdate = true;
        }

        protected virtual void UpdateGestureObjectsInternal(HandLandmarkerResult result)
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
            }
        }

        protected virtual void UpdateHandGestureObject(GameObject gestureGroup, HandGesture gesture, int handIndex)
        {
            gestureGroup.SetActive(true);
            UpdateObjectPosition(gestureGroup, handIndex);

            // Check if rightHandGameObject or leftHandGameObject is overlapping targetButton
            bool newOverlappingState = IsOverlappingTargetButton(gestureGroup);
            if (newOverlappingState)
            {
                isOverlapping = newOverlappingState;
            }
            else if (Time.time - lastStateChangeTime >= stateChangeCooldown)
            {
                isOverlapping = newOverlappingState;
                lastStateChangeTime = Time.time;
            }
        }

        protected virtual void UpdateObjectPosition(GameObject targetObject, int handIndex)
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

        protected virtual void DisableAllObjects(GameObject gameObject)
        {
            gameObject.SetActive(false);
        }
    }
}