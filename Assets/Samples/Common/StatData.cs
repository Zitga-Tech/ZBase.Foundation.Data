using UnityEngine;
using ZBase.Foundation.Data;

namespace ZBase.Foundation.Data.Samples
{
    public partial class StatData : IData
    {
        [SerializeField]
        private IntWrapper _hp;

        [SerializeField]
        private FloatWrapper _atk;
    }
}