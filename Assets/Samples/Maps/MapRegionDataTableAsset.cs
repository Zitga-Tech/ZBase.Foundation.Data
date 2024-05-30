using System.Runtime.CompilerServices;
using ZBase.Foundation.Data;

namespace ZBase.Foundation.Data.Samples
{
    public sealed partial class MapRegionDataTableAsset : DataTableAsset<MapRegionIdData, MapRegionData, MapRegionId>, IDataTableAsset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override MapRegionId Convert(MapRegionIdData value)
            => value;
    }
}
