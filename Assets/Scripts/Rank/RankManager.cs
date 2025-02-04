using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using ClickClick.Manager;
using ClickClick.Data;
using System.Linq;

namespace ClickClick.Rank
{
    public class RankManager : MonoBehaviour
    {
        [SerializeField] private Transform rankParent;
        [SerializeField] private List<RankObject> rankDataList;
        [SerializeField] private GameObject btn;
        [SerializeField] private float revealDuration = 3f; // Duration of the reveal animation
        [SerializeField] private float numberChangeInterval = 0.05f; // How fast numbers change

        [Header("Audio")]
        [SerializeField] private AudioController drumRollAudio;
        [SerializeField] private AudioController showRankAudio;

        [SerializeField] private GameObject transitionButtonContainer;


        private List<GameObject> rankObjects = new List<GameObject>();
        private Transform playerRankObject;
        private int targetPlayerRank;
        private bool isRevealing = false;

        private void Awake()
        {
            // Get all rank objects under rankParent
            for (int i = 0; i < rankParent.childCount; i++)
            {
                rankObjects.Add(rankParent.GetChild(i).gameObject);
            }
            // Set player rank object (last one)
            playerRankObject = rankObjects[rankObjects.Count - 1].transform;
        }

        private void Start()
        {
            transitionButtonContainer.SetActive(false);
            // Just load rankings directly
            LoadPlayerRankings();
        }

        private void LoadPlayerRankings()
        {
            PlayerData currentPlayer = DataManager.Instance.GetCurrentPlayer();
            int currentRank = currentPlayer.rank;

            // Get all players except current player
            List<PlayerData> allPlayers = DataManager.Instance.GetTopPlayers(999)
                .Where(p => p.playerId != currentPlayer.playerId)
                .ToList();

            List<PlayerData> displayPlayers = new List<PlayerData>();

            if (currentRank == 1)
            {
                displayPlayers = allPlayers
                    .Take(4)
                    .ToList();
            }
            else
            {
                int startIndex = Mathf.Max(currentRank - 3, 0);
                displayPlayers = allPlayers
                    .Skip(startIndex)
                    .Take(4)
                    .ToList();
            }

            // Update UI for surrounding players
            for (int i = 0; i < displayPlayers.Count && i < rankDataList.Count - 1; i++)
            {
                PlayerData player = displayPlayers[i];
                UpdateRankDisplay(rankDataList[i].rankData, player.rank, player.score);
                rankDataList[i].rankData.rank = player.rank;
                rankDataList[i].rankData.score = player.score;

                // Load avatar
                if (rankDataList[i].rankData.avatarImage != null)
                {
                    Sprite characterSprite = DataManager.Instance.GetCharacterSprite(player.characterId);
                    if (characterSprite != null)
                    {
                        rankDataList[i].rankData.avatarImage.sprite = characterSprite;
                        rankDataList[i].rankData.avatarImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        rankDataList[i].rankData.avatarImage.gameObject.SetActive(false);
                    }
                }

                // Load player photo
                if (rankDataList[i].rankData.playerPhotoImage != null && !string.IsNullOrEmpty(player.playerPhotoPath))
                {
                    StartCoroutine(LoadPlayerPhoto(rankDataList[i].rankData.playerPhotoImage, player.playerPhotoPath));
                }
                else if (rankDataList[i].rankData.playerPhotoImage != null)
                {
                    rankDataList[i].rankData.playerPhotoImage.gameObject.SetActive(false);
                }
            }

            // Load current player's images
            var currentPlayerRankData = rankDataList[rankDataList.Count - 1];
            if (currentPlayerRankData.rankData.avatarImage != null)
            {
                Sprite characterSprite = DataManager.Instance.GetCharacterSprite(currentPlayer.characterId);
                if (characterSprite != null)
                {
                    currentPlayerRankData.rankData.avatarImage.sprite = characterSprite;
                    currentPlayerRankData.rankData.avatarImage.gameObject.SetActive(true);
                }
            }

            if (currentPlayerRankData.rankData.playerPhotoImage != null && !string.IsNullOrEmpty(currentPlayer.playerPhotoPath))
            {
                StartCoroutine(LoadPlayerPhoto(currentPlayerRankData.rankData.playerPhotoImage, currentPlayer.playerPhotoPath));
            }

            // Start reveal animation
            AssignRank(currentRank);
        }

        private IEnumerator LoadPlayerPhoto(UnityEngine.UI.Image targetImage, string photoPath)
        {
            if (!System.IO.File.Exists(photoPath))
            {
                Debug.LogWarning($"Player photo not found at path: {photoPath}");
                targetImage.gameObject.SetActive(false);
                yield break;
            }

            byte[] photoData = System.IO.File.ReadAllBytes(photoPath);
            Texture2D texture = new Texture2D(2, 2);

            if (texture.LoadImage(photoData))
            {
                Sprite photoSprite = Sprite.Create(
                    texture,
                    new UnityEngine.Rect(0, 0, texture.width, texture.height),
                    new UnityEngine.Vector2(0.5f, 0.5f)
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

        public void AssignRank(int rank)
        {
            if (isRevealing) return;

            PlayerData currentPlayer = DataManager.Instance.GetCurrentPlayer();
            targetPlayerRank = currentPlayer.rank;

            FetchRank(targetPlayerRank);
            StartCoroutine(RevealRankSequence());
        }

        public void FetchRank(int rank)
        {
            int playerPosition = rank == 1 ? 0 : (rank == 2 ? 1 : 2);

            for (int i = 0; i < rankDataList.Count; i++)
            {
                if (i < playerPosition)
                {
                    rankDataList[i].rankData.rank = rank - (playerPosition - i);
                }
                else if (i > playerPosition)
                {
                    rankDataList[i].rankData.rank = rank + (i - playerPosition);
                }
                else
                {
                    rankDataList[i].rankData.rank = rank;
                }

                if (rankDataList[i].rankData.rank < 1) rankDataList[i].rankData.rank = 1;
            }
        }

        private IEnumerator RevealRankSequence()
        {
            Debug.Log("Starting RevealRankSequence");
            isRevealing = true;
            int currentDisplayRank;
            float elapsedTime = 0f;

            // Start random number animation for all ranks except player
            Coroutine randomizeCoroutine = StartCoroutine(RandomizeNumbers());

            drumRollAudio.DoAction();

            // Decrease player's rank
            while (elapsedTime < revealDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / revealDuration;
                currentDisplayRank = Mathf.RoundToInt(Mathf.Lerp(9999, targetPlayerRank, t));
                UpdatePlayerRankDisplay(currentDisplayRank);
                yield return null;
            }

            // Stop only the randomize numbers coroutine instead of all coroutines
            if (randomizeCoroutine != null)
                StopCoroutine(randomizeCoroutine);

            // Set the final rank first
            UpdatePlayerRankDisplay(targetPlayerRank);

            for (int i = 0; i < rankObjects.Count - 1; i++) // Changed to exclude player's rank
            {
                UpdateRankDisplay(rankDataList[i].rankData, rankDataList[i].rankData.rank, rankDataList[i].rankData.score);
            }

            rankDataList[rankDataList.Count - 1].rankData.rankText.text = "?";

            // yield return new WaitForSeconds(1f);

            // Determine player position based on rank
            int targetPosition;
            if (targetPlayerRank == 1)
                targetPosition = 0;
            else if (targetPlayerRank == 2)
                targetPosition = 1;
            else
                targetPosition = 2;

            // yield return new WaitForSeconds(1f);
            yield return StartCoroutine(AnimatePlayerToPosition(targetPosition));
            yield return new WaitForSeconds(1f);

            yield return StartCoroutine(DisplayFinalRanks());
            isRevealing = false;

            btn.SetActive(true);
        }

        private IEnumerator AnimatePlayerToPosition(int targetIndex)
        {
            int currentIndex = rankObjects.Count - 1;
            while (currentIndex > targetIndex)
            {
                playerRankObject.SetSiblingIndex(currentIndex - 1);
                currentIndex--;
                yield return new WaitForSeconds(0.15f);
            }
            yield return null;
        }

        private IEnumerator RandomizeNumbers()
        {
            while (true)
            {
                int index = 0;
                foreach (RankObject rankObj in rankDataList)
                {
                    if (index != rankDataList.Count - 1)
                    {
                        int randomRank = Random.Range(1, 10000);
                        int randomScore = Random.Range(0, 1000000);
                        UpdateRankDisplay(rankObj.rankData, randomRank, randomScore);
                    }
                    index++;
                }
                yield return new WaitForSeconds(numberChangeInterval);
            }
        }

        private IEnumerator DisplayFinalRanks()
        {
            PlayerData currentPlayer = DataManager.Instance.GetCurrentPlayer();
            List<PlayerData> allPlayers = DataManager.Instance.GetTopPlayers(999)
                .Where(p => p.playerId != currentPlayer.playerId)
                .ToList();

            List<PlayerData> displayPlayers;

            if (currentPlayer.rank == 1)
            {
                displayPlayers = allPlayers.Take(4).ToList();
            }
            else
            {
                int startIndex = Mathf.Max(currentPlayer.rank - 3, 0);
                displayPlayers = allPlayers
                    .Skip(startIndex)
                    .Take(4)
                    .ToList();
            }

            // Display ranks for other players
            for (int i = 0; i < displayPlayers.Count && i < rankDataList.Count - 1; i++)
            {
                PlayerData player = displayPlayers[i];
                UpdateRankDisplay(rankDataList[i].rankData, player.rank, player.score);
                yield return new WaitForSeconds(0.15f);
            }

            showRankAudio.DoAction();
            yield return new WaitForSeconds(0.1f);

            // Show current player's rank
            UpdatePlayerRankDisplay(currentPlayer.rank);
        }

        private void UpdatePlayerRankDisplay(int rank)
        {
            PlayerData currentPlayer = DataManager.Instance.GetCurrentPlayer();
            UpdateRankDisplay(rankDataList[rankDataList.Count - 1].rankData, rank, currentPlayer.score);
            transitionButtonContainer.SetActive(true);
        }

        private void UpdateRankDisplay(RankData rankData, int rank, int score)
        {
            if (rankData != null)
            {
                if (rankData.rankText != null)
                {
                    rankData.rankText.text = rank.ToString();
                }
                else
                {
                    rankData.rankText.text = "?";
                }

                if (rankData.scoreText != null)
                {
                    rankData.scoreText.text = score.ToString();
                }
                else
                {
                    rankData.scoreText.text = score.ToString();
                }
            }
        }
    }

    [System.Serializable]
    public class RankData
    {
        public TMP_Text rankText;
        public TMP_Text scoreText;
        public UnityEngine.UI.Image avatarImage;
        public UnityEngine.UI.Image playerPhotoImage;

        [HideInInspector]
        public int rank;
        [HideInInspector]
        public Sprite sprite;
        [HideInInspector]
        public Texture rawImageTexture;
        [HideInInspector]
        public int score;
    }
}
