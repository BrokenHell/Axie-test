using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Axie.Core.HexMap
{
    /// <summary>
    /// Hexagon map style
    /// </summary>
    [System.Serializable]
    public class HexMap : ICloneable
    {
        #region Internal Class

        [System.Serializable]
        public enum Direction
        {
            MiddleRight = 0,
            TopRight,
            TopLeft,
            MiddleLeft,
            BotLeft,
            BotRight
        }

        internal struct Orientation
        {
            public Vector4 f;
            public Vector4 b;
            public float startAngle;
            public Orientation(Vector4 f,
                                Vector4 b,
                                float startAngle)
            {
                this.f = f;
                this.b = b;
                this.startAngle = startAngle;
            }
        }

        #endregion

        #region Fields

        private static object s_lock = new object();
        private static int s_spawnId = 0;

        /// <summary>
        /// Matrix to convert hex -> pixel and reserved
        /// f will be hex -> pixel Matrix
        ///            [ sqrt(3)  sqrt(3)/2 ]
        ///            [ 0              3/2 ]
        /// b will be pixel -> hex Matrix
        ///            [ sqrt(3)/3     -1/3 ]
        ///            [ 0              2/3 ]
        /// </summary>
        private readonly Orientation orientation = new Orientation(new Vector4(Mathf.Sqrt(3f),
                                                                                Mathf.Sqrt(3f) / 2,
                                                                                0,
                                                                                3f / 2f),
                                                                    new Vector4(Mathf.Sqrt(3f) / 3f,
                                                                                -1f / 3f,
                                                                                0,
                                                                                2f / 3f),
                                                                    0.5f);
        /// <summary>
        /// Direction vector
        /// </summary>
        private readonly Dictionary<Direction, Hex> directions = new Dictionary<Direction, Hex>()
        {
            { Direction.MiddleRight, new Hex( 1, 0,-1) },
            { Direction.TopRight,    new Hex( 1,-1, 0) },
            { Direction.TopLeft,     new Hex( 0,-1, 1) },
            { Direction.MiddleLeft,  new Hex(-1, 0, 1) },
            { Direction.BotLeft,     new Hex(-1, 1, 0) },
            { Direction.BotRight,    new Hex( 0, 1,-1) },
        };

        /// <summary>
        /// Origin of the map
        /// </summary>
        public Vector2Int origin;
        /// <summary>
        /// Cell size in unit
        /// </summary>
        public Vector2 cellSize;

        /// <summary>
        /// Map Radiu
        /// </summary>
        public int mapRadius;

        /// <summary>
        /// Map to store hex cells
        /// </summary>
        public Dictionary<Hex, AxieModel> cellMap = new Dictionary<Hex, AxieModel>();

        public List<AxieModel> activeAxies = new List<AxieModel>();

        private List<AxieModel> pools = new List<AxieModel>();

        #endregion

        #region Constructor & Destructor

        public HexMap(Vector2Int origin, Vector2 size, int radius)
        {
            this.origin = origin;
            this.cellSize = size;
            this.mapRadius = radius;

            Initialize();
        }

        public HexMap(Vector2Int origin, Vector2 size, int radius, Dictionary<Hex, AxieModel> map)
        {
            this.origin = origin;
            this.cellSize = size;
            this.mapRadius = radius;

            cellMap.Clear();
            foreach (var item in map)
            {
                AxieModel axie = null;
                if (item.Value != null)
                {
                    axie = item.Value.Clone() as AxieModel;

                    activeAxies.Add(axie);
                }
                cellMap.Add(item.Key, axie);
            }
        }

        public HexMap(int originX, int originY, float cellWidth, float cellHeight, int radius)
        {
            this.origin = new Vector2Int(originX, originY);
            this.cellSize = new Vector2(cellWidth, cellHeight);
            this.mapRadius = radius;

            Initialize();
        }

        #endregion

        #region Public Methods

        public void Initialize()
        {
            for (int q = -mapRadius; q <= mapRadius; ++q)
            {
                int r1 = Mathf.Max(-mapRadius, -q - mapRadius);
                int r2 = Mathf.Min(mapRadius, -q + mapRadius);
                for (int r = r1; r <= r2; ++r)
                {
                    var hex = new Hex(q, r, -q - r);
                    cellMap.Add(hex, null);
                }
            }
        }

        public Dictionary<Hex, AxieModel> ThreadIncreaseSize(int increase)
        {
            Dictionary<Hex, AxieModel> createDict = new Dictionary<Hex, AxieModel>();
            int oldRadius = mapRadius;
            mapRadius += increase;
            int current = oldRadius + 1;
            int turn = 0;
            while (current <= mapRadius)
            {
                if (current % 2 == 0)
                {
                    int q = -current;
                    int r = 0;
                    for (r = 0; r <= current; ++r)
                    {
                        var hex = new Hex(q, r, -q - r);
                        cellMap.Add(hex, null);
                        createDict.Add(hex, null);
                    }

                    r = current;
                    q += 1;
                    for (; q <= current; ++q)
                    {
                        var hex = new Hex(q, r, -q - r);
                        cellMap.Add(hex, null);
                        createDict.Add(hex, null);
                        if (q >= 0)
                        {
                            r -= 1;
                            r = Mathf.Max(r, 0);
                        }
                    }

                    q = current;
                    r -= 1;
                    for (; r >= -current; --r)
                    {
                        var hex = new Hex(q, r, -q - r);
                        cellMap.Add(hex, null);
                        createDict.Add(hex, null);
                    }

                    r = -current;
                    q -= 1;
                    for (; q > -current; --q)
                    {
                        var hex = new Hex(q, r, -q - r);
                        cellMap.Add(hex, null);
                        createDict.Add(hex, null);
                        if (q <= 0)
                        {
                            r += 1;
                            r = Mathf.Min(r, 0);
                        }
                    }
                }
                else
                {
                    FactionType factionType = turn % 2 == 0 ? FactionType.Defender
                                                                : FactionType.Attacker;
                    int q = -current;
                    int r = 0;
                    for (r = 0; r <= current; ++r)
                    {
                        var hex = new Hex(q, r, -q - r);
                        var axie = ThreadCreateAxieAt(hex, factionType);
                        createDict.Add(hex, axie.Clone() as AxieModel);
                    }

                    r = current;
                    q += 1;
                    for (; q <= current; ++q)
                    {
                        var hex = new Hex(q, r, -q - r);
                        var axie = ThreadCreateAxieAt(hex, factionType);
                        createDict.Add(hex, axie.Clone() as AxieModel);
                        if (q >= 0)
                        {
                            r -= 1;
                            r = Mathf.Max(r, 0);
                        }
                    }

                    q = current;
                    r -= 1;
                    for (; r >= -current; --r)
                    {
                        var hex = new Hex(q, r, -q - r);
                        var axie = ThreadCreateAxieAt(hex, factionType);
                        createDict.Add(hex, axie.Clone() as AxieModel);
                    }

                    r = -current;
                    q -= 1;
                    for (; q > -current; --q)
                    {
                        var hex = new Hex(q, r, -q - r);
                        var axie = ThreadCreateAxieAt(hex, factionType);
                        createDict.Add(hex, axie.Clone() as AxieModel);
                        if (q <= 0)
                        {
                            r += 1;
                            r = Mathf.Min(r, 0);
                        }
                    }

                    turn += 1;
                }

                current += 1;
            }

            //int qRangeMin = -mapRadius + increase;
            //int qRangeMax = mapRadius - increase;
            //for (int q = -mapRadius; q <= mapRadius; ++q)
            //{
            //    int r1 = Mathf.Max(-mapRadius, -q - mapRadius);
            //    int r2 = Mathf.Min(mapRadius, -q + mapRadius);
            //    for (int r = r1; r <= r2; ++r)
            //    {
            //        if (q < qRangeMin)
            //        {
            //            var hex = new Hex(q, r, -q - r);
            //            if ((Mathf.Abs(q) - oldRadius) % 2 != 0)//odd
            //            {
            //                var axie = ThreadCreateAxieAt(hex, q == qRangeMin - 1 ?
            //                                                    (r == r1 || r == r2) ?
            //                                        FactionType.Attacker : FactionType.Defender
            //                                        : FactionType.Attacker);
            //                createDict.Add(hex, axie.Clone() as AxieModel);
            //            }


            //        }
            //        else if (q > qRangeMax)
            //        {
            //            var hex = new Hex(q, r, -q - r);
            //            var axie = ThreadCreateAxieAt(hex, q == qRangeMax + 1 ?
            //                (r == r1 || r == r2) ?
            //                FactionType.Attacker : FactionType.Defender
            //                : FactionType.Attacker);
            //            createDict.Add(hex, axie.Clone() as AxieModel);
            //        }
            //        else
            //        {
            //            if (r < r1 + increase || r > r2 - increase)
            //            {
            //                var hex = new Hex(q, r, -q - r);
            //                if (r < r1 + increase)
            //                {
            //                    var axie = ThreadCreateAxieAt(hex, r == r1 + increase - 1 ? FactionType.Defender
            //                                                                                : FactionType.Attacker);
            //                    createDict.Add(hex, axie.Clone() as AxieModel);
            //                }
            //                else if (r > r2 - increase)
            //                {
            //                    var axie = ThreadCreateAxieAt(hex, r == r2 - increase + 1 ? FactionType.Defender
            //                                                                                : FactionType.Attacker);
            //                    createDict.Add(hex, axie.Clone() as AxieModel);
            //                }
            //                if ((r + 1) == (r1 + increase))
            //                {
            //                    r = r2 - increase;
            //                }
            //            }
            //        }
            //    }
            //}

            return createDict;
        }

        public Vector2 HexToPixel(Hex hex)
        {
            float x = (orientation.f.x * hex.q + orientation.f.y * hex.r) * this.cellSize.x;
            float y = (orientation.f.z * hex.q + orientation.f.w * hex.r) * this.cellSize.y;

            return new Vector2(x + this.origin.x, y + this.origin.y);
        }

        public Vector2 HexCornerOffset(int corner)
        {
            float angle = 2f * Mathf.PI * (orientation.startAngle + corner) / 6;

            return new Vector2(this.cellSize.x * Mathf.Cos(angle), this.cellSize.y * Mathf.Sin(angle));
        }

        public List<Vector2> PoligonCorner(Hex h)
        {
            List<Vector2> corners = new List<Vector2>();
            Vector2 center = HexToPixel(h);
            for (int i = 0; i < 6; i++)
            {
                Vector2 offset = HexCornerOffset(i);
                corners.Add(new Vector2(center.x + offset.x,
                                        center.y + offset.y));
            }
            return corners;
        }

        public Hex GetHexDirection(Direction direction)
        {
            return directions[direction];
        }

        public Hex GetNeighbor(Hex current, Direction direction)
        {
            return Hex.Add(current, GetHexDirection(direction));
        }

        public Hex[] GetNeighbors(Hex current)
        {
            Hex[] hexes = new Hex[6];
            for (int i = 0; i < 6; ++i)
            {
                hexes[i] = GetNeighbor(current, (Direction)i);
            }

            return hexes;
        }

        public bool IsValid(Hex hex)
        {
            if (hex.q < -mapRadius || hex.q > mapRadius
                || hex.r < -mapRadius || hex.r > mapRadius
                || hex.s < -mapRadius || hex.s > mapRadius)
            {
                return false;
            }

            return true;
        }

        public float MovementCost(Hex from, Hex to)
        {
            return 0f;
        }

        public object Clone()
        {
            return new HexMap(origin, cellSize, mapRadius, this.cellMap);
        }

        public AxieModel CreateAxieAt(Hex hex, AxieConfig config)
        {
            lock (s_lock)
            {
                if (pools.Count > 0)
                {
                    var axie = pools[pools.Count - 1];
                    axie.id = s_spawnId++;
                    axie.hex = hex;
                    axie.configId = config.id;
                    axie.currentHp = config.hp;
                    axie.maxHp = config.hp;
                    axie.movement = config.movement;
                    axie.faction = config.faction;
                    pools.RemoveAt(pools.Count - 1);

                    activeAxies.Add(axie);
                    cellMap[hex] = axie;

                    return axie;
                }
                else
                {
                    AxieModel axie = new AxieModel()
                    {
                        id = s_spawnId++,
                        hex = hex,
                        configId = config.id,
                        currentHp = config.hp,
                        maxHp = config.hp,
                        movement = config.movement,
                        faction = config.faction
                    };

                    activeAxies.Add(axie);
                    cellMap[hex] = axie;
                    return axie;
                }
            }
        }

        public void AddAxie(Hex hex, AxieModel axie)
        {
            activeAxies.Add(axie);
            cellMap[hex] = axie;
        }

        public AxieModel ThreadCreateAxieAt(Hex hex, FactionType faction)
        {
            lock (s_lock)
            {
                bool isAtker = faction == FactionType.Attacker;
                if (pools.Count > 0)
                {
                    var axie = pools[pools.Count - 1];
                    axie.id = s_spawnId++;
                    axie.hex = hex;
                    axie.configId = isAtker ? "attacker" : "defender";
                    axie.currentHp = isAtker ? 10 : 30;
                    axie.maxHp = axie.currentHp;
                    axie.movement = isAtker ? 1 : 0;
                    axie.faction = faction;
                    pools.RemoveAt(pools.Count - 1);

                    activeAxies.Add(axie);
                    cellMap[hex] = axie;
                    return axie;
                }
                else
                {
                    AxieModel axie = new AxieModel()
                    {
                        id = s_spawnId++,
                        hex = hex,
                        configId = isAtker ? "attacker" : "defender",
                        currentHp = isAtker ? 10 : 30,
                        maxHp = isAtker ? 10 : 30,
                        movement = isAtker ? 1 : 0,
                        faction = faction
                    };

                    activeAxies.Add(axie);
                    cellMap[hex] = axie;
                    return axie;
                }
            }
        }

        public void RemoveAxieAt(Hex hex)
        {
            var axie = cellMap[hex];
            cellMap[hex] = null;
            pools.Add(axie);

            activeAxies.Remove(axie);
        }

        #endregion

        #region Private Methods

        #endregion
    }
}
