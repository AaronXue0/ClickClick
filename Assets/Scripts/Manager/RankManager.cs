using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Random = UnityEngine.Random;

namespace ClickClick.Manager
{
    using ClickClick.Data;
    using ClickClick.Rank;

    public class RankManager : SingletonManager<RankManager>
    {
        public int GetCurrentPlayerRank()
        {
            List<PlayerData> players = DataManager.Instance.GetAllPlayers()
                .OrderByDescending(p => p.Score)
                .ToList();

            if (players.Count <= 0)
            {
                return 1;
            }

            return players.FindIndex(p => p.Score == DataManager.Instance.GetCurrentPlayer().Score) + 1;
        }

        public (List<RankData>, int) GetDatasForRankList()
        {
            DataManager.Instance.Initialize();
            PlayerData currentPlayer = DataManager.Instance.GetCurrentPlayer() ??
                new PlayerData(999, Random.Range(100, 10000));

            // Handle invalid cases
            if (currentPlayer.Score < 0 || DataManager.Instance.GetCurrentPlayer() == null)
            {
                return CreateDefaultRankData(currentPlayer);
            }

            // Get all players except current player, sorted by score
            List<PlayerData> sortedPlayers = DataManager.Instance.GetAllPlayers()
                .OrderByDescending(p => p.Score)
                .Where(p => p != currentPlayer)
                .ToList();

            if (sortedPlayers.Count == 0)
            {
                return CreateDefaultRankData(currentPlayer);
            }

            List<PlayerData> selectedPlayers = SelectPlayersForRank(sortedPlayers, currentPlayer);
            int currentPlayerRank = CalculateCurrentPlayerRank(selectedPlayers, currentPlayer);

            return (HandleAddPlayer(selectedPlayers, currentPlayer), currentPlayerRank);
        }

        private (List<RankData>, int) CreateDefaultRankData(PlayerData currentPlayer)
        {
            currentPlayer.Score = Random.Range(100, 10000);
            return (new List<RankData>()
            {
                new RankData(1, currentPlayer.Score - 10, -1),
                new RankData(2, currentPlayer.Score - 20, -1),
                new RankData(3, currentPlayer.Score - 30, -1),
                new RankData(4, currentPlayer.Score - 40, -1),
                new RankData(9999, currentPlayer.Score, -1),
            }, 1);
        }

        private List<PlayerData> SelectPlayersForRank(List<PlayerData> sortedPlayers, PlayerData currentPlayer)
        {
            // Find index where current player should be inserted (handling ties)
            int currentPlayerIndex = sortedPlayers.FindIndex(p => p.Score < currentPlayer.Score);
            if (currentPlayerIndex < 0) currentPlayerIndex = sortedPlayers.Count;

            // Find how many players have the same score as current player
            int tiedPlayersCount = sortedPlayers.Count(p => p.Score == currentPlayer.Score);

            // Case 1: Current player has highest or tied for highest score
            if (currentPlayerIndex == 0)
            {
                return sortedPlayers.Take(4).ToList();
            }
            // Case 2: Current player has second highest score or tied for second
            else if (currentPlayerIndex == 1)
            {
                var result = new List<PlayerData> { sortedPlayers[0] };
                result.AddRange(sortedPlayers.Skip(1).Take(3));
                return result;
            }
            // Case 3: All other cases - get 2 higher and 2 lower scores
            else
            {
                var result = new List<PlayerData>();

                // Add two higher scores
                int higherStart = Math.Max(0, currentPlayerIndex - 2);
                result.AddRange(sortedPlayers.Skip(higherStart).Take(2));

                // Add two lower scores, skipping tied scores if necessary
                int lowerStart = currentPlayerIndex;
                result.AddRange(sortedPlayers.Skip(lowerStart).Take(2));

                return result;
            }
        }

        private int CalculateCurrentPlayerRank(List<PlayerData> players, PlayerData currentPlayer)
        {
            if (players.Count == 0) return 1;

            // Count how many players have higher scores
            int higherScores = players.Count(p => p.Score > currentPlayer.Score);

            // The rank is one more than the number of players with higher scores
            return higherScores + 1;
        }

        private List<RankData> HandleAddPlayer(List<PlayerData> players, PlayerData currentPlayer)
        {
            var result = new List<RankData>();

            // First convert existing players to RankData
            foreach (var player in players)
            {
                // Calculate rank based on scores
                int rank = players.Count(p => p.Score > player.Score) + 1;
                result.Add(new RankData(rank, player.Score, player.PlayerId));
            }

            // Add current player with correct rank
            int currentPlayerRank = players.Count(p => p.Score > currentPlayer.Score) + 1;
            var playerRankData = new RankData(currentPlayerRank, currentPlayer.Score, currentPlayer.PlayerId);
            playerRankData.photoPath = currentPlayer.PlayerPhotoPath;
            result.Add(playerRankData);

            return result;
        }
    }
}