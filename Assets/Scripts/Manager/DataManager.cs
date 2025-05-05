using UnityEngine;
using ClickClick.Data;
using System.Collections.Generic;
using System.Linq;

namespace ClickClick.Manager
{
    public class DataManager : SingletonManager<DataManager>
    {
        private static int defaultGoalScore = 12000;
        private int _goalScore = defaultGoalScore;
        public int GoalScore
        {
            get => _goalScore;
            set
            {
                _goalScore = value;
                SaveGoalScore();
            }
        }

        [SerializeField] private CharacterGroup characterGroup;
        private GoogleSheetsManager googleSheetsManager;

        private List<PlayerData> players = new List<PlayerData>();
        private int currentPlayerId = 0;  // To track the next player ID
        private PlayerData currentPlayer;  // To track the current active player

        public string CurrentPhotoPath { get; set; }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            SavePlayersData();
        }

        public void Initialize()
        {
            if (characterGroup != null)
            {
                characterGroup.Initialize();
            }

            googleSheetsManager = GetComponent<GoogleSheetsManager>();

            LoadPlayersData();
            LoadGoalScore();
        }

        private void SaveGoalScore()
        {
            PlayerPrefs.SetInt("GoalScore", _goalScore);
            PlayerPrefs.Save();
        }

        private void LoadGoalScore()
        {
            _goalScore = PlayerPrefs.GetInt("GoalScore", defaultGoalScore);
        }

        #region Player
        // Create a new player with default values
        public PlayerData CreateNewPlayer(int characterId = 0)
        {
            if (players.Count == 0)
            {
                currentPlayerId = 0;
            }
            else
            {
                currentPlayerId = players.Count;
            }

            PlayerData newPlayer = new PlayerData
            {
                PlayerId = currentPlayerId++,
                CharacterId = characterId,
                Score = 0,
                Rank = 99999
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
            // Save the entire players list as one JSON string
            string playersJson = JsonUtility.ToJson(new PlayerDataList { players = players });
            PlayerPrefs.SetString("Players_Data", playersJson);
            PlayerPrefs.Save();
        }

        // Load all saved players data
        private void LoadPlayersData()
        {
            string jsonData = PlayerPrefs.GetString("Players_Data", "");
            if (!string.IsNullOrEmpty(jsonData))
            {
                PlayerDataList playerDataList = JsonUtility.FromJson<PlayerDataList>(jsonData);
                if (playerDataList != null && playerDataList.players != null)
                {
                    players = playerDataList.players;
                    if (players.Count > 0)
                    {
                        currentPlayerId = players.Max(p => p.PlayerId) + 1;
                    }
                }
            }

            RecalculateRanks();

            Debug.Log("Players loaded: " + players.Count);
        }

        // Get player by ID
        public PlayerData GetPlayer(int playerId)
        {
            return players.Find(p => p.PlayerId == playerId);
        }

        public List<PlayerData> GetAllPlayers()
        {
            return players.Where(p => p.Score > 0).ToList();
        }

        public void ResetAllData()
        {
            // Clear all players data
            players.Clear();
            currentPlayerId = 0;
            currentPlayer = null;
            CurrentPhotoPath = null;
            _goalScore = defaultGoalScore;

            // Save empty state to PlayerPrefs
            SavePlayersData();
            SaveGoalScore();
        }
        #endregion

        #region Google Sheets
        public void UploadCurrentPlayer()
        {
            UploadPlayerData(GetCurrentPlayer());
        }

        public void UploadPlayerData(PlayerData playerData)
        {
            Debug.Log($"Uploading player data: {playerData.PlayerId}, {playerData.Score}");

            if (playerData.Score > 0)
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
            return characterGroup.GetCharacterSprite(GetCurrentPlayer().CharacterId);
        }

        public Sprite GetCharacterPhotoMaskSprite(int characterId)
        {
            return characterGroup.GetCharacterPhotoMaskSprite(characterId);
        }

        public string GetCharacterName(int characterId)
        {
            return characterGroup.GetCharacterName(characterId);
        }

        public string GetCurrentPlayerName()
        {
            return characterGroup.GetCharacterName(GetCurrentPlayer().CharacterId);
        }

        public PlayerData GetCurrentPlayer()
        {
            if (currentPlayer != null)
            {
                Debug.Log("GetCurrentPlayer: " + currentPlayer.PlayerId);
            }
            return currentPlayer;
        }

        public void UpdatePlayerScore(int playerId, int newScore)
        {
            PlayerData player = GetPlayer(playerId);
            if (player != null)
            {
                player.Score = newScore;
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
            player.Score = newScore;
            RecalculateRanks();
            SavePlayersData();
        }

        private void RecalculateRanks()
        {
            // Filter out zero-score players and sort remaining players by score in descending order
            var sortedPlayers = players.Where(p => p.Score > 0)
                                     .OrderByDescending(p => p.Score)
                                     .ToList();

            // Assign ranks (1-based index)
            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                // Handle tied scores
                if (i > 0 && sortedPlayers[i].Score == sortedPlayers[i - 1].Score)
                {
                    sortedPlayers[i].Rank = sortedPlayers[i - 1].Rank;
                }
                else
                {
                    sortedPlayers[i].Rank = i + 1;
                }
            }
        }

        public List<PlayerData> GetTopPlayers(int count)
        {
            if (players.Count == 0)
            {
                return new List<PlayerData>();
            }

            return players
                .Where(p => p.Score > 0)
                .OrderByDescending(p => p.Score)
                .Take(count)
                .ToList();
        }

        public void AddScoreToCurrentPlayer(int scoreToAdd)
        {
            PlayerData player = GetCurrentPlayer();
            player.Score += scoreToAdd;
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

        public void SetCurrentPlayerPhotoPath(string photoPath)
        {
            PlayerData player = GetCurrentPlayer();
            if (player != null)
            {
                player.PlayerPhotoPath = photoPath;
                SavePlayersData();
            }
        }
        #endregion
    }

    [System.Serializable]
    public class PlayerDataList
    {
        public List<PlayerData> players;
    }
}