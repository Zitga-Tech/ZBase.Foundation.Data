using UnityEngine;
using ZBase.Foundation.Data;

namespace Samples
{
    public partial class EnemyData : IData
    {
        [SerializeField]
        private IdData _id;

        [SerializeField]
        private string _name;

        [SerializeField]
        private StatData _stat;
    }
}