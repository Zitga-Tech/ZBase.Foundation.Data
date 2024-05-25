using UnityEngine;
using ZBase.Foundation.Data;

namespace ZBase.Foundation.Data.Samples
{
    public partial class EnemyIdData : IData
    {
        [SerializeField]
        private IdData _kindId;

        [SerializeField]
        private int _rarity;
    }

    public partial class EnemyData : IData
    {
        [SerializeField]
        private EnemyIdData _id;

        [SerializeField]
        private string _name;

        [SerializeField]
        private StatData _stat;
    }
}