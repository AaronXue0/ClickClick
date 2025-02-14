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
                .Where(p => p.PlayerId != currentPlayer.PlayerId)
                .OrderByDescending(p => p.Score)
                .ToList();

            if (sortedPlayers.Count == 0)
            {
                return CreateDefaultRankData(currentPlayer);
            }

            List<PlayerData> selectedPlayers = SelectPlayersForRank(sortedPlayers, currentPlayer);
            int currentPlayerRank = CalculateCurrentPlayerRank(sortedPlayers, currentPlayer);

            return (HandleAddPlayer(selectedPlayers, currentPlayer), currentPlayerRank);
        }

        private (List<RankData>, int) CreateDefaultRankData(PlayerData currentPlayer)
        {
            // Ensure current player has a valid score
            currentPlayer.Score = currentPlayer.Score <= 0 ? Random.Range(100, 10000) : currentPlayer.Score;

            var defaultRankData = new List<RankData>()
            {
                new RankData(1, Random.Range(currentPlayer.Score + 10, currentPlayer.Score + 100), -1),
                new RankData(2, Random.Range(currentPlayer.Score + 1, currentPlayer.Score + 9), -1),
                new RankData(3, Random.Range(1, currentPlayer.Score - 1), -1),
                new RankData(4, Random.Range(1, currentPlayer.Score - 1), -1),
                new RankData(9999, currentPlayer.Score, currentPlayer.PlayerId)
                {
                    photoPath = currentPlayer.PlayerPhotoPath
                }
            };

            return (defaultRankData, 5);
        }

        private List<PlayerData> SelectPlayersForRank(List<PlayerData> sortedPlayers, PlayerData currentPlayer)
        {
            // Find index where current player would be inserted
            int currentPlayerIndex = sortedPlayers.FindIndex(p => p.Score < currentPlayer.Score);
            if (currentPlayerIndex < 0) currentPlayerIndex = sortedPlayers.Count;

            var result = new List<PlayerData>();

            if (currentPlayerIndex == 0)
            {
                // Current player is highest - take next 4 players
                result.AddRange(sortedPlayers.Take(4));
            }
            else if (currentPlayerIndex == 1)
            {
                // Current player is second - take first player and next 3
                result.Add(sortedPlayers[0]);
                result.AddRange(sortedPlayers.Skip(1).Take(3));
            }
            else
            {
                // Take 2 players above and 2 below current player's position
                int higherStart = Math.Max(0, currentPlayerIndex - 2);
                result.AddRange(sortedPlayers.Skip(higherStart).Take(2));

                int lowerStart = currentPlayerIndex;
                result.AddRange(sortedPlayers.Skip(lowerStart).Take(2));
            }

            // Ensure we have exactly 4 players (pad with empty data if needed)
            while (result.Count < 4)
            {
                result.Add(new PlayerData(-1, 0));
            }

            // Take only the first 4 if we somehow got more
            return result.Take(4).ToList();
        }

        private int CalculateCurrentPlayerRank(List<PlayerData> allPlayers, PlayerData currentPlayer)
        {
            if (allPlayers.Count == 0) return 1;

            // Count how many players have higher scores
            int higherScores = allPlayers.Count(p => p.Score > currentPlayer.Score);

            // The rank is one more than the number of players with higher scores
            return higherScores + 1;
        }

        private List<RankData> HandleAddPlayer(List<PlayerData> players, PlayerData currentPlayer)
        {
            var result = new List<RankData>();

            // Get up to 4 other players (excluding current player)
            var otherPlayers = players.Take(4).ToList();

            // Fill remaining slots with empty data if we have less than 4 other players
            while (otherPlayers.Count < 4)
            {
                otherPlayers.Add(new PlayerData(-1, 0));
            }

            // Convert other players to RankData, ensuring ranks are calculated correctly
            foreach (var player in otherPlayers)
            {
                if (player.PlayerId == -1)
                {
                    // For empty data, assign a rank that puts it below the current player
                    result.Add(new RankData(9999, 0, -1));
                }
                else
                {
                    int rank = player.Score > currentPlayer.Score ?
                        players.Count(p => p.Score > player.Score) + 1 :
                        players.Count(p => p.Score >= player.Score) + 2;
                    var rankData = new RankData(rank, player.Score, player.PlayerId);
                    rankData.photoPath = player.PlayerPhotoPath;
                    result.Add(rankData);
                }
            }

            // Add current player as the last entry
            var currentPlayerRankData = new RankData(
                CalculateCurrentPlayerRank(players, currentPlayer),
                currentPlayer.Score,
                currentPlayer.PlayerId
            )
            {
                photoPath = currentPlayer.PlayerPhotoPath
            };
            result.Add(currentPlayerRankData);

            return result;
        }
    }
}