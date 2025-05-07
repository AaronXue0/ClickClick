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
                SetupCanvas();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void SetupCanvas()
        {
            // 確保 Canvas 的渲染順序最高
            canvas.sortingOrder = 32767;
            // 確保 fadeImage 在 Canvas 的最上層
            fadeImage.transform.SetAsLastSibling();
            // 初始時隱藏 fadeImage
            fadeImage.gameObject.SetActive(false);
        }

        public void TransitionToScene(string sceneName)
        {
            StartCoroutine(TransitionCoroutine(sceneName));
        }

        private IEnumerator TransitionCoroutine(string sceneName)
        {
            // 確保 fadeImage 已經準備好
            fadeImage.gameObject.SetActive(true);
            canvas.worldCamera = Camera.main;
            canvas.gameObject.SetActive(true);
            audioController.DoAction();

            transitionParticle.Play();

            // 開始淡入
            yield return StartCoroutine(FadeIn());

            // 等待轉場動畫完成
            yield return new WaitForSeconds(transitionDuration);

            // 開始異步加載場景
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            while (asyncLoad.progress < 0.9f)
            {
                yield return null;
            }

            // 更新 Canvas 的相機
            canvas.worldCamera = Camera.main;

            // 允許場景激活
            asyncLoad.allowSceneActivation = true;

            // 等待場景完全加載
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            yield return new WaitForSeconds(transitionDuration);
            
            // 確保 fadeImage 仍然在最上層
            fadeImage.transform.SetAsLastSibling();

            // 開始淡出
            yield return StartCoroutine(FadeOut());

            // 完成後隱藏 fadeImage
            fadeImage.gameObject.SetActive(false);
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

            // 確保最終顏色完全設置
            fadeImage.color = targetColor;
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

            // 確保最終顏色完全設置
            fadeImage.color = targetColor;
        }
    }
}