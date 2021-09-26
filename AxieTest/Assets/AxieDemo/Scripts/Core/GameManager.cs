using System;
using System.Collections.Generic;
using Axie.Core.HexMap;
using Axie.Core.Simulation;
using UnityEngine;

namespace Axie.Core
{
    [System.Serializable]
    public class BattleData
    {
        public AxieModel axie;
        public int turn;
        public int newHp;
        public int oldHp;
        public bool isDie;
        public int damageDealt;
        public int damageReceived;
    }

    public delegate void AxieBornDelegate(AxieModel axie);
    public delegate void AxieMoveDelegate(int turn, AxieModel axie, Hex oldPos, Hex newPos);
    public delegate void AxieAttackDelegate(BattleData attacker, BattleData defender);
    public delegate void AxieIdleDelegate(AxieModel axie);
    public delegate void MapExpandDelegate(Dictionary<Hex, AxieModel> newAddedList);
    public delegate void TurnInfoDelegate(int attackCount, int defendCount);

    /// <summary>
    /// Game Manager to manage game logic
    /// </summary>
    public class GameManager : ICloneable
    {
        //private Faction attacker;
        //private Faction defender;

        private HexMap.HexMap hexMap;

        private AxieCatalog catalog;

        //public Faction Attacker => attacker;
        //public Faction Defender => defender;
        public HexMap.HexMap Map => hexMap;

        public int fps;

        public bool waitMapExpand;

        public event AxieBornDelegate OnAxieBorn;
        public event AxieMoveDelegate OnAxieMove;
        public event AxieAttackDelegate OnAxieAttack;
        public event AxieIdleDelegate OnAxieIdle;
        public event MapExpandDelegate OnMapExpanded;
        public event TurnInfoDelegate OnTurnInfoUpdate;

        private GameSimulation simulation;

        private Queue<ICommand> commands = new Queue<ICommand>();
        private float timer = 0;
        private float updateDuration = 2f;

        public GameManager(AxieCatalog catalog, HexMap.HexMap hexMap)
        {
            this.catalog = catalog;
            this.hexMap = hexMap;
        }

        public GameManager(HexMap.HexMap map)
        {
            this.hexMap = map;
            //this.attacker = attacker;
            //this.defender = defender;
        }

        public void Initialize()
        {
            //attacker = new Faction(FactionType.Attacker);
            //defender = new Faction(FactionType.Defender);

            // Spawn defender
            for (int q = -2; q <= 2; ++q)
            {
                int r1 = Mathf.Max(-2, -q - 2);
                int r2 = Mathf.Min(2, -q + 2);
                for (int r = r1; r <= r2; ++r)
                {
                    SpawnDefender(new Hex(q, r, -q - r));
                }
            }

            // Spawn attacker
            int outlineCount = 2;
            int qRangeMin = -hexMap.mapRadius + outlineCount;
            int qRangeMax = hexMap.mapRadius - outlineCount;
            for (int q = -hexMap.mapRadius; q <= hexMap.mapRadius; ++q)
            {
                int r1 = Mathf.Max(-hexMap.mapRadius, -q - hexMap.mapRadius);
                int r2 = Mathf.Min(hexMap.mapRadius, -q + hexMap.mapRadius);
                for (int r = r1; r <= r2; ++r)
                {
                    if (q < qRangeMin)
                    {
                        SpawnAttacker(new Hex(q, r, -q - r));
                    }
                    else if (q > qRangeMax)
                    {
                        SpawnAttacker(new Hex(q, r, -q - r));
                    }
                    else
                    {
                        if (r < r1 + outlineCount || r > r2 - outlineCount)
                        {
                            SpawnAttacker(new Hex(q, r, -q - r));
                            if (r + 1 == (r1 + outlineCount))
                            {
                                r = r2 - outlineCount;
                            }
                        }
                    }
                }
            }

            simulation = new GameSimulation(this);
            simulation.RunSimulation();

            timer = updateDuration * 0.8f;
        }

        public void End()
        {
            simulation.StopSimulation();
        }

        public void Update()
        {
            if (simulation == null)
                return;
            if (waitMapExpand)
                return;

            timer += Time.deltaTime;
            if (timer >= updateDuration)
            {
                timer -= updateDuration;
                lock (GameSimulation.s_fpsLock)
                {
                    simulation.fpsValue = fps;
                }
                lock (GameSimulation.s_commandLock)
                {
                    if (simulation.savedCommands.Count > 0)
                    {
                        var list = simulation.savedCommands.Dequeue();
                        commands = list;
                    }
                }
            }

            if (commands.Count > 0)
            {
                foreach (var cmd in commands)
                {
                    if (cmd is MoveCommand move)
                    {
                        AxieModel axie = null;

                        axie = hexMap.cellMap[move.from];
                        hexMap.cellMap[move.from] = null;
                        axie.hex = move.to;
                        hexMap.cellMap[move.to] = axie;

                        OnAxieMove?.Invoke(move.Turn, axie, move.from, move.to);
                    }
                    else if (cmd is AttackCommand atkCommand)
                    {
                        AxieModel attackAxie = null;
                        AxieModel defendAxie = null;

                        attackAxie = hexMap.cellMap[atkCommand.attacker.position];
                        attackAxie.currentHp = atkCommand.attacker.hpNew;
                        if (atkCommand.attacker.isDie)
                        {
                            hexMap.RemoveAxieAt(atkCommand.attacker.position);
                        }

                        defendAxie = hexMap.cellMap[atkCommand.defender.position];
                        defendAxie.currentHp = atkCommand.defender.hpNew;

                        if (atkCommand.defender.isDie)
                        {
                            hexMap.RemoveAxieAt(atkCommand.defender.position);
                        }

                        OnAxieAttack?.Invoke(new BattleData()
                        {
                            turn = atkCommand.turn,
                            axie = attackAxie,
                            newHp = atkCommand.attacker.hpNew,
                            oldHp = atkCommand.attacker.hpOld,
                            isDie = atkCommand.attacker.isDie,
                            damageDealt = atkCommand.attacker.damageDealt,
                            damageReceived = atkCommand.attacker.damageReceived
                        },
                        new BattleData()
                        {
                            turn = atkCommand.turn,
                            axie = defendAxie,
                            newHp = atkCommand.defender.hpNew,
                            oldHp = atkCommand.defender.hpOld,
                            isDie = atkCommand.defender.isDie,
                            damageDealt = atkCommand.defender.damageDealt,
                            damageReceived = atkCommand.defender.damageReceived
                        });
                    }
                    else if (cmd is MapExpandCommand expand)
                    {
                        waitMapExpand = true;
                        hexMap.mapRadius += expand.increase;
                        foreach (var item in expand.axies)
                        {
                            hexMap.AddAxie(item.Key, item.Value);
                        }

                        OnMapExpanded?.Invoke(expand.axies);
                    }
                    else if (cmd is TurnInfoCommand turnInfo)
                    {
                        OnTurnInfoUpdate?.Invoke(turnInfo.attackCount, turnInfo.defendCount);
                    }
                }

                commands.Clear();
            }
        }

        private AxieModel SpawnAttacker(Hex position)
        {
            var axie = hexMap.CreateAxieAt(position, catalog.GetConfig("attacker"));
            OnAxieBorn?.Invoke(axie);

            return axie;
        }

        private AxieModel SpawnDefender(Hex position)
        {
            var axie = hexMap.CreateAxieAt(position, catalog.GetConfig("defender"));
            OnAxieBorn?.Invoke(axie);

            return axie;
        }

        public object Clone()
        {
            return new GameManager(hexMap.Clone() as HexMap.HexMap);
        }
    }
}