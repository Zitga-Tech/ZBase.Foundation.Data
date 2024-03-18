using UnityEngine;
using ZBase.Foundation.Data;

namespace Samples
{
    public partial class StatData : IData
    {
        [SerializeField]
        private IntWrapper _hp;

        [SerializeField]
        private FloatWrapper _atk;
    }
}