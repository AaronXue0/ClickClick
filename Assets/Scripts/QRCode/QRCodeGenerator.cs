using UnityEngine;
using UnityEngine.UI;
using QRCoder;
using System.Drawing;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using ClickClick.Manager;
using ClickClick.Data;
using TMPro;

namespace ClickClick
{
    public class QRCodeGenerator : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image qrCodeImage;
        [SerializeField] private int qrCodeSize = 256;
        [SerializeField] private int pixelsPerModule = 20; // Size of each QR code module

        [Header("Loading")]
        [SerializeField] private UnityEngine.UI.Image loadingImage;

        [Header("Avatart")]
        [SerializeField] private UnityEngine.UI.Image characterImage;
        [SerializeField] private UnityEngine.UI.Image avatarImage;
        [SerializeField] private TMP_Text characterName;
        [SerializeField] private string testUrl = "https://www.google.com";

        [Header("Score and Rank")]
        [SerializeField] private UnityEngine.UI.Image giftImage;
        [SerializeField] private int requiredScore = 12000;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private UnityEngine.Color giftColor;
        [SerializeField] private UnityEngine.Color normalColor;


        private void Awake()
        {
            qrCodeImage.gameObject.SetActive(false);
        }

        private void Start()
        {
            UpdateAvatar();
            UpdateScoreAndRankText();
        }

        private void UpdateAvatar()
        {
            PlayerData playerData = DataManager.Instance.GetCurrentPlayer();

            if (playerData == null)
            {
                playerData = DataManager.Instance.GetTopPlayers(1)[0];
            }

            Sprite characterSprite = DataManager.Instance.GetCharacterSprite(playerData.CharacterId) ?? null;
            characterImage.sprite = characterSprite;
            StartCoroutine(LoadPlayerPhoto(avatarImage, playerData.PlayerPhotoPath));

            characterName.text = DataManager.Instance.GetCharacterName(playerData.CharacterId);
        }

        private void UpdateScoreAndRankText()
        {
            int score;
            int rank;

            if (DataManager.Instance == null || DataManager.Instance.GetCurrentPlayer() == null)
            {
                score = 12000;
                rank = 1;
            }
            else
            {
                score = DataManager.Instance.GetCurrentPlayer().Score;
                rank = DataManager.Instance.GetCurrentPlayer().Rank;
            }

            scoreText.text = $"總分：{score}";
            rankText.text = $"排名：{rank}";

            if (score >= requiredScore)
            {
                giftImage.gameObject.SetActive(true);
                scoreText.color = giftColor;
            }
            else
            {
                giftImage.gameObject.SetActive(false);
                scoreText.color = normalColor;
            }
        }

        private IEnumerator LoadPlayerPhoto(UnityEngine.UI.Image targetImage, string photoPath)
        {
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
            }
            else
            {
                Debug.LogError($"Failed to load player photo from path: {photoPath}");
                targetImage.gameObject.SetActive(false);
                Destroy(texture);
            }

            yield return new WaitForSeconds(1f);

            UploadToGoogleScript();
        }

        private string googleScriptUrl = "https://script.google.com/macros/s/AKfycbwt0ZiDtTZxi7JNoNjPOMfhBvCWKRSCE8bAqhMUKzhX0w8bWupIFb-QuTnXD_9Cx-kq/exec";

        private void UploadToGoogleScript()
        {
            StartCoroutine(CaptureAndUploadScreenshot());
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                UploadToGoogleScript();
            }
        }

        private string _url;
        public string URL
        {
            get => _url;
            set
            {
                _url = value;
                GenerateQRCode();
            }
        }

        private void GenerateQRCode()
        {
            if (string.IsNullOrEmpty(_url) || qrCodeImage == null)
            {
                Debug.LogWarning("URL is empty or QR code image reference is missing");
                return;
            }

            using (var qrGenerator = new QRCoder.QRCodeGenerator())
            using (var qrCodeData = qrGenerator.CreateQrCode(_url, QRCoder.QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCoder.PngByteQRCode(qrCodeData))
            {
                byte[] qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);

                // Create texture from bytes
                Texture2D qrCodeTexture = new Texture2D(2, 2);
                qrCodeTexture.LoadImage(qrCodeBytes);

                // Get the actual size of the generated QR code
                int actualWidth = qrCodeTexture.width;
                int actualHeight = qrCodeTexture.height;

                // Create sprite from texture with actual dimensions
                Sprite qrCodeSprite = Sprite.Create(
                    qrCodeTexture,
                    new Rect(0, 0, actualWidth, actualHeight),
                    new Vector2(0.5f, 0.5f)
                );

                loadingImage.gameObject.SetActive(false);

                // Apply sprite to image
                qrCodeImage.sprite = qrCodeSprite;
                qrCodeImage.preserveAspect = true; // Maintain aspect ratio
                qrCodeImage.gameObject.SetActive(true);
            }
        }

        private IEnumerator CaptureAndUploadScreenshot()
        {
            yield return new WaitForEndOfFrame();

            Texture2D screenImage = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenImage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenImage.Apply();

            byte[] imageBytes = screenImage.EncodeToPNG();
            string base64Image = System.Convert.ToBase64String(imageBytes);
            string fileName = "screenshot_" + System.DateTime.Now.Ticks + ".png";

            // Manually build POST data
            string postData = $"fileName={UnityWebRequest.EscapeURL(fileName)}&imageBase64={UnityWebRequest.EscapeURL(base64Image)}";
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(postData);

            UnityWebRequest www = new UnityWebRequest(googleScriptUrl, "POST");
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Upload success! File URL: " + www.downloadHandler.text);
                URL = www.downloadHandler.text;
            }
            else
            {
                Debug.LogError("Upload failed: " + www.error);
            }

            Destroy(screenImage);
        }
    }
}
