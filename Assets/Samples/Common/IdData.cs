using ZBase.Foundation.Data;

namespace Samples
{
    public partial class IdData : IData
    {
        [DataProperty]
        public EntityKind Kind => GetValue_Kind();

        [DataProperty]
        public int Id => GetValue_Id();
    }
}
