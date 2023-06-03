using UnityEngine;
using ZBase.Foundation.Data;

namespace Samples
{
    public partial class StatData : IData
    {
        [SerializeField]
        private int _hp;

        [SerializeField]
        private int _atk;
    }
}