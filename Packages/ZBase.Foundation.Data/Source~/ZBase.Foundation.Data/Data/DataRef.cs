using System;
using System.Runtime.CompilerServices;

namespace ZBase.Foundation.Data
{
    public readonly ref struct DataRef<TData> where TData : IData
    {
        private readonly ReadOnlySpan<TData> _ref;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataRef(ReadOnlySpan<TData> @ref)
        {
            _ref = @ref;
        }

        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _ref.IsEmpty == false;
        }

        public ref readonly TData Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _ref[0];
        }
    }
}
