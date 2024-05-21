using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ZBase.Foundation.Data
{
    public readonly ref struct DataRef<T>
    {
        private readonly ReadOnlySpan<T> _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataRef(ReadOnlySpan<T> value)
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
            return ref _value[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref readonly T GetValueByRef(ref T defaultValue)
        {
            return ref IsValid ? ref _value[0] : ref defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T GetValue()
        {
            ThrowIfInvalid(IsValid);
            return _value[0];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T GetValue(T defaultValue)
        {
            return IsValid ? _value[0] : defaultValue;
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ThrowIfInvalid(bool isValid)
        {
            if (isValid == false)
            {
                throw new InvalidOperationException("DataRef is invalid");
            }
        }
    }
}
