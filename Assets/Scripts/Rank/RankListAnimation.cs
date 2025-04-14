using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using ClickClick.Manager;

namespace ClickClick.Rank
{
    public class RankListAnimation : MonoBehaviour
    {
        [SerializeField] private Transform rankParent;
        [SerializeField] private List<RankObject> _rankObjects;
        [SerializeField] private float revealDuration = 3f; // Duration of the reveal animation
        [SerializeField] private float numberChangeInterval = 0.05f; // How fast numbers change

        [SerializeField] private GameObject targetButton;

        [Header("Audio")]
        [SerializeField] private AudioController drumRollAudio;
        [SerializeField] private AudioController showRankAudio;

        private List<GameObject> _rankGameObjects = new List<GameObject>();
        private Transform playerRankObject;
        private int _targetRank;

        // private bool _isTestMode = false;

        private void Start()
        {
            // if (_isTestMode)
            // {
            //     TestMode();
            // }

            // If rankDataList is set in the inspector, use its last element as the player UI.
            if (_rankObjects != null && _rankObjects.Count > 0)
            {
                playerRankObject = _rankObjects[_rankObjects.Count - 1].transform;
            }
            else if (rankParent != null)
            {
                // Otherwise, populate rankObjects from rankParent
                _rankGameObjects.Clear();
                foreach (Transform child in rankParent)
                {
                    _rankGameObjects.Add(child.gameObject);
                }
                if (_rankGameObjects.Count > 0)
                {
                    playerRankObject = rankParent.GetChild(rankParent.childCount - 1);
                }
            }

            Invoke("InvokeAnimation", 1f);
        }

        private void InvokeAnimation()
        {
            (List<RankData> players, int targetRank) = RankManager.Instance.GetDatasForRankList();
            Debug.Log("targetRank: " + targetRank);
            Debug.Log("players: " + players.Count);
            foreach (var player in players)
            {
                Debug.Log("player: " + player.rank + " " + player.score);
            }

            PlayAnimation(players, targetRank, () => { });
        }

        // private void TestMode()
        // {
        //     DataManager.Instance.CreateNewPlayer();
        //     DataManager.Instance.UpdateCurrentPlayerScore(Random.Range(100, 10000));
        // }

        public void PlayAnimation(List<RankData> players, int targetRank, System.Action onComplete)
        {
            SetScores(players);
            SetPlayerPhoto(players);
            SetRanks(targetRank);

            _targetRank = targetRank;
            StartCoroutine(RevealRankSequence(onComplete));
        }

        private void SetPlayerPhoto(List<RankData> players)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].playerId != -1)
                {
                    Debug.Log("Setting photo for " + _rankObjects[i].name + " to " + players[i].photoPath);
                    PhotoLoader.LoadPlayerPhoto(_rankObjects[i].photoImage, players[i].photoPath);
                    _rankObjects[i].SetAvatar(DataManager.Instance.GetCharacterSprite(players[i].playerId));
                }
                else
                {
                    Debug.Log("No photo for " + _rankObjects[i].name + " to " + players[i].playerId);
                }
            }
        }

        private void SetScores(List<RankData> players)
        {
            if (_rankObjects == null || _rankObjects.Count == 0)
            {
                Debug.LogError("_rankObjects list is not assigned or empty in RankListAnimation.");
                return;
            }

            int count = Mathf.Min(_rankObjects.Count, players.Count);
            for (int i = 0; i < count; i++)
            {
                if (_rankObjects[i] == null)
                {
                    Debug.LogWarning("RankObject at index " + i + " is null.");
                    continue;
                }
                Debug.Log("Setting score for " + _rankObjects[i].name + " to " + players[i].score);
                _rankObjects[i].SetScore(players[i].score);
            }
        }

        private void SetRanks(int targetRank)
        {
            int[] desiredRanks;
            if (targetRank == 1)
            {
                desiredRanks = new int[] { 2, 3, 4, 5, 1 };
            }
            else if (targetRank == 2)
            {
                desiredRanks = new int[] { 1, 3, 4, 5, 2 };
            }
            else
            {
                desiredRanks = new int[]
                {
                    targetRank - 2,
                    targetRank - 1,
                    targetRank + 1,
                    targetRank + 2,
                    targetRank
                };
            }

            for (int i = 0; i < Mathf.Min(desiredRanks.Length, _rankObjects.Count); i++)
            {
                _rankObjects[i].SetRank(desiredRanks[i]);
            }
        }

        private IEnumerator RevealRankSequence(System.Action onComplete)
        {
            Debug.Log("Starting RevealRankSequence");
            float elapsedTime = 0f;

            // Set target player's rank from the separately passed current player's data
            int targetRank = _targetRank;

            // Start random number animation for all non-player ranks
            Coroutine randomizeCoroutine = StartCoroutine(RandomizeNumbers());
            drumRollAudio.DoAction();

            // Animate player's displayed rank from an initial value to the target rank.
            int initialPlayerDisplayedRank = 9999;
            while (elapsedTime < revealDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / revealDuration);
                int currentDisplayedRank = Mathf.RoundToInt(Mathf.Lerp(initialPlayerDisplayedRank, targetRank, t));
                // Directly update the player's rank display (assuming player's object is the last in rankDataList)
                _rankObjects[_rankObjects.Count - 1].SetRankDisplay(currentDisplayedRank);
                yield return null;
            }

            // Stop the randomization animation
            if (randomizeCoroutine != null)
                StopCoroutine(randomizeCoroutine);

            int rank = _rankObjects.Min(ro => ro.Rank);
            int index = 0;
            foreach (RankObject rankObject in _rankObjects.Where(ro => ro.Rank != 9999))
            {
                rankObject.SetRankDisplay(rank + index);
                rankObject.SetScoreDisplay();
                index++;
            }
            _rankObjects.Where(ro => ro.Rank == 9999).ToList().ForEach(ro => ro.SetRankDisplay(ro.Rank));

            // Determine player position to animate (adjust based on target player's rank) 
            int targetPosition;
            if (targetRank == 1)
                targetPosition = 0;
            else if (targetRank == 2)
                targetPosition = 1;
            else
                targetPosition = 2;

            yield return StartCoroutine(AnimatePlayerToPosition(targetPosition));

            yield return new WaitForSeconds(1f);
            yield return StartCoroutine(DisplayFinalRanks());

            onComplete?.Invoke();
        }

        private IEnumerator AnimatePlayerToPosition(int targetIndex)
        {
            // Use the current sibling index rather than an externally stored count.
            while (playerRankObject.GetSiblingIndex() > targetIndex)
            {
                int newIndex = playerRankObject.GetSiblingIndex() - 1;
                playerRankObject.SetSiblingIndex(newIndex);
                yield return new WaitForSeconds(0.15f);
            }
            yield return null;
        }

        private IEnumerator RandomizeNumbers()
        {
            while (true)
            {
                int index = 0;
                foreach (RankObject rankObj in _rankObjects)
                {
                    int randomRank = Random.Range(1, 10000);
                    int randomScore = Random.Range(0, 10000);

                    rankObj.SetRankDisplay(randomRank);
                    rankObj.SetScoreDisplay(randomScore);
                    index++;
                }
                yield return new WaitForSeconds(numberChangeInterval);
            }
        }

        private IEnumerator DisplayFinalRanks()
        {
            showRankAudio.DoAction();
            yield return new WaitForSeconds(0.1f);

            var orderedRanks = GetOrderedRankObjects();
            foreach (var rank in orderedRanks)
            {
                rank.SetRankDisplay();
                rank.SetScoreDisplay();
            }

            targetButton.SetActive(true);
        }

        private List<RankObject> GetOrderedRankObjects()
        {
            return _rankObjects.OrderBy(ro => ro.transform.GetSiblingIndex()).ToList();
        }
    }
}
