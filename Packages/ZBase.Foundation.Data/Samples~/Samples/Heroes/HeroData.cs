using System;
using ZBase.Foundation.Data;

namespace ZBase.Foundation.Data.Samples
{
    public partial class HeroData : IData
    {
        [DataProperty]
        public EntityIdData Id => Get_Id();

        [DataProperty]
        public string Name => Get_Name();

        [DataProperty]
        public StatData Stat => Get_Stat();

        [DataProperty]
        public ReadOnlyMemory<StatMultiplierData> Multipliers => Get_Multipliers();
    }
}