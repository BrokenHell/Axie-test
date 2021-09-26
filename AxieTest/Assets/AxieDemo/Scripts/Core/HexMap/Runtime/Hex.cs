using UnityEngine;

namespace Axie.Core.HexMap
{
    /// <summary>
    /// Struct represent for Hex map
    /// Reference : https://www.redblobgames.com/grids/hexagons/
    /// </summary>
    [System.Serializable]
    public struct Hex
    {
        //Column
        public int q;
        //Row
        public int r;
        //Neighbor
        public int s;

        /// <summary>
        /// Constructor with Cube Coordinates
        /// </summary>
        /// <param name="q"></param>
        /// <param name="r"></param>
        /// <param name="s"></param>
        public Hex(int q, int r, int s)
        {
            this.q = q;
            this.r = r;
            this.s = s;
        }

        /// <summary>
        /// Constructor with Axial Coordinates
        /// </summary>
        /// <param name="q"></param>
        /// <param name="r"></param>
        /// <param name="s"></param>
        public Hex(int q, int r)
        {
            this.q = q;
            this.r = r;
            this.s = -q - r;
        }

        public long GetHash()
        {

            var hq = this.q.GetHashCode();
            var hr = this.r.GetHashCode();

            return hq ^ (hr + 0x9e3779b9 + (hq << 6) + (hq >> 2));
        }

        public override bool Equals(object obj)
        {
            var other = (Hex)obj;
            if (other.GetHash() != this.GetHash())
            {
                return false;
            }

            return this.q == other.q &&
                this.r == other.r &&
                this.s == other.s;
        }

        public static float Distance(Hex a, Hex b)
        {
            return Lenght(Subtract(a, b));
        }

        public static int Lenght(Hex hex)
        {
            return (int)((Mathf.Abs(hex.q) + Mathf.Abs(hex.r) + Mathf.Abs(hex.s)) / 2);
        }

        public static Hex Add(Hex a, Hex b)
        {
            return new Hex(a.q + b.q, a.r + b.r, a.s + b.s);
        }

        public static Hex Subtract(Hex a, Hex b)
        {
            return new Hex(a.q - b.q, a.r - b.r, a.s - b.s);
        }

        public static Hex Multiply(Hex a, int k)
        {
            return new Hex(a.q * k, a.r * k, a.s * k);
        }

        public override string ToString()
        {
            return $"q={q};r={r};s={s}";
        }
    }
}
