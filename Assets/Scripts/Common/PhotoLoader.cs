using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

namespace ClickClick
{
    public class PhotoLoader : SingletonManager<PhotoLoader>
    {
        private static Dictionary<string, Sprite> photoCache = new Dictionary<string, Sprite>();

        public static void LoadPlayerPhoto(Image targetImage, string photoPath)
        {
            Instance.StartCoroutine(LoadPlayerPhotoCoroutine(targetImage, photoPath));
        }

        private static IEnumerator LoadPlayerPhotoCoroutine(Image targetImage, string photoPath)
        {
            if (!File.Exists(photoPath))
            {
                Debug.LogWarning($"Player photo not found at path: {photoPath}");
                targetImage.gameObject.SetActive(false);
                yield break;
            }

            if (photoCache.TryGetValue(photoPath, out Sprite cachedSprite))
            {
                targetImage.sprite = cachedSprite;
                targetImage.gameObject.SetActive(true);
                yield break;
            }

            byte[] photoData = File.ReadAllBytes(photoPath);
            Texture2D texture = new Texture2D(2, 2);

            if (texture.LoadImage(photoData))
            {
                Sprite photoSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                targetImage.sprite = photoSprite;
                targetImage.gameObject.SetActive(true);
                photoCache[photoPath] = photoSprite;
            }
            else
            {
                Debug.LogError($"Failed to load player photo from path: {photoPath}");
                targetImage.gameObject.SetActive(false);
                Destroy(texture);
            }
        }
    }
}