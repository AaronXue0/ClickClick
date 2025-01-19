using FancyCarouselView.Runtime.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Carousel.Scripts
{
    public class CarouselCell : CarouselCell<CarouselData, CarouselCell>
    {
        [SerializeField] private Image _image;
        [SerializeField] private Text _text;
        [SerializeField] private Button _button;

        private CarouselData _data;

        protected override void Refresh(CarouselData data)
        {
            _data = data;
            _image.sprite = Resources.Load<Sprite>(data.SpriteResourceKey);
            _text.text = data.Text;
        }

        protected override void OnVisibilityChanged(bool visibility)
        {
            if (visibility)
                _button.onClick.AddListener(OnClick);
            else
                _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            _data?.Clicked?.Invoke();
        }
    }
}
