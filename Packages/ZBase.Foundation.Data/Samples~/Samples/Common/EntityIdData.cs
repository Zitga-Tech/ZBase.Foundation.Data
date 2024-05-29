using ZBase.Foundation.Data;

namespace ZBase.Foundation.Data.Samples
{
    public partial struct EntityIdData : IData
    {
        [DataProperty]
        public EntityKind Kind { readonly get => Get_Kind(); init => Set_Kind(value); }

        [DataProperty]
        public int SubId { readonly get => Get_SubId(); init => Set_SubId(value); }
    }
}
