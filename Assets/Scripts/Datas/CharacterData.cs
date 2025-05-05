using UnityEngine;

namespace ClickClick.Data
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "CharacterData", menuName = "ClickClick/CharacterData")]
    public class CharacterData : ScriptableObject
    {
        [HideInInspector] public int characterId;
        public string characterName;
        public Sprite characterSprite;
        public Sprite photoMaskSprite;
    }
}