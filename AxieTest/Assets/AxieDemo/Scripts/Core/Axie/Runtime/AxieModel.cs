using System;
using Axie.Core.HexMap;

namespace Axie.Core
{
    [System.Serializable]
    public class AxieModel : ICloneable
    {
        public int id;
        public Hex hex;

        public string configId;

        public int currentHp;
        public int maxHp;

        public int movement;

        public bool justMoved;
        public bool hasAttacked;

        public FactionType faction;

        public object Clone()
        {
            return new AxieModel()
            {
                id = id,
                hex = hex,
                configId = configId,
                currentHp = currentHp,
                maxHp = maxHp,
                movement = movement,
                faction = faction
            };
        }
    }
}