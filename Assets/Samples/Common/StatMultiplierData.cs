using UnityEngine;
using ZBase.Foundation.Data;

namespace ZBase.Foundation.Data.Samples
{
    public partial class StatMultiplierData : IData
    {
        [SerializeField]
        private int _level;

        [SerializeField]
        private float _hp;

        [SerializeField]
        private float _atk;
    }
}