using System.Collections.Generic;
using Axie.Core.HexMap;

namespace Axie.Core.Simulation
{
    public interface ICommand
    {
        int Turn { get; set; }
        FactionType Faction { get; set; }
    }

    [System.Serializable]
    public class MoveCommand : ICommand
    {
        public int Turn { get; set; }
        public FactionType Faction { get; set; }
        public Hex from;
        public Hex to;
    }

    [System.Serializable]
    public class AttackModel
    {
        public Hex position;
        public int hpOld;
        public int hpNew;
        public int damageReceived;
        public int damageDealt;
        public bool isDie;
    }

    [System.Serializable]
    public class AttackCommand : ICommand
    {
        public int Turn { get; set; }
        public FactionType Faction { get; set; }
        public int turn;
        public FactionType faction;
        public AttackModel attacker;
        public AttackModel defender;
    }

    [System.Serializable]
    public class MapExpandCommand : ICommand
    {
        public int Turn { get; set; }
        public FactionType Faction { get; set; }
        public int increase;
        public Dictionary<Hex, AxieModel> axies = new Dictionary<Hex, AxieModel>();
    }

    [System.Serializable]
    public class TurnInfoCommand : ICommand
    {
        public int Turn { get; set; }
        public FactionType Faction { get; set; }
        public int attackCount;
        public int defendCount;
    }
}