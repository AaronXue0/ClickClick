using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ClickClick.Rank
{
    [System.Serializable]
    public class RankData
    {
        public RankData(int rank, int score, int playerId)
        {
            this.rank = rank;
            this.score = score;
            this.playerId = playerId;
        }

        [HideInInspector]
        public int rank;
        [HideInInspector]
        public Sprite sprite;
        [HideInInspector]
        public Texture rawImageTexture;
        [HideInInspector]
        public int score;
        [HideInInspector]
        public int playerId;
        [HideInInspector]
        public string photoPath;
    }
}