namespace ZBase.Foundation.Data
{
    public interface IData { }

    public interface IDataWithId<TDataId> : IData
    {
        TDataId Id { get; }
    }
}
