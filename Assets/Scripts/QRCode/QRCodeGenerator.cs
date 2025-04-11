using UnityEngine;
using UnityEngine.UI;
using QRCoder;
using System.Drawing;
using System.IO;

namespace ClickClick
{
    public class QRCodeGenerator : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image qrCodeImage;
        [SerializeField] private int qrCodeSize = 256;
        [SerializeField] private int pixelsPerModule = 20; // Size of each QR code module

        [SerializeField] private string testUrl = "https://www.google.com";

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                URL = testUrl;
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

                // Apply sprite to image
                qrCodeImage.sprite = qrCodeSprite;
                qrCodeImage.preserveAspect = true; // Maintain aspect ratio
                qrCodeImage.gameObject.SetActive(true);
            }
        }
    }
}
