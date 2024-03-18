#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA2225 // Operator overloads have named alternates
#pragma warning disable CA1815 // Override equals and operator equals on value types

using System;

namespace Samples
{
    [Serializable]
    public struct IntWrapper : IEquatable<IntWrapper>
    {
        public int value;

        public IntWrapper(int value)
        {
            this.value = value;
        }

        public readonly override bool Equals(object obj)
            => obj is IntWrapper other && value == other.value;

        public readonly bool Equals(IntWrapper other)
            => value == other.value;

        public readonly override int GetHashCode()
            => value.GetHashCode();

        public static IntWrapper Convert(int value)
            => new(value);

        public static implicit operator IntWrapper(int value)
            => new(value);

        public static bool operator ==(IntWrapper left, IntWrapper right)
            => left.value == right.value;

        public static bool operator !=(IntWrapper left, IntWrapper right)
            => left.value != right.value;
    }

    public struct IntWrapperConverter
    {
        public static IntWrapper Convert(int value)
            => new(value);
    }

    [Serializable]
    public struct FloatWrapper : IEquatable<FloatWrapper>
    {
        public float value;

        public FloatWrapper(float value)
        {
            this.value = value;
        }

        public readonly override bool Equals(object obj)
            => obj is FloatWrapper other && value == other.value;

        public readonly bool Equals(FloatWrapper other)
            => value == other.value;

        public readonly override int GetHashCode()
            => value.GetHashCode();

        public static implicit operator FloatWrapper(float value)
            => new(value);

        public static bool operator ==(FloatWrapper left, FloatWrapper right)
            => left.value == right.value;

        public static bool operator !=(FloatWrapper left, FloatWrapper right)
            => left.value != right.value;
    }

    public struct FloatWrapperConverter
    {
        public static FloatWrapper Convert(float value)
            => new(value);
    }
}