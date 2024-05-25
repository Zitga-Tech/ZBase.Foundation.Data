#pragma warning disable CA2225 // Operator overloads have named alternates
#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1815 // Override equals and operator equals on value types
#pragma warning disable CA2235 // Mark all non-serializable fields

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Samples
{
    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public readonly partial struct MapRegionId
        : IEquatable<MapRegionId>
        , IComparable<MapRegionId>
    {
        [FieldOffset(0)]
        private readonly uint _raw;

        [FieldOffset(0)]
        public readonly byte Region;

        [FieldOffset(2)]
        public readonly MapId MapId;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MapRegionId(uint value) : this()
        {
            _raw = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MapRegionId(ushort mapId, byte region) : this()
        {
            MapId = MapId.Convert(mapId);
            Region = region;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MapRegionId(MapId mapId, byte region) : this()
        {
            MapId = mapId;
            Region = region;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(MapRegionId other)
            => _raw == other._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
            => obj is MapRegionId other && _raw == other._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
            => _raw.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
            => $"{MapId}[{Region}]";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(MapRegionId other)
            => _raw.CompareTo(other._raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator uint(MapRegionId id)
            => id._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator MapRegionId(uint value)
            => new(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(MapRegionId lhs, MapRegionId rhs)
            => lhs._raw == rhs._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(MapRegionId lhs, MapRegionId rhs)
            => lhs._raw != rhs._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(MapRegionId left, MapRegionId right)
            => left._raw > right._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(MapRegionId left, MapRegionId right)
            => left._raw >= right._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(MapRegionId left, MapRegionId right)
            => left._raw < right._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(MapRegionId left, MapRegionId right)
            => left._raw <= right._raw;

        public readonly struct Comparer : IComparer<MapRegionId>
        {
            public static readonly Comparer Default;

            public int Compare(MapRegionId x, MapRegionId y)
                => x.CompareTo(y);
        }
    }
}
