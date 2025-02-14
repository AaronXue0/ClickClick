using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
            PlayerData currentPlayer = DataManager.Instance.GetCurrentPlayer() != null
                ? DataManager.Instance.GetCurrentPlayer()
                : new PlayerData(999, Random.Range(100, 10000));

            // Retrieve all players and sort them by score in descending order.
            List<PlayerData> players = DataManager.Instance.GetAllPlayers()
                .OrderByDescending(p => p.Score)
                .Where(p => p != currentPlayer)
                .ToList();


            // Find the index of the player with the given currentScore.
            int index = players.FindIndex(p => p.Score == currentPlayer.Score);
            if (index < 0 || DataManager.Instance.GetCurrentPlayer() == null || currentPlayer.Score < 0)
            {
                currentPlayer.Score = Random.Range(100, 10000);
                return (new List<RankData>()
                {
                    new RankData(1, currentPlayer.Score - 10),
                    new RankData(2, currentPlayer.Score - 20),
                    new RankData(3, currentPlayer.Score - 30),
                    new RankData(4, currentPlayer.Score - 40),
                    new RankData(9999, currentPlayer.Score),
                }, 1);
            }

            List<PlayerData> result = new List<PlayerData>();

            // If current score is highest, pick the next four (lower scores).
            if (index == 0)
            {
                for (int i = 1; i < players.Count && result.Count < 4; i++)
                {
                    result.Add(players[i]);
                }
            }
            // If current score is the second highest, pick one higher and three lower.
            else if (index == 1)
            {
                // One higher (index 0)
                result.Add(players[0]);
                // Three lower starting from index 2.
                for (int i = 2; i < players.Count && result.Count < 4; i++)
                {
                    result.Add(players[i]);
                }
            }
            // Otherwise, pick two with higher scores and two with lower scores.
            else
            {
                List<PlayerData> temp = new List<PlayerData>();

                // Get up to two players with higher scores.
                int higherCount = System.Math.Min(2, index);
                for (int i = index - higherCount; i < index; i++)
                {
                    temp.Add(players[i]);
                }

                // Get up to two players with lower scores.
                int lowerCount = System.Math.Min(2, players.Count - index - 1);
                for (int i = index + 1; i <= index + lowerCount; i++)
                {
                    temp.Add(players[i]);
                }

                // If we haven't reached a total of 4, try to fill with additional lower scores first.
                int remaining = 4 - temp.Count;
                int extraLowerStart = index + lowerCount + 1;
                while (remaining > 0 && extraLowerStart < players.Count)
                {
                    temp.Add(players[extraLowerStart]);
                    extraLowerStart++;
                    remaining--;
                }

                // Then, if needed, fill the remaining from the higher side.
                int extraHigherIndex = index - higherCount - 1;
                while (remaining > 0 && extraHigherIndex >= 0)
                {
                    // Insert at the beginning to maintain order.
                    temp.Insert(0, players[extraHigherIndex]);
                    extraHigherIndex--;
                    remaining--;
                }
                result = temp;
            }

            // Add null check and handle empty result case
            int currentPlayerRank = 1; // Default rank if no other players
            if (result.Count > 0)
            {
                currentPlayerRank = result[0].Rank;
                foreach (var player in result)
                {
                    if (currentPlayer.Score < player.Score)
                    {
                        currentPlayerRank = player.Rank + 1;
                        break;
                    }
                }
            }

            return (HandleAddPlayer(result, currentPlayer), currentPlayerRank);
        }

        private List<RankData> HandleAddPlayer(List<PlayerData> players, PlayerData currentPlayer)
        {
            var result = players.Select(p => new RankData(p.Rank, p.Score)).ToList();

            result.Add(new RankData(result.Count + 1, currentPlayer.Score));

            return result;
        }
    }
}