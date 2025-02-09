using UnityEngine;
using ClickClick.Data;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace ClickClick.Manager
{
    public class DataManager : SingletonManager<DataManager>
    {
        [SerializeField] private CharacterGroup characterGroup;
        private GoogleSheetsManager googleSheetsManager;

        private List<PlayerData> players = new List<PlayerData>();
        private int currentPlayerId = 0;  // To track the next player ID
        private PlayerData currentPlayer;  // To track the current active player

        public string CurrentPhotoPath { get; set; }

        private void Start()
        {
            googleSheetsManager = gameObject.AddComponent<GoogleSheetsManager>();
            Initialize();
        }

        public void Initialize()
        {
            CurrentPhotoPath = "";
            characterGroup.Initialize();
            LoadPlayersData();
        }

        #region Player
        // Create a new player with default values
        public PlayerData CreateNewPlayer(int characterId = 0)
        {
            PlayerData newPlayer = new PlayerData
            {
                playerId = currentPlayerId++,
                characterId = characterId,
                score = 0,
                rank = 99999
            };

            players.Add(newPlayer);

            currentPlayer = newPlayer;

            return newPlayer;
        }

        // Convert single player data to JSON
        public string ConvertPlayerToJson(PlayerData player)
        {
            return JsonUtility.ToJson(player);
        }

        // Convert all players data to JSON
        private void SavePlayersData()
        {
            foreach (PlayerData player in players)
            {
                string jsonData = ConvertPlayerToJson(player);
                PlayerPrefs.SetString($"Player_{player.playerId}", jsonData);
            }

            PlayerPrefs.SetInt("CurrentPlayerId", currentPlayerId);
            PlayerPrefs.Save();
        }

        // Load all saved players data
        private void LoadPlayersData()
        {
            currentPlayerId = PlayerPrefs.GetInt("CurrentPlayerId", 0);
            players.Clear();

            for (int i = 0; i < currentPlayerId; i++)
            {
                string jsonData = PlayerPrefs.GetString($"Player_{i}", "");
                if (!string.IsNullOrEmpty(jsonData))
                {
                    PlayerData player = JsonUtility.FromJson<PlayerData>(jsonData);
                    players.Add(player);
                }
            }
        }

        // Get player by ID
        public PlayerData GetPlayer(int playerId)
        {
            return players.Find(p => p.playerId == playerId);
        }

        public List<PlayerData> GetAllPlayers()
        {
            return players;
        }
        #endregion

        #region Google Sheets
        public void UploadCurrentPlayer()
        {
            UploadPlayerData(GetCurrentPlayer());
        }

        public void UploadPlayerData(PlayerData playerData)
        {
            Debug.Log($"Uploading player data: {playerData.playerId}, {playerData.score}");

            if (playerData.score > 0)
            {
                StartCoroutine(googleSheetsManager.UploadPlayerData(playerData));
            }
        }

        public void LoadAllPlayersFromSheet()
        {
            StartCoroutine(googleSheetsManager.GetAllPlayersData());
        }
        #endregion

        #region Current Player Management

        public Sprite GetCharacterSprite(int characterId)
        {
            return characterGroup.GetCharacterSprite(characterId);
        }

        public Sprite GetCharacterSprite()
        {
            return characterGroup.GetCharacterSprite(GetCurrentPlayer().characterId);
        }

        public string GetCharacterName(int characterId)
        {
            return characterGroup.GetCharacterName(characterId);
        }

        public string GetCurrentPlayerName()
        {
            return characterGroup.GetCharacterName(GetCurrentPlayer().characterId);
        }

        public PlayerData GetCurrentPlayer()
        {
            return currentPlayer;
        }

        public void SetCurrentPlayer(PlayerData player)
        {
            currentPlayer = player;
        }

        public void SetCurrentPlayer(int playerId)
        {
            currentPlayer = GetPlayer(playerId);
        }

        public void UpdatePlayerScore(int playerId, int newScore)
        {
            PlayerData player = GetPlayer(playerId);
            if (player != null)
            {
                player.score = newScore;
                RecalculateRanks();
                SavePlayersData();
            }
            else
            {
                Debug.LogWarning($"Player with ID {playerId} not found. Cannot update score.");
            }
        }

        public void UpdateCurrentPlayerScore(int newScore)
        {
            PlayerData player = GetCurrentPlayer();
            player.score = newScore;
            RecalculateRanks();
            SavePlayersData();
        }

        private void RecalculateRanks()
        {
            // Sort players by score in descending order
            var sortedPlayers = players.OrderByDescending(p => p.score).ToList();

            // Assign ranks (1-based index)
            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                // Handle tied scores
                if (i > 0 && sortedPlayers[i].score == sortedPlayers[i - 1].score)
                {
                    sortedPlayers[i].rank = sortedPlayers[i - 1].rank;
                }
                else
                {
                    sortedPlayers[i].rank = i + 1;
                }
            }
        }

        public int GetPlayerRank(int playerId)
        {
            PlayerData player = GetPlayer(playerId);
            return player?.rank ?? -1;
        }

        public List<PlayerData> GetTopPlayers(int count)
        {
            return players
                .OrderByDescending(p => p.score)
                .Take(count)
                .ToList();
        }

        public void AddScoreToCurrentPlayer(int scoreToAdd)
        {
            PlayerData player = GetCurrentPlayer();
            player.score += scoreToAdd;
            RecalculateRanks();
            SavePlayersData();

            // Upload to Google Sheets
            UploadCurrentPlayer();
        }

        public void SaveCurrentPlayerScore()
        {
            if (currentPlayer != null)
            {
                // Save to local storage
                SavePlayersData();

                // Upload to Google Sheets
                UploadCurrentPlayer();
            }
        }

        public int GetCurrentPlayerScore()
        {
            PlayerData player = GetCurrentPlayer();
            return player.score;
        }

        public bool IsHighScore(int score)
        {
            PlayerData player = GetCurrentPlayer();
            return score > player.score;
        }

        public void ResetCurrentPlayerScore()
        {
            PlayerData player = GetCurrentPlayer();
            player.score = 0;
            RecalculateRanks();
            SavePlayersData();
            UploadCurrentPlayer();
        }

        public void SetCurrentPlayerPhotoPath(string photoPath)
        {
            PlayerData player = GetCurrentPlayer();
            if (player != null)
            {
                player.playerPhotoPath = photoPath;
                SavePlayersData();
                UploadCurrentPlayer(); // Upload to Google Sheets if needed
            }
        }
        #endregion
    }
}