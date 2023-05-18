namespace DataTest
{
    public class Program
    {
        public static void Main()
        {
        }
    }
}

namespace MyGame
{
    using ZBase.Foundation.Data;
    using UnityEngine;

    public enum EntityKind
    {
        Hero,
        Enemy,
    }

    public partial struct IdData : IData
    {
        [SerializeField]
        private EntityKind _kind;

        [SerializeField]
        private int _id;
    }

    public partial struct StatData : IData
    {
        [SerializeField]
        private int _hp;

        [SerializeField]
        private int _atk;
    }

    public partial struct StatMultiplierData : IData
    {
        [SerializeField]
        private int _level;

        [SerializeField]
        private float _hp;

        [SerializeField]
        private float _atk;
    }
}

namespace MyGame.Heroes
{
    using ZBase.Foundation.Data;
    using UnityEngine;

    public partial struct HeroData : IData
    {
        [SerializeField]
        private IdData _id;

        [SerializeField]
        private string _name;

        [SerializeField]
        private StatData _stat;

        [SerializeField]
        private StatMultiplierData[] _multipliers;
    }

    public partial class HeroDataTableAsset : DataTableAsset<IdData, HeroData>
    {
    }
}

namespace MyGame.Enemies
{
    using ZBase.Foundation.Data;
    using UnityEngine;

    public partial struct EnemyData : IData
    {
        [SerializeField]
        private IdData _id;

        [SerializeField]
        private string _name;

        [SerializeField]
        private StatData _stat;
    }

    public abstract class EnemyDataTableAsset<T> : DataTableAsset<IdData, T> where T : IData
    {
    }

    public partial class EnemyDataTableAsset : EnemyDataTableAsset<EnemyData>
    {
    }
}

#if UNITY_EDITOR
namespace MyGame.Authoring
{
    using UnityEngine;
    using ZBase.Foundation.Data.Authoring;

    [Database]
    [CreateAssetMenu(fileName = "SampleDatabase", menuName = "Sample Database", order = 0)]
    public partial class Database : ScriptableObject
    {

    }


    [Table(typeof(Heroes.HeroDataTableAsset), "Hero", NamingStrategy.SnakeCase)]
    [VerticalList(typeof(Heroes.HeroData), nameof(Heroes.HeroData.Multipliers))]
    partial class Database { }


    [Table(typeof(Enemies.EnemyDataTableAsset), "Enemy", NamingStrategy.SnakeCase)]
    partial class Database { }

}
#endif
