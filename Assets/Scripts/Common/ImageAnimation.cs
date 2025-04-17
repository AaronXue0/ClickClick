using UnityEngine;
using DG.Tweening;

namespace ClickClick
{
    public class ImageAnimation : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float rotationAmount = 5f;
        [SerializeField] private float animationDuration = 1.5f;
        [SerializeField] private Ease easeType = Ease.InOutSine;
        [SerializeField] private bool playOnStart = true;

        private Tweener rotationTweener;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (playOnStart)
            {
                PlayAnimation();
            }
        }

        public void PlayAnimation()
        {
            // Kill previous animation if it exists
            if (rotationTweener != null)
            {
                rotationTweener.Kill();
            }

            // Reset rotation
            transform.localRotation = Quaternion.identity;

            // Create a sequence of rotations to make it look alive
            rotationTweener = transform.DOLocalRotate(new Vector3(0, 0, rotationAmount), animationDuration)
                .SetEase(easeType)
                .OnComplete(() =>
                {
                    transform.DOLocalRotate(new Vector3(0, 0, -rotationAmount), animationDuration * 2)
                        .SetEase(easeType)
                        .OnComplete(() =>
                        {
                            transform.DOLocalRotate(Vector3.zero, animationDuration)
                                .SetEase(easeType)
                                .OnComplete(PlayAnimation);
                        });
                });
        }

        public void StopAnimation()
        {
            if (rotationTweener != null)
            {
                rotationTweener.Kill();
                transform.DOLocalRotate(Vector3.zero, 0.5f);
            }
        }

        void OnDestroy()
        {
            // Make sure to kill tweens when object is destroyed
            if (rotationTweener != null)
            {
                rotationTweener.Kill();
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}