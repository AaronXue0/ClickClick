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
            int currentPlayerIndex = sortedPlayers.FindIndex(p => p.Score <= currentPlayer.Score);
            if (currentPlayerIndex < 0) currentPlayerIndex = sortedPlayers.Count;

            // Case 1: Current player has highest score
            if (currentPlayerIndex == 0)
            {
                return sortedPlayers.Take(4).ToList();
            }
            // Case 2: Current player has second highest score
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

                // Add two lower scores
                int lowerStart = currentPlayerIndex;
                result.AddRange(sortedPlayers.Skip(lowerStart).Take(2));

                return result;
            }
        }

        private int CalculateCurrentPlayerRank(List<PlayerData> players, PlayerData currentPlayer)
        {
            if (players.Count == 0) return 1;

            foreach (var player in players)
            {
                if (currentPlayer.Score >= player.Score)
                {
                    return player.Rank;
                }
            }

            return players.Last().Rank + 1;
        }

        private List<RankData> HandleAddPlayer(List<PlayerData> players, PlayerData currentPlayer)
        {
            var result = players.Select(p => new RankData(p.Rank, p.Score, p.PlayerId)).ToList();

            var playerRankData = new RankData(currentPlayer.Rank, currentPlayer.Score, currentPlayer.PlayerId);
            playerRankData.photoPath = currentPlayer.PlayerPhotoPath;
            result.Add(playerRankData);

            return result;
        }
    }
}