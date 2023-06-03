using UnityEngine;
using ZBase.Foundation.Data;

namespace Samples
{
    public partial class IdData : IData
    {
        [SerializeField]
        private EntityKind _kind;

        [SerializeField]
        private int _id;
    }
}
