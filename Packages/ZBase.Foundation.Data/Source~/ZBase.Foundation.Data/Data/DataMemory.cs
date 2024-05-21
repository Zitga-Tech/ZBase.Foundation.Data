using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ZBase.Foundation.Data
{
    public readonly struct DataMemory<T> : IEquatable<DataMemory<T>>
    {
        private readonly ReadOnlyMemory<T> _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataMemory(ReadOnlyMemory<T> value)
        {
            _value = value;
        }

        public readonly bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value.IsEmpty == false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref readonly T GetValueByRef()
        {
            ThrowIfInvalid(IsValid);
            return ref _value.Span[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref readonly T GetValueByRef(ref T defaultValue)
        {
            return ref IsValid ? ref _value.Span[0] : ref defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T GetValue()
        {
            ThrowIfInvalid(IsValid);
            return _value.Span[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T GetValue(T defaultValue)
        {
            return IsValid ? _value.Span[0] : defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(DataMemory<T> other)
        {
            return _value.Equals(other._value);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is DataMemory<T> other && _value.Equals(other._value);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return _value.IsEmpty == false
                ? HashCode.Combine(RuntimeHelpers.GetHashCode(_value), 0, _value.Length)
                : 0;
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ThrowIfInvalid(bool isValid)
        {
            if (isValid == false)
            {
                throw new InvalidOperationException("DataRef is invalid");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(DataMemory<T> left, DataMemory<T> right)
        {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(DataMemory<T> left, DataMemory<T> right)
        {
            return !(left == right);
        }
    }
}
