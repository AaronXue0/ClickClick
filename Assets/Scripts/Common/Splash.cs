using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace ClickClick
{
    public class Splash : MonoBehaviour
    {
        [SerializeField] private GameObject splash;
        [SerializeField] private Image logoImage;
        [SerializeField] private float animationDuration = 2.0f;

        private void Start()
        {
            StartCoroutine(PlaySplashAnimation());
        }

        private IEnumerator PlaySplashAnimation()
        {
            // Ensure the logo starts invisible and small
            logoImage.canvasRenderer.SetAlpha(0.0f);
            logoImage.transform.localScale = Vector3.zero;

            // Fade in and scale up
            logoImage.CrossFadeAlpha(1.0f, animationDuration, false);
            float elapsedTime = 0f;
            while (elapsedTime < animationDuration)
            {
                logoImage.transform.localScale = Vector3.Lerp(new Vector3(0.5f, 0.5f, 0.5f), Vector3.one, elapsedTime / animationDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            logoImage.transform.localScale = Vector3.one;

            // Transition to menu after animation
            TransitionToMenu();
        }

        private void TransitionToMenu()
        {
            SceneTransition.Instance.TransitionToScene("Menu");
        }
    }
}