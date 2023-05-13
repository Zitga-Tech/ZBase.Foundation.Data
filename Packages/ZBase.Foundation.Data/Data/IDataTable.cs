using System;
using System.Collections.Generic;

namespace ZBase.Foundation.Data
{
    public interface IImmutableDataTable<TData>
        where TData : IData
    {
        ReadOnlyMemory<TData> Rows { get; }
    }

    public interface IDataTable<TData> : IImmutableDataTable<TData>
        where TData : IData
    {
        void AddRange(in ReadOnlyMemory<TData> rows);

        void AddRange<TRows>(TRows rows) where TRows : IEnumerable<TData>;

        void Clear();
    }
}
