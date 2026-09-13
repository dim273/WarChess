using System;

namespace WarChess.Domain
{
    // 角色的稳定业务标识
    public readonly struct UnitId: IEquatable<UnitId>
    {
        public string Value { get; }
        public UnitId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("UnitId cannot be null or whitespace.", nameof(value));
            }
            Value = value;
        }

        public bool Equals(UnitId other)
        {
            return Value == other.Value;
        }
        public override bool Equals(object obj)
        {
            return obj is UnitId other && Equals(other);
        }
        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(UnitId left, UnitId right) => left.Equals(right);
        public static bool operator !=(UnitId left, UnitId right) => !left.Equals(right);
    }
}