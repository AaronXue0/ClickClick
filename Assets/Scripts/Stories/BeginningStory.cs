using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ClickClick
{
    public class BeginningStory : BaseStory
    {
        [SerializeField] protected AudioController logoSound;

        public override IEnumerator PlayStoryCoroutine(Action onComplete)
        {
            group.gameObject.SetActive(true);
            group.alpha = 1f;

            onCompleteCallback = onComplete;

            if (isTest == false)
            {
                yield return new WaitForSeconds(0.3f);

                // Second Cut
                yield return SecondCutCoroutine();
                yield return new WaitForSeconds(defaultDelay);

                // First Cut
                yield return FirstCutCoroutine();
                yield return new WaitForSeconds(defaultDelay);

                // Third Cut
                yield return ThirdCutCoroutine();
                yield return new WaitForSeconds(defaultDelay);

                // Fourth Cut
                yield return FourthCutCoroutine();
                yield return new WaitForSeconds(defaultDelay);

                // End Story
                yield return EndStoryCoroutine();
            }
            else
            {
                yield return EndStoryCoroutine();
            }

            onCompleteCallback?.Invoke();
        }

        private IEnumerator FirstCutCoroutine()
        {
            // Fade in character and change background color
            characterImage.gameObject.SetActive(true);
            characterImage.color = new Color(1f, 1f, 1f, 0f);

            text.color = new Color(1f, 1f, 1f, 0f);
            text.gameObject.SetActive(true);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(characterImage.DOFade(1f, 1f));
            sequence.Join(backgroundImage.DOColor(colors[1], 1f));

            yield return sequence.WaitForCompletion();
            yield return new WaitForSeconds(defaultDelay / 5);

            yield return TypeText(scripts[0]);
        }

        private IEnumerator SecondCutCoroutine()
        {
            logoImage.gameObject.SetActive(true);
            logoImage.transform.localScale = Vector3.zero;
            logoImage.color = new Color(1f, 1f, 1f, 0f);

            // Fade in and scale up logo
            Sequence sequence = DOTween.Sequence();
            sequence.Append(logoImage.DOFade(1f, 1f));
            sequence.Join(logoImage.transform.DOScale(1f, 1.5f).SetEase(Ease.OutBack));
            logoSound.DoAction();
            yield return sequence.WaitForCompletion();

            yield return new WaitForSeconds(defaultDelay);

            // Fade out logo
            yield return logoImage.DOFade(0f, 0.5f).WaitForCompletion();
            logoImage.gameObject.SetActive(false);
        }

        private IEnumerator ThirdCutCoroutine()
        {
            // Fade out logo
            yield return logoImage.DOFade(0f, 0.5f).WaitForCompletion();
            logoImage.gameObject.SetActive(false);

            // Play script[1] and script[2]
            yield return TypeText(scripts[1]);

            yield return new WaitForSeconds(defaultDelay);

            yield return TypeText(scripts[2]);
        }

        private IEnumerator FourthCutCoroutine()
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Append(characterImage.DOFade(0f, 0.4f));
            sequence.Join(backgroundImage.DOColor(colors[2], 0.4f));
            yield return sequence.WaitForCompletion();
            text.text = "";

            yield return new WaitForSeconds(0.2f);

            yield return TypeText(scripts[3]);
        }

        private IEnumerator EndStoryCoroutine()
        {
            // Fade out everything
            yield return group.DOFade(0f, 1.5f).WaitForCompletion();
            group.gameObject.SetActive(false);
        }
    }
}