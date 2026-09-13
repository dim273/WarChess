using System;

namespace WarChess.Domain
{
    // 纯C#的不可变棋盘坐标
    public readonly struct GridCoord : IEquatable<GridCoord>
    {
        public GridCoord(int x, int z)
        {
            X = x;
            Z = z;
        }

        public int X { get; }
        public int Z { get; }

        public int ManhattanDistance(GridCoord other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Z - other.Z);
        }

        public bool Equals(GridCoord other)
        {
            return X == other.X && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is GridCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Z;
            }
        }

        public override string ToString()
        {
            return $"({X}, {Z})"; 
        }

        public static bool operator ==(GridCoord left, GridCoord right) => left.Equals(right);
        public static bool operator !=(GridCoord left, GridCoord right) => !left.Equals(right);
        public static GridCoord operator +(GridCoord left, GridCoord right) => new GridCoord(left.X + right.X, left.Z + right.Z);
    }
}