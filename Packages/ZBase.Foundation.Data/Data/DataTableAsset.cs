using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ZBase.Foundation.Data
{
    public abstract class DataTableAsset : ScriptableObject
    {
        internal abstract void SetDataTable(object obj);
    }

    public abstract class DataTableAsset<TDataTable, TId, TData> : DataTableAsset
        where TDataTable : IDataTable<TId, TData>
        where TData : IData
    {
        [SerializeField, SerializeReference]
        private TDataTable _table;

        public TDataTable Ref
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _table;
        }

        internal sealed override void SetDataTable(object obj)
        {
            if (obj is TDataTable dataTable)
            {
                _table = dataTable;
            }
            else
            {
                throw new InvalidCastException($"Cannot cast {obj.GetType()} into {typeof(TDataTable)}");
            }
        }
    }
}
