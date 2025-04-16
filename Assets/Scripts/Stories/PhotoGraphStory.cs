using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ClickClick
{
    public class PhotoGraphStory : BaseStory
    {
        public override IEnumerator PlayStoryCoroutine(Action onComplete)
        {
            backgroundImage.color = colors[0];
            group.gameObject.SetActive(true);
            group.alpha = 1f;

            onCompleteCallback = onComplete;

            yield return new WaitForSeconds(0.3f);

            // CutA: Show Text, Character and play scripts
            yield return CutACoroutine();
            yield return new WaitForSeconds(defaultDelay);

            // End Story
            yield return EndStoryCoroutine();

            onCompleteCallback?.Invoke();
        }

        private IEnumerator CutACoroutine()
        {
            // Setup character and text
            characterImage.gameObject.SetActive(true);
            characterImage.color = new Color(1f, 1f, 1f, 0f);

            text.color = new Color(1f, 1f, 1f, 0f);
            text.gameObject.SetActive(true);

            // Fade in character
            Sequence sequence = DOTween.Sequence();
            sequence.Append(characterImage.DOFade(1f, 1f));
            if (colors != null && colors.Count > 1)
            {
                sequence.Join(backgroundImage.DOColor(colors[1], 1f));
            }

            yield return sequence.WaitForCompletion();
            yield return new WaitForSeconds(defaultDelay / 5);

            // Play through scripts 0-3
            for (int i = 0; i < 4 && i < scripts.Count; i++)
            {
                yield return TypeText(scripts[i]);

                if (i < 3) // Don't wait after the last script
                {
                    yield return new WaitForSeconds(defaultDelay);
                }
            }
        }

        private IEnumerator EndStoryCoroutine()
        {
            // Fade out everything
            yield return group.DOFade(0f, 1.5f).WaitForCompletion();
            group.gameObject.SetActive(false);
        }
    }
}