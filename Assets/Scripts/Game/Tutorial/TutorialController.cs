using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ClickClick.GestureTracking;
using Mediapipe.Tasks.Vision.HandLandmarker;
using TMPro;
using DG.Tweening;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
namespace ClickClick.Tutorial
{
    public class TutorialController : BaseStory
    {
        [System.Serializable]
        public class TutorialStep
        {
            public string instructionText;
            public HandGesture requiredGesture;
            public float requiredDuration = 1.0f;

            public Sprite outlineSprite;
            public Sprite fillSprite;
        }

        [Header("Gesture Tutorial")]
        [SerializeField] private TutorialGestureManager gestureManager;
        [SerializeField] private GameObject parentImage;
        [SerializeField] private Image outlineImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private TutorialStep[] gestureSteps;

        [Header("Tools")]
        [SerializeField] private CanvasGroup[] imageGroups;
        [SerializeField] private string[] toolScripts;

        [Header("Tutorial Video")]
        [SerializeField] private GameObject tutorialVideo;
        [SerializeField] private GameObject interactionObject;
        [SerializeField] private TutorialHLR hlrManager;
        [SerializeField] private HandLandmarkerSelector handLandmarker;

        private enum TutorialState
        {
            Inactive,
            GestureStep,
            MovementStep,
            Completed
        }

        // private TutorialState currentState = TutorialState.Inactive;
        // private int currentGestureStepIndex = 0;
        // private float currentStepTimer = 0f;
        // private bool stepCompleted = false;
        // private HandLandmarkerResult lastHandResult;

        private void Start()
        {
            ResetTutorial();

            StartCoroutine(PlayStoryCoroutine(OnTutorialCompleted));
        }

        public override IEnumerator PlayStoryCoroutine(Action onComplete)
        {
            onCompleteCallback = onComplete;

            // currentState = TutorialState.GestureStep;
            // currentGestureStepIndex = 0;

            yield return StartCoroutine(ShowCaseCoroutine());

            foreach (var step in gestureSteps)
            {
                yield return StartCoroutine(ProcessTutorialStep(step));
            }

            yield return StartCoroutine(ShowTools());

            onCompleteCallback?.Invoke();
        }

        private void OnTutorialCompleted()
        {
            // currentState = TutorialState.Inactive;
            Debug.Log("Tutorial Completed");

            interactionObject.SetActive(false);
            hlrManager.enabled = false;

            tutorialVideo.SetActive(true);
            handLandmarker.gameObject.SetActive(true);
        }

        #region Tutorial Control

        public void ResetTutorial()
        {
            // currentState = TutorialState.Inactive;
            // currentGestureStepIndex = 0;
            // currentStepTimer = 0f;
            // stepCompleted = false;
            parentImage.SetActive(false);
            outlineImage.gameObject.SetActive(false);
            fillImage.gameObject.SetActive(false);
        }
        #endregion

        #region Tutorial Process/Flow
        #region Flow
        private IEnumerator ShowCaseCoroutine()
        {
            yield return StartCoroutine(FadeIn(characterImage));

            foreach (var script in scripts)
            {
                yield return TypeText(script);
            }
        }

        private IEnumerator ProcessTutorialStep(TutorialStep step)
        {
            // Start both coroutines at the same time
            Coroutine outlineCoroutine = StartCoroutine(FadeInOutlineImage(step.outlineSprite));
            Coroutine textCoroutine = StartCoroutine(TypeText(step.instructionText));

            // Wait for both to complete
            yield return outlineCoroutine;
            yield return textCoroutine;

            yield return WaitForGestureDetection(step.requiredGesture, step.requiredDuration);

            yield return StartCoroutine(FadeFromOutlineToFill(step.fillSprite));

            parentImage.SetActive(false);

            yield return new WaitForSeconds(1);

        }

        private IEnumerator ShowTools()
        {
            foreach (var imageGroup in imageGroups)
            {
                yield return FadeInImageGroup(imageGroup);
            }

            yield return new WaitForSeconds(0.3f);

            foreach (var script in toolScripts)
            {
                yield return TypeText(script);
            }
        }

        #endregion


        #region Helper
        private IEnumerator FadeInImageGroup(CanvasGroup imageGroup)
        {
            imageGroup.alpha = 0f;
            yield return imageGroup.DOFade(1f, 0.5f).WaitForCompletion();
        }

        private IEnumerator WaitForGestureDetection(HandGesture gesture, float duration)
        {
            float timer = 0f;

            while (gestureManager.GetRightHandGesture() != gesture || timer < duration)
            {
                if (gestureManager.GetRightHandGesture() == gesture)
                {
                    timer += Time.deltaTime;
                }
                else
                {
                    timer = 0f;
                }

                yield return null;
            }
        }
        #endregion
        #endregion


        #region Preview Image
        private IEnumerator FadeInOutlineImage(Sprite sprite)
        {
            parentImage.SetActive(true);

            fillImage.gameObject.SetActive(false);

            outlineImage.gameObject.SetActive(true);
            outlineImage.sprite = sprite;

            yield return outlineImage.DOFade(1f, 0.5f).WaitForCompletion();
        }

        private IEnumerator FadeFromOutlineToFill(Sprite sprite)
        {
            parentImage.SetActive(true);

            yield return outlineImage.DOFade(0f, 0.5f).WaitForCompletion();

            outlineImage.gameObject.SetActive(false);

            fillImage.gameObject.SetActive(true);
            fillImage.sprite = sprite;

            yield return fillImage.DOFade(1f, 0.5f).WaitForCompletion();
        }
        #endregion
    }
}
