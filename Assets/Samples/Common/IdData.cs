using ZBase.Foundation.Data;

namespace Samples
{
    public partial class IdData : IData
    {
        [DataProperty]
        public EntityKind Kind => Get_Kind();

        [DataProperty]
        public int Id => Get_Id();
    }
}
