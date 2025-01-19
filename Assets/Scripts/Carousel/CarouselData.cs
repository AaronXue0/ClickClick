using System;
using UnityEngine;

namespace Carousel.Scripts
{
    public class CarouselData
    {
        public Sprite AvatarSprite { get; }
        public Sprite PhotoPath { get; }
        public int Rank { get; }
        public int Score { get; }
        public Action Clicked { get; }

        public CarouselData(Sprite avatarSprite, Sprite photoPath, int rank, int score, Action clicked)
        {
            AvatarSprite = avatarSprite;
            PhotoPath = photoPath;
            Rank = rank;
            Score = score;
            Clicked = clicked;
        }
    }
}
