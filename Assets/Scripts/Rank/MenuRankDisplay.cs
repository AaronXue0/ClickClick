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
        [SerializeField] private RankObject[] rankContainers;
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

            // Uncomment this line to test with sample data
            // CreateTestData();

            StartCoroutine(InitializeRankDisplayCoroutine());
        }

        private IEnumerator InitializeRankDisplayCoroutine()
        {
            Debug.Log("Waiting for players data...");
            yield return new WaitUntil(() => dataManager.GetAllPlayers().Count > 0);
            Debug.Log($"Players loaded: {dataManager.GetAllPlayers().Count}");

            InitializeRankDisplay();
        }

        public void GameStart()
        {
            DataManager.Instance.CreateNewPlayer();
        }

        private void InitializeRankDisplay()
        {
            UpdateRankDisplay();

            loadingContainer.SetActive(false);
            rankContainer.SetActive(true);
        }

        private void UpdateRankDisplay()
        {
            if (dataManager == null)
            {
                Debug.LogError("DataManager is null!");
                return;
            }

            var topPlayers = dataManager.GetTopPlayers(TOP_PLAYERS_COUNT);
            Debug.Log($"Top players count: {topPlayers.Count}");

            if (topPlayers.Count == 0)
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
            if (topPlayers != null && topPlayers.Count > 0)
            {
                int displayCount = Mathf.Min(TOP_PLAYERS_COUNT, topPlayers.Count, rankContainers.Length);

                for (int i = 0; i < displayCount; i++)
                {
                    if (i >= topPlayers.Count || topPlayers[i].PlayerId == -1)
                    {
                        continue;
                    }

                    if (topPlayers[i].Score > 0)
                    {
                        Debug.Log("Updating rank display for player " + topPlayers[i].Score);
                        if (i > 0)
                            rankContainers[i].SetRankDisplay(topPlayers[i].Rank);

                        yield return StartCoroutine(UpdateRankContainerCoroutine(rankContainers[i], topPlayers[i], i + 1));
                    }
                }
            }
        }

        private IEnumerator UpdateRankContainerCoroutine(RankObject container, PlayerData playerData, int rank)
        {
            if (container == null || playerData == null)
            {
                yield break;
            }

            // Update score
            if (playerData != null)
            {
                container.SetScoreDisplay(playerData.Score);
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

                yield return StartCoroutine(LoadPlayerPhoto(container.photoImage, playerData.PlayerPhotoPath));
            }
            else
            {
                container.SetScoreDisplay(0);
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
