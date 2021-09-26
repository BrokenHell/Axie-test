using UnityEngine;

namespace Axie.Core
{
    [System.Serializable]
    public enum FactionType
    {
        Attacker,
        Defender
    }

    [CreateAssetMenu(fileName = "Axie Config", menuName = "Database/Axie/Axie Config", order = 10)]
    public class AxieConfig : ScriptableObject
    {
        [Header("General Description")]
        public string id;
        public FactionType faction;

        [Header("Stats")]
        public int hp;
        public int movement;

        [Header("Visual")]
        public GameObject prefab;
    }
}