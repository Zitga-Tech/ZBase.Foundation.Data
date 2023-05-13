using System.Runtime.CompilerServices;
using UnityEngine;

namespace ZBase.Foundation.Data
{
    public abstract class DataTableAsset : ScriptableObject { }

    public abstract class DataTableAsset<TDataTable, TData> : DataTableAsset
        where TDataTable : IDataTable<TData>
        where TData : IData
    {
        [SerializeField, SerializeReference]
        private TDataTable _dataTable;

        public TDataTable Ref
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _dataTable;
        }

        internal void SetDataTable(TDataTable dataTable)
        {
            _dataTable = dataTable;
        }
    }
}
