using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickClick
{
    public abstract class BaseStory : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup group;
        [SerializeField] protected Image backgroundImage;
        [SerializeField] protected TextMeshProUGUI text;
        [SerializeField] protected Image characterImage;
        [SerializeField] protected Image logoImage;

        [SerializeField] protected List<string> scripts;
        [SerializeField] protected List<Color> colors;
        [SerializeField] protected float defaultDelay = 2f;
        [SerializeField] protected float typingSpeed = 0.05f;

        [Header("Sound")]
        [SerializeField] protected List<AudioController> typingSounds;

        protected virtual void Awake()
        {
            text.gameObject.SetActive(false);
            logoImage.gameObject.SetActive(false);
            characterImage.gameObject.SetActive(false);

            if (colors != null && colors.Count > 0)
            {
                backgroundImage.color = colors[0];
            }
        }

        public abstract IEnumerator PlayStoryCoroutine(Action onComplete);

        protected IEnumerator TypeText(string content)
        {
            text.text = "";
            text.gameObject.SetActive(true);
            text.color = new Color(1f, 1f, 1f, 1f);

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

        protected void PlayRandomTypingSFX()
        {
            // Check if we have any typing sounds
            if (typingSounds == null || typingSounds.Count == 0)
                return;

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