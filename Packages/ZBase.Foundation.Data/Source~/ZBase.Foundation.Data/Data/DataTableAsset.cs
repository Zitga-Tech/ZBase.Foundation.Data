#pragma warning disable CA1040 // Avoid empty interfaces

using UnityEngine;

namespace ZBase.Foundation.Data
{
    public interface IDataTableAsset { }

    public abstract class DataTableAsset : ScriptableObject, IDataTableAsset
    {
        internal abstract void SetEntries(object obj);

        internal protected virtual void Initialize() { }

        internal protected virtual void Deinitialize() { }
    }
}
