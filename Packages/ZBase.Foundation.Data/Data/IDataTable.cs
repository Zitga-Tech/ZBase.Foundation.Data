namespace ZBase.Foundation.Data
{
    public interface IDataTable<TId, TData> where TData : IData { }

    public interface IGetData<TId, TData> : IDataTable<TId, TData>
        where TData : IData
    {
        TData GetData(TId id);
    }

    public interface ITryGetData<TId, TData> : IDataTable<TId, TData>
        where TData : IData
    {
        bool TryGetData(TId id, out TData data);
    }
}
