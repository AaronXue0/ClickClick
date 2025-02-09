using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace ClickClick.Rank
{
    public class RankAssigner : MonoBehaviour
    {
        [SerializeField] private List<RankObject> rankObjects;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                RankData rankData = new RankData(1, 100);

                AssignRanks(new List<RankData> { rankData });
            }
        }

        public void AssignRanks(List<RankData> players)
        {
            for (int i = 0; i < players.Count; i++)
            {
                rankObjects[i].SetRankDisplay(players[i].rank);
                rankObjects[i].SetScoreDisplay(players[i].score);
            }
        }
    }
}