using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace ClickClick.Rank
{
    public class RankObject : MonoBehaviour
    {
        private RankData rankData = new RankData(0, 0);

        [SerializeField] private Image avatarImage;
        [SerializeField] private Image photoImage;

        [SerializeField] private List<Sprite> numberSprites;
        [SerializeField] private List<Image> numberImages;
        [SerializeField] private List<Image> scoreImages;

        public int Score => rankData.score;
        public int Rank => rankData.rank;

        public void SetAvatar(Sprite avatar)
        {
            avatarImage.sprite = avatar;
        }

        public void SetPhoto(Sprite photo)
        {
            photoImage.sprite = photo;
        }

        public void SetRankDisplay(int rank)
        {
            DisplayRankImages(rank);
        }

        public void SetRankDisplay()
        {
            DisplayRankImages(rankData.rank);
        }

        public void SetRank(int rank)
        {
            rankData.rank = rank;
        }

        public void SetScoreDisplay(int score)
        {
            DisplayScoreImages(score);
        }

        public void SetScoreDisplay()
        {
            DisplayScoreImages(rankData.score);
        }

        public void SetScore(int score)
        {
            rankData.score = score;
        }

        private void DisplayRankImages(int rank)
        {
            foreach (var img in numberImages)
            {
                img.sprite = null;
                img.gameObject.SetActive(false);
            }

            string rankString = rank.ToString("D4");
            for (int i = 0; i < rankString.Length; i++)
            {
                int digit = int.Parse(rankString[i].ToString());
                if (i < numberImages.Count)
                {
                    numberImages[i].sprite = numberSprites[digit];
                    numberImages[i].gameObject.SetActive(true);
                }
            }
        }

        private void DisplayScoreImages(int score)
        {
            foreach (var img in scoreImages)
            {
                img.sprite = null;
                img.gameObject.SetActive(false);
            }

            string scoreString = score.ToString("D5");
            for (int i = 0; i < scoreString.Length; i++)
            {
                int digit = int.Parse(scoreString[i].ToString());
                if (i < scoreImages.Count)
                {
                    scoreImages[i].sprite = numberSprites[digit];
                    scoreImages[i].gameObject.SetActive(true);
                }
            }
        }
    }
}