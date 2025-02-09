using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ClickClick.Manager;
using ClickClick.Data;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace ClickClick.Rank
{
    public class MenuRankDisplay : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject rankContainer;
        [SerializeField] private GameObject loadingContainer;

        [Header("Rank Display")]
        [SerializeField] private RankContainer[] rankContainers;
        private const int TOP_PLAYERS_COUNT = 4;
        private DataManager dataManager;

        private void Awake()
        {
            rankContainer.SetActive(false);
            loadingContainer.SetActive(true);
        }

        private void Start()
        {
            dataManager = DataManager.Instance;
            StartCoroutine(InitializeRankDisplay());
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PlayerPrefs.DeleteAll();
                if (dataManager != null)
                {
                    dataManager.Initialize();
                }
                StartCoroutine(InitializeRankDisplay());
                SceneTransition.Instance.TransitionToScene("Splash");
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                SceneTransition.Instance.TransitionToScene("Standby");
            }
        }

        public void GameStart()
        {
            DataManager.Instance.CreateNewPlayer();
        }

        private IEnumerator InitializeRankDisplay()
        {
            // Wait for DataManager to be fully initialized
            while (dataManager == null)
            {
                dataManager = DataManager.Instance;
                yield return new WaitForSeconds(0.1f);
            }

            while (dataManager.GetTopPlayers(TOP_PLAYERS_COUNT).Count == 0)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Initial hide of all containers
            foreach (var container in rankContainers)
            {
                if (container.rankContainer != null)
                {
                    container.rankContainer.SetActive(false);
                }
            }

            UpdateRankDisplay();

            loadingContainer.SetActive(false);
            rankContainer.SetActive(true);
        }

        private void UpdateRankDisplay()
        {
            if (dataManager == null)
            {
                Debug.LogWarning("DataManager is not initialized yet.");
                return;
            }

            var topPlayers = dataManager.GetTopPlayers(TOP_PLAYERS_COUNT);

            if (topPlayers == null || topPlayers.Count == 0)
            {
                Debug.Log("No player data available yet.");
                return;
            }

            if (rankContainers.Length < TOP_PLAYERS_COUNT)
            {
                Debug.LogWarning($"Not enough rank containers assigned. Please assign {TOP_PLAYERS_COUNT} containers.");
                return;
            }

            StartCoroutine(UpdateRankDisplayCoroutine(topPlayers));
        }

        private IEnumerator UpdateRankDisplayCoroutine(List<PlayerData> topPlayers)
        {
            // Update each rank container
            for (int i = 0; i < TOP_PLAYERS_COUNT; i++)
            {
                if (i < topPlayers.Count)
                {
                    yield return StartCoroutine(UpdateRankContainerCoroutine(rankContainers[i], topPlayers[i], i + 1));
                }
                else
                {
                    if (rankContainers[i].rankContainer != null)
                    {
                        rankContainers[i].rankContainer.SetActive(false);
                    }
                }

                // Add small delay between updates to prevent potential performance issues
                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator UpdateRankContainerCoroutine(RankContainer container, PlayerData playerData, int rank)
        {
            if (container == null || playerData == null)
            {
                yield break;
            }

            // First check if we have valid data
            bool hasValidPhoto = container.playerPhotoImage != null && !string.IsNullOrEmpty(playerData.PlayerPhotoPath);
            bool hasValidAvatar = container.avatarImage != null && playerData.CharacterId >= 0;
            bool hasValidScore = playerData.Score > 0;

            // Only show container if we have at least some valid data
            if (!hasValidPhoto && !hasValidAvatar && !hasValidScore)
            {
                container.rankContainer.SetActive(false);
                yield break;
            }

            container.rankContainer.SetActive(true);

            // Update score
            if (container.scoreText != null)
            {
                container.scoreText.text = playerData.Score.ToString();
            }

            // Update avatar image if available
            if (hasValidAvatar)
            {
                Sprite characterSprite = dataManager.GetCharacterSprite(playerData.CharacterId);
                if (characterSprite != null)
                {
                    container.avatarImage.sprite = characterSprite;
                    container.avatarImage.gameObject.SetActive(true);
                }
                else
                {
                    container.avatarImage.gameObject.SetActive(false);
                }
            }
            else if (container.avatarImage != null)
            {
                container.avatarImage.gameObject.SetActive(false);
            }

            // Update player photo if available
            if (hasValidPhoto)
            {
                yield return StartCoroutine(LoadPlayerPhoto(container.playerPhotoImage, playerData.PlayerPhotoPath));
            }
            else if (container.playerPhotoImage != null)
            {
                container.playerPhotoImage.gameObject.SetActive(false);
            }
        }

        private IEnumerator LoadPlayerPhoto(Image targetImage, string photoPath)
        {
            if (!File.Exists(photoPath))
            {
                Debug.LogWarning($"Player photo not found at path: {photoPath}");
                targetImage.gameObject.SetActive(false);
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
            }
            else
            {
                Debug.LogError($"Failed to load player photo from path: {photoPath}");
                targetImage.gameObject.SetActive(false);
                Destroy(texture);
            }
        }

        public void RefreshDisplay()
        {
            StartCoroutine(InitializeRankDisplay());
        }
    }

    [System.Serializable]
    public class RankContainer
    {
        public GameObject rankContainer;
        public Image avatarImage;
        public Image playerPhotoImage;
        public TextMeshProUGUI scoreText;
    }
}
