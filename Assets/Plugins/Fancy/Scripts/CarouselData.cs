using System;

namespace Carousel.Scripts
{
    public class CarouselData
    {
        public string SpriteResourceKey { get; }
        public string Text { get; }
        public Action Clicked { get; }

        public CarouselData(string spriteResourceKey, string text, Action clicked)
        {
            SpriteResourceKey = spriteResourceKey;
            Text = text;
            Clicked = clicked;
        }
    }
}
