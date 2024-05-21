using ZBase.Foundation.Data;

namespace Samples
{
    public partial struct IdData : IData
    {
        [DataProperty]
        public readonly EntityKind Kind => Get_Kind();

        [DataProperty]
        public readonly int Id => Get_Id();
    }
}
