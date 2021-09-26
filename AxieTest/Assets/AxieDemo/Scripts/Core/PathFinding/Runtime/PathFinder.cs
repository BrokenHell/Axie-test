using System.Collections.Generic;
using System.Linq;
using Axie.Core.HexMap;

namespace Axie.Core.PathFinding
{
    public class PriorityQueue
    {
        private SortedList<double, Hex> elements = new SortedList<double, Hex>();

        public int Count => elements.Count;

        public void Enqueue(Hex hex, double priority)
        {
            elements.Add(priority, hex);
        }

        public Hex Dequeue()
        {
            var element = elements.First();
            elements.Remove(element.Key);

            return element.Value;
        }
    }

    public class PathFinder
    {
        public Dictionary<Hex, Hex> cameFrom = new Dictionary<Hex, Hex>();
        public Dictionary<Hex, double> costSoFar = new Dictionary<Hex, double>();

        public (bool, Hex) FindNextMove(Hex start, FactionType finderFaction, ref HexMap.HexMap map)
        {
            var frontier = new Queue<Hex>();
            frontier.Enqueue(start);

            cameFrom[start] = start;

            Hex goal = new Hex();
            bool hasGoal = false;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();

                if (map.cellMap[current] != null &&
                    map.cellMap[current].faction != finderFaction)
                {
                    goal = current;
                    hasGoal = true;
                    break;
                }

                foreach (var neighbor in map.GetNeighbors(current))
                {
                    if (!map.IsValid(neighbor))
                        continue;
                    var axie = map.cellMap[neighbor];
                    if (axie != null && axie.faction == finderFaction)
                        continue;

                    if (!cameFrom.ContainsKey(neighbor))
                    {
                        frontier.Enqueue(neighbor);
                        cameFrom[neighbor] = current;
                    }
                }
            }

            if (hasGoal)
            {
                var paths = BuildPaths(start, goal);
                if (paths.Count > 0)
                {
                    return (true, paths[0]);
                }

                return (false, goal);
            }
            else
            {
                return (false, goal);
            }
        }

        private List<Hex> BuildPaths(Hex start, Hex goal)
        {
            List<Hex> paths = new List<Hex>();
            Hex current = goal;
            long startHash = start.GetHash();

            while (current.GetHash() != startHash)
            {
                paths.Add(current);
                current = cameFrom[current];
            }

            paths.Reverse();
            paths.RemoveAt(paths.Count - 1);
            return paths;
        }
    }
}