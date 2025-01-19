using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ClickClick
{
    public class SceneTransition : MonoBehaviour
    {
        public static SceneTransition Instance { get; private set; }

        [SerializeField] private GameObject canvas;
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
            canvas.SetActive(true);
            audioController.DoAction();

            // Start the fade in
            yield return StartCoroutine(FadeIn());

            // Wait for the transition animation to complete
            yield return new WaitForSeconds(transitionDuration);

            // Load the new scene
            yield return SceneManager.LoadSceneAsync(sceneName);

            // Start the fade out
            yield return StartCoroutine(FadeOut());
            canvas.SetActive(false);
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