using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Carousel.Scripts
{
    public class CarouselController : MonoBehaviour
    {
        [SerializeField] private SimpleCarouselView _carouselView;
        [SerializeField][Range(1, 15)] private int _bannerCount = 15;
        [SerializeField] private Button _setupButton;
        [SerializeField] private Button _cleanupButton;

        private bool _isSetup;

        private void Start()
        {
            _setupButton.onClick.AddListener(Setup);
            _cleanupButton.onClick.AddListener(Cleanup);

            Setup();
        }

        private void Setup()
        {
            if (_isSetup)
                return;

            var items = Enumerable.Range(0, _bannerCount)
                .Select(i =>
                {
                    var spriteResourceKey = $"Stingray";
                    var text = $"Player Name";
                    return new CarouselData(spriteResourceKey, "", text, 0, null);
                })
                .ToArray();
            _carouselView.Setup(items);
            _isSetup = true;
        }

        private void Cleanup()
        {
            if (!_isSetup)
                return;

            _carouselView.Cleanup();
            _isSetup = false;
        }
    }
}
