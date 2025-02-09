using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ClickClick.Rank
{
    [System.Serializable]
    public class RankData
    {
        public TMP_Text rankText;
        public TMP_Text scoreText;
        public UnityEngine.UI.Image avatarImage;
        public UnityEngine.UI.Image playerPhotoImage;

        public RankData(int rank, int score)
        {
            this.rank = rank;
            this.score = score;
        }

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