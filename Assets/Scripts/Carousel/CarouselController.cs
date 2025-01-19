using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ClickClick.Manager;

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

            var players = DataManager.Instance.GetAllPlayers()
                .OrderBy(p => p.rank)  // Sort by rank
                .Select(player =>
                {
                    var characterSprite = DataManager.Instance.GetCharacterSprite(player.characterId);
                    var playerName = DataManager.Instance.GetCharacterName(player.characterId);
                    var photoPath = PhotoLoader.LoadPhotoAsSprite(player.playerPhotoPath);

                    // Create a new CarouselData with player information
                    return new CarouselData(
                        avatarSprite: characterSprite,
                        photoPath: photoPath,
                        rank: player.rank,
                        score: player.score,
                        clicked: () => Debug.Log($"Clicked on player {playerName} with rank {player.rank} and score {player.score}")
                    );
                })
                .ToArray();

            _carouselView.Setup(players);
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
