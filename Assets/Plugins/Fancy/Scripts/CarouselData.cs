using System;

namespace Carousel.Scripts
{
    public class CarouselData
    {
        public string SpriteResourceKey { get; }
        public string PlayerAvatarResourceKey { get; }
        public string Text { get; }
        public int Score { get; }
        public Action Clicked { get; }

        public CarouselData(string spriteResourceKey, string playerAvatarResourceKey, string text, int score, Action clicked)
        {
            SpriteResourceKey = spriteResourceKey;
            PlayerAvatarResourceKey = playerAvatarResourceKey;
            Text = text;
            Score = score;
            Clicked = clicked;
        }
    }
}
