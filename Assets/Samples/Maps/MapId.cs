#pragma warning disable CA2225 // Operator overloads have named alternates
#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1815 // Override equals and operator equals on value types

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ZBase.Foundation.Data.Samples
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly partial struct MapId
        : IEquatable<MapId>
        , IComparable<MapId>
    {
        [FieldOffset(0)]
        private readonly ushort _raw;

        [FieldOffset(0)]
        public readonly byte Id;

        [FieldOffset(1)]
        public readonly byte WorldId;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MapId(ushort value) : this()
        {
            _raw = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MapId(byte worldId, byte id) : this()
        {
            WorldId = worldId;
            Id = id;
        }

        /// <summary>
        /// The Map ID format consists of 3 to 4 digits:
        /// <list type="bullet">
        /// <item>The first 1 or 2 digits: ID of a World</item>
        /// <item>The last 2 digits: ID of a Map inside that World</item>
        /// </list>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MapId Convert(int value)
            => new((byte)(value / 100), (byte)(value % 100));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToSingleId()
            => WorldId * 100 + Id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(MapId other)
            => _raw == other._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
            => obj is MapId other && _raw == other._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
            => _raw.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
            => ToSingleId().ToString(CultureInfo.InvariantCulture);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(MapId other)
            => _raw.CompareTo(other._raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ushort(MapId id)
            => id._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator MapId(ushort value)
            => new(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(MapId lhs, MapId rhs)
            => lhs._raw == rhs._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(MapId lhs, MapId rhs)
            => lhs._raw != rhs._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(MapId left, MapId right)
            => left._raw > right._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(MapId left, MapId right)
            => left._raw >= right._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(MapId left, MapId right)
            => left._raw < right._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(MapId left, MapId right)
            => left._raw <= right._raw;

        public readonly struct Comparer : IComparer<MapId>
        {
            public static readonly Comparer Default;

            public int Compare(MapId x, MapId y)
                => x.CompareTo(y);
        }
    }
}
