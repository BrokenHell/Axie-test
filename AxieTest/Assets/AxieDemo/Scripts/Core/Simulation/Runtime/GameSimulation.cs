using System.Collections.Generic;
using System.Threading;
using System.Linq;
using UnityEngine;
using Axie.Core.HexMap;

namespace Axie.Core.Simulation
{
    /// <summary>
    /// Simulationg game on other threads
    /// </summary>
    public class GameSimulation
    {
        public static object s_commandLock = new object();
        public static object s_fpsLock = new object();

        public Queue<Queue<ICommand>> savedCommands = new Queue<Queue<ICommand>>();
        public bool isStop;
        public int fpsValue;

        private GameManager copyOfGameManager;
        private System.Random rand = new System.Random();
        private List<Hex> tobeRemovedDeadAxie = new List<Hex>();
        private int commandAhead = 10;


        public GameSimulation(GameManager manager)
        {
            copyOfGameManager = manager.Clone() as GameManager;
        }

        public void RunSimulation()
        {
            Thread logicThread = new Thread(Update);
            logicThread.Start();
        }

        public void StopSimulation()
        {
            isStop = true;
        }

        private void Update()
        {
            Debug.Log("[Simulation] - Start");
            int turn = 1;

            while (!isStop)
            //while (turn <= 1)
            {
                Debug.Log($"[Simulation] - Turn {turn}");

                Queue<ICommand> queue = new Queue<ICommand>();
                var hexMap = copyOfGameManager.Map;

                TurnUpdate(ref queue, ref hexMap, turn);

                lock (s_fpsLock)
                {
                    if (fpsValue > 30 || turn < 20)
                    {
                        IncreaseMap(ref queue, turn);
                    }
                }

                int attackFactionExist = 0;
                int defFactionExist = 0;
                foreach (var item in hexMap.activeAxies)
                {
                    if (item.faction == FactionType.Attacker)
                    {
                        attackFactionExist += 1;
                    }
                    else if (item.faction == FactionType.Defender)
                    {
                        defFactionExist += 1;
                    }
                    queue.Enqueue(new TurnInfoCommand()
                    {
                        Turn = turn,
                        attackCount = attackFactionExist,
                        defendCount = defFactionExist,
                    });
                }

                lock (s_commandLock)
                {
                    savedCommands.Enqueue(queue);
                }

                //Sleep to wait main-thread update
                while (savedCommands.Count >= commandAhead)
                {
                    Thread.Sleep(1000);
                }

                turn += 1;


                //if (attackFactionExist == 0 || defFactionExist == 0)
                //{
                //    isStop = false;
                //    break;
                //}

                Thread.Sleep(333);
            }
            Debug.LogError("End Game");
            //End game
        }

        private void TurnUpdate(ref Queue<ICommand> commandQueue,
                                ref HexMap.HexMap hexMap,
                                int turn)
        {
            var axies = hexMap.activeAxies;

            int count = axies.Count;
            tobeRemovedDeadAxie.Clear();

            for (int i = 0; i < count; ++i)
            {
                var attacker = axies[i];
                if (attacker == null || attacker.currentHp <= 0)
                    continue;
                attacker.justMoved = false;
                attacker.hasAttacked = false;
                var neighbors = hexMap.GetNeighbors(attacker.hex);
                AxieModel weakestAxie = null;

                foreach (var neighbor in neighbors)
                {
                    if (!hexMap.IsValid(neighbor))
                        continue;

                    var axie = hexMap.cellMap[neighbor];

                    if (axie != null
                        && axie.currentHp > 0
                        && axie.faction != attacker.faction
                        && !axie.justMoved)
                    {
                        if (weakestAxie != null)
                        {
                            if (weakestAxie.currentHp > axie.currentHp)
                            {
                                weakestAxie = axie;
                            }
                        }
                        else
                        {
                            weakestAxie = axie;
                        }
                    }
                }

                if (weakestAxie != null) // Has target -> attack
                {
                    var damageDealtFromAttacker = GenerateDamage();
                    var damageDealtFromDefender = weakestAxie.hasAttacked ? 0 : GenerateDamage();

                    var defenderOldHp = weakestAxie.currentHp;
                    var attackerOldHp = attacker.currentHp;

                    weakestAxie.currentHp -= damageDealtFromAttacker;
                    attacker.currentHp -= damageDealtFromDefender;

                    //Debug.Log($"[Simulation] - Add Attack Command {attacker.hex}->{weakestAxie.hex}");

                    var command = new AttackCommand()
                    {
                        Turn = turn,
                        Faction = attacker.faction,
                        turn = turn,
                        faction = attacker.faction,
                        attacker = new AttackModel()
                        {
                            position = attacker.hex,
                            hpOld = attackerOldHp,
                            hpNew = attacker.currentHp,
                            damageReceived = damageDealtFromDefender,
                            damageDealt = damageDealtFromAttacker,
                            isDie = attacker.currentHp <= 0
                        },
                        defender = new AttackModel()
                        {
                            position = weakestAxie.hex,
                            hpOld = defenderOldHp,
                            hpNew = weakestAxie.currentHp,
                            damageReceived = damageDealtFromAttacker,
                            damageDealt = damageDealtFromDefender,
                            isDie = weakestAxie.currentHp <= 0
                        }
                    };

                    if (attacker.currentHp <= 0)
                    {
                        tobeRemovedDeadAxie.Add(attacker.hex);
                    }
                    if (weakestAxie.currentHp <= 0)
                    {
                        tobeRemovedDeadAxie.Add(weakestAxie.hex);
                    }

                    attacker.hasAttacked = true;
                    weakestAxie.hasAttacked = true;
                    commandQueue.Enqueue(command);
                }
                else if (!attacker.hasAttacked) // Not found target -> move
                {
                    bool canMove = false;
                    if (attacker.movement > 0)
                    {
                        PathFinding.PathFinder pathFinder = new PathFinding.PathFinder();
                        (bool hasMove, HexMap.Hex nextCell) = pathFinder.FindNextMove(attacker.hex,
                                                                                    attacker.faction,
                                                                                    ref hexMap);
                        canMove = hasMove;
                        //Debug.Log($"[Simulation] - Find Path {attacker.hex}," +
                        //       $"- Has Move {hasMove}" +
                        //       $"- Next Cell {nextCell}");

                        if (canMove)
                        {
                            var fromCell = attacker.hex;

                            hexMap.cellMap[fromCell] = null;
                            attacker.hex = nextCell;
                            hexMap.cellMap[nextCell] = attacker;
                            attacker.justMoved = true;

                            //Debug.Log($"[Simulation] - Add Move Command {fromCell}->{nextCell}");

                            var command = new MoveCommand()
                            {
                                Turn = turn,
                                from = fromCell,
                                Faction = attacker.faction,
                                to = nextCell
                            };
                            commandQueue.Enqueue(command);
                        }
                    }
                }
            }

            //Clean up waves
            foreach (var item in tobeRemovedDeadAxie)
            {
                hexMap.RemoveAxieAt(item);
            }
        }

        private void IncreaseMap(ref Queue<ICommand> commandQueue, int turn)
        {
            int increase = 4;
            var dict = copyOfGameManager.Map.ThreadIncreaseSize(increase);
            var command = new MapExpandCommand()
            {
                Turn = turn,
                increase = increase,
                axies = dict,
            };
            commandQueue.Enqueue(command);
        }

        private int GenerateDamage()
        {
            var attackNum = rand.Next(0, 3);
            var defendNum = rand.Next(0, 3);
            var result = (3 + attackNum - defendNum) % 3;

            if (result == 0)
                return 4;
            if (result == 1)
                return 5;
            if (result == 2)
                return 3;

            return 0;
        }
    }
}
