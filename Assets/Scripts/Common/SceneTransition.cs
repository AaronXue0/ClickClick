using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ClickClick
{
    public class SceneTransition : MonoBehaviour
    {
        public static SceneTransition Instance { get; private set; }

        [SerializeField] private ParticleSystem transitionParticle;
        [SerializeField] private Canvas canvas;
        [SerializeField] private float transitionDuration = 1f;
        [SerializeField] private Image fadeImage;
        [SerializeField] private float fadeDuration = 1f;

        [Header("Audio")]
        [SerializeField] private AudioController audioController;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void TransitionToScene(string sceneName)
        {
            StartCoroutine(TransitionCoroutine(sceneName));
        }

        private IEnumerator TransitionCoroutine(string sceneName)
        {
            canvas.worldCamera = Camera.main;
            canvas.gameObject.SetActive(true);
            audioController.DoAction();

            transitionParticle.Play();
            // Start the fade in
            yield return StartCoroutine(FadeIn());

            // Wait for the transition animation to complete
            yield return new WaitForSeconds(transitionDuration);

            // Start asynchronous loading with manual activation control
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false; // Prevent automatic scene activation

            // Optionally, you can monitor loading progress here (asyncLoad.progress goes from 0 to 0.9)
            while (asyncLoad.progress < 0.9f)
            {
                // Insert code here for updating a progress bar, if needed.
                yield return null; // wait one frame
            }

            // At this point, the loading is nearly complete.
            // Update the canvas camera to the new scene's camera.
            canvas.worldCamera = Camera.main;

            // Now, allow the scene to activate.
            asyncLoad.allowSceneActivation = true;

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // Start the fade out effect to reveal the new scene smoothly.
            yield return StartCoroutine(FadeOut());

            canvas.gameObject.SetActive(false);
        }

        private IEnumerator FadeIn()
        {
            float elapsedTime = 0f;
            Color startColor = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0f);
            Color targetColor = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 1f);

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / fadeDuration);
                fadeImage.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }
        }

        private IEnumerator FadeOut()
        {
            float elapsedTime = 0f;
            Color startColor = fadeImage.color;
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / fadeDuration);
                fadeImage.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }
        }
    }
}