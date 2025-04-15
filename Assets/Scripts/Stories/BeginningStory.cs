using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ClickClick
{
    public class BeginningStory : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Image characterImage;
        [SerializeField] private Image logoImage;

        [SerializeField] private List<string> scripts;
        [SerializeField] private List<Color> colors;
        [SerializeField] private float defaultDelay = 2f;
        [SerializeField] private float typingSpeed = 0.05f;

        [Header("Sound")]
        [SerializeField] private AudioController typingSound1;
        [SerializeField] private AudioController typingSound2;
        [SerializeField] private AudioController typingSound3;
        [SerializeField] private AudioController typingSound4;

        private Color tempTextColor;

        private void Awake()
        {
            text.gameObject.SetActive(false);
            logoImage.gameObject.SetActive(false);
            characterImage.gameObject.SetActive(false);

            backgroundImage.color = colors[0];
        }

        public IEnumerator PlayFirstStoryCoroutine(Action onComplete)
        {
            group.gameObject.SetActive(true);
            group.alpha = 1f;

            // First Cut
            yield return FirstCutCoroutine();
            yield return new WaitForSeconds(defaultDelay);

            // Second Cut
            yield return SecondCutCoroutine();
            yield return new WaitForSeconds(defaultDelay);

            // Third Cut
            yield return ThirdCutCoroutine();
            yield return new WaitForSeconds(defaultDelay);

            // Fourth Cut
            yield return FourthCutCoroutine();
            yield return new WaitForSeconds(defaultDelay);

            // End Story
            yield return EndStoryCoroutine();

            onComplete?.Invoke();
        }

        private IEnumerator FirstCutCoroutine()
        {
            text.gameObject.SetActive(true);
            yield return TypeText(scripts[0]);
        }

        private IEnumerator SecondCutCoroutine()
        {
            // Fade out text first
            tempTextColor = text.color;
            yield return text.DOFade(0f, 0.5f).WaitForCompletion();
            text.gameObject.SetActive(false);
            text.color = tempTextColor;

            logoImage.gameObject.SetActive(true);
            logoImage.transform.localScale = Vector3.zero;
            logoImage.color = new Color(1f, 1f, 1f, 0f);

            // Fade in and scale up logo
            Sequence sequence = DOTween.Sequence();
            sequence.Append(logoImage.DOFade(1f, 1f));
            sequence.Join(logoImage.transform.DOScale(1f, 1.5f).SetEase(Ease.OutBack));

            yield return sequence.WaitForCompletion();
        }

        private IEnumerator ThirdCutCoroutine()
        {
            // Fade out logo
            yield return logoImage.DOFade(0f, 0.5f).WaitForCompletion();
            logoImage.gameObject.SetActive(false);

            // Fade in character and change background color
            characterImage.gameObject.SetActive(true);
            characterImage.color = new Color(1f, 1f, 1f, 0f);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(characterImage.DOFade(1f, 1f));
            sequence.Join(backgroundImage.DOColor(colors[1], 1f));

            yield return sequence.WaitForCompletion();
            yield return new WaitForSeconds(defaultDelay / 2);

            // Play script[1] and script[2]
            text.text = "";
            text.gameObject.SetActive(true);
            yield return TypeText(scripts[1]);
            yield return new WaitForSeconds(defaultDelay);

            text.text = "";
            text.gameObject.SetActive(true);
            yield return TypeText(scripts[2]);
        }

        private IEnumerator FourthCutCoroutine()
        {
            // Play script[3]
            text.text = "";
            yield return TypeText(scripts[3]);
        }

        private IEnumerator EndStoryCoroutine()
        {
            // Fade out everything
            yield return group.DOFade(0f, 1.5f).WaitForCompletion();
            group.gameObject.SetActive(false);
        }

        private IEnumerator TypeText(string content)
        {
            text.text = "";
            int charIndex = 0;

            while (charIndex < content.Length)
            {
                text.text += content[charIndex];
                PlayRandomTypingSFX();
                charIndex++;
                yield return new WaitForSeconds(typingSpeed);
            }

            // Wait for an appropriate time based on text length to allow reading
            float readTime = Mathf.Max(defaultDelay, content.Length * 0.05f);
            yield return new WaitForSeconds(readTime);
        }

        private void PlayRandomTypingSFX()
        {
            // Create array of available typing sounds
            AudioController[] typingSounds = new AudioController[]
            {
                typingSound1,
                typingSound2,
                typingSound3,
                typingSound4
            };

            // Filter out null references
            List<AudioController> availableSounds = new List<AudioController>();
            foreach (var sound in typingSounds)
            {
                if (sound != null)
                {
                    availableSounds.Add(sound);
                }
            }

            // Play random sound if any are available
            if (availableSounds.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, availableSounds.Count);
                availableSounds[randomIndex].DoAction();
            }
        }
    }
}