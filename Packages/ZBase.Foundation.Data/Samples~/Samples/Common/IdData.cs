using ZBase.Foundation.Data;

namespace ZBase.Foundation.Data.Samples
{
    public partial struct IdData : IData
    {
        [DataProperty]
        public EntityKind Kind { readonly get => Get_Kind(); init => Set_Kind(value); }

        [DataProperty]
        public int Id { readonly get => Get_Id(); init => Set_Id(value); }
    }
}
