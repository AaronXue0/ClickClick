using UnityEngine;

namespace ClickClick.Data
{
    [System.Serializable]
    public class PlayerData
    {
        public int PlayerId;
        public int CharacterId;
        public int Score = 0;
        public int Rank = 99999;

        public string PlayerPhotoPath;


        public PlayerData() { }
        public PlayerData(int rank, int score)
        {
            this.Rank = rank;
            this.Score = score;
        }
        public PlayerData(int rank, int score, string playerPhotoPath)
        {
            this.Rank = rank;
            this.Score = score;
            this.PlayerPhotoPath = playerPhotoPath;
        }
    }
}