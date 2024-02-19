using System;
using ZBase.Foundation.Data;

namespace Samples
{
    public partial class HeroData : IData
    {
        [DataProperty]
        public IdData Id => Get_Id();

        [DataProperty]
        public string Name => Get_Name();

        [DataProperty]
        public StatData Stat => Get_Stat();

        [DataProperty]
        public ReadOnlyMemory<StatMultiplierData> Multipliers => Get_Multipliers();
    }
}