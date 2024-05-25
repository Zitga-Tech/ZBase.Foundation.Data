#pragma warning disable CA2225 // Operator overloads have named alternates

using System.Runtime.CompilerServices;
using ZBase.Foundation.Data;

namespace ZBase.Foundation.Data.Samples
{
    public partial struct MapRegionIdData : IData
    {
        [DataProperty]
        public readonly int MapId => Get_MapId();

        [DataProperty]
        public readonly int Region => Get_Region();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator MapRegionId(MapRegionIdData data)
            => new((ushort)data.MapId, (byte)data.Region);
    }

    public partial struct MapRegionData : IData
    {
        [DataProperty]
        public readonly MapRegionIdData Id => Get_Id();

        [DataProperty]
        public readonly int UnlockCost => Get_UnlockCost();
    }
}
