#pragma warning disable CA1040 // Avoid empty interfaces

namespace ZBase.Foundation.Data
{
    public interface IData { }

    public interface IDataWithId<TDataId> : IData
    {
        TDataId Id { get; }
    }
}
