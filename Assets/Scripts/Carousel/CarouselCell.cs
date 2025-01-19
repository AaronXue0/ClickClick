using FancyCarouselView.Runtime.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Carousel.Scripts
{
    public class CarouselCell : CarouselCell<CarouselData, CarouselCell>
    {
        [SerializeField] private Image _playerAvatar;
        [SerializeField] private Image _playerPhoto;
        [SerializeField] private Text _rankText;
        [SerializeField] private Text _scoreText;
        [SerializeField] private Button _button;

        private CarouselData _data;

        protected override void Refresh(CarouselData data)
        {
            _data = data;
            _playerPhoto.sprite = data.PhotoPath;
            _playerAvatar.sprite = data.AvatarSprite;
            _rankText.text = data.Rank.ToString();
            _scoreText.text = data.Score.ToString();
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
