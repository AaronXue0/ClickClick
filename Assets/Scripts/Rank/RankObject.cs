using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace ClickClick.Rank
{
    public class RankObject : MonoBehaviour
    {
        private RankData rankData = new RankData(0, 0, -1);

        public Image avatarImage;
        public Image photoImage;

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
            // First deactivate all images
            foreach (var img in numberImages)
            {
                img.sprite = null;
                img.gameObject.SetActive(false);
            }

            string rankString = rank.ToString("D4");
            bool leadingZero = true;

            for (int i = 0; i < rankString.Length; i++)
            {
                int digit = int.Parse(rankString[i].ToString());
                if (i < numberImages.Count)
                {
                    // If it's not zero or we've already encountered a non-zero number
                    if (digit != 0 || !leadingZero)
                    {
                        leadingZero = false;
                        numberImages[i].sprite = numberSprites[digit];
                        numberImages[i].gameObject.SetActive(true);
                    }
                }
            }

            // Always show at least the last digit even if it's zero
            int lastIndex = rankString.Length - 1;
            if (lastIndex < numberImages.Count)
            {
                numberImages[lastIndex].sprite = numberSprites[int.Parse(rankString[lastIndex].ToString())];
                numberImages[lastIndex].gameObject.SetActive(true);
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