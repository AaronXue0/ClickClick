using UnityEngine;
using System.Collections.Generic;
namespace ClickClick.Data
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "CharacterGroup", menuName = "ClickClick/CharacterGroup")]
    public class CharacterGroup : ScriptableObject
    {
        [SerializeField] private List<CharacterData> characterDatas;

        public void Initialize()
        {
            for (int i = 0; i < characterDatas.Count; i++)
            {
                characterDatas[i].characterId = i;
            }
        }

        public List<CharacterData> GetCharacterDatas()
        {
            return characterDatas;
        }

        public CharacterData GetCharacterData(int characterId)
        {
            return characterDatas[characterId];
        }

        public CharacterData GetCharacterData(string characterName)
        {
            return characterDatas.Find(character => character.characterName == characterName);
        }

        public Sprite GetCharacterSprite(int characterId)
        {
            return characterDatas[characterId].characterSprite;
        }

        public Sprite GetCharacterSprite(string characterName)
        {
            return characterDatas.Find(character => character.characterName == characterName).characterSprite;
        }

        public string GetCharacterName(int characterId)
        {
            return characterDatas[characterId].characterName;
        }

        public string GetCharacterName(string characterName)
        {
            return characterDatas.Find(character => character.characterName == characterName).characterName;
        }
    }
}