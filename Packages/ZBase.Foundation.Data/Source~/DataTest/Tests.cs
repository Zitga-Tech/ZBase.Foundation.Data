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
    using Newtonsoft.Json;
    using System;

    public enum EntityKind
    {
        Hero,
        Enemy,
    }

    public partial class IdData : IData
    {
        [DataProperty]
        public EntityKind Kind => GetValue_Kind();

        [DataProperty]
        public int Id => GetValue_Id();
    }

    public partial class StatData : IData
    {
        [SerializeField]
        private int _hp;

        [JsonProperty]
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

    public enum StatKind
    {
        Hp,
        Atk,
    }
}

namespace MyGame.Heroes
{
    using ZBase.Foundation.Data;
    using UnityEngine;
    using System.Collections.Generic;

    public partial class HeroData : IData
    {
        [SerializeField]
        private IdData _id;

        [SerializeField]
        private string _name;

        [SerializeField]
        private StatData _stat;

        [SerializeField]
        private int[] _values;

        [SerializeField]
        private List<float> _floats;

        [SerializeField]
        private Dictionary<int, string> _stringMap;

        [SerializeField]
        private StatMultiplierData[] _multipliers;

        [SerializeField]
        private StatMultiplierData[] _multipliersX;

        [SerializeField]
        private List<StatMultiplierData> _abc;

        [SerializeField]
        private Dictionary<StatKind, StatMultiplierData> _statMap;
    }

    public partial class HeroDataTableAsset : DataTableAsset<IdData, HeroData>
    {
    }
}

namespace MyGame.Enemies
{
    using ZBase.Foundation.Data;
    using UnityEngine;
    using System.Collections.Generic;

    public partial class EnemyData : IData
    {
        [SerializeField]
        private IdData _id;

        [SerializeField]
        private string _name;

        [SerializeField]
        private StatData _stat;

        [SerializeField]
        private HashSet<int> _intSet;

        [SerializeField]
        private Queue<float> _floatQueue;

        [SerializeField]
        private Stack<float> _floatStack;
    }

    public abstract class EnemyDataTableAsset<T> : DataTableAsset<IdData, T> where T : IData
    {
    }

    public partial class EnemyDataTableAsset : EnemyDataTableAsset<EnemyData>
    {
    }

    public partial class NewEnemyDataTableAsset : EnemyDataTableAsset<EnemyData>
    {
    }
}

#if UNITY_EDITOR
namespace MyGame.Authoring
{
    using ZBase.Foundation.Data.Authoring;

    [Database]
    public partial class Database : UnityEngine.ScriptableObject
    {
        partial class SheetContainer
        {
        }
    }

    [Table(typeof(Heroes.HeroDataTableAsset), "Hero", NamingStrategy.SnakeCase)]
    [VerticalList(typeof(Heroes.HeroData), nameof(Heroes.HeroData.Multipliers))]
    partial class Database
    {
        partial class HeroDataTableAsset_HeroDataSheet
        {
        }
    }

    [Table(typeof(Enemies.EnemyDataTableAsset), "Enemy", NamingStrategy.SnakeCase)]
    [Table(typeof(Enemies.NewEnemyDataTableAsset), "NewEnemy", NamingStrategy.SnakeCase)]
    partial class Database
    {
        partial class EnemyDataTableAsset_EnemyDataSheet
        {
        }
    }

}
#endif
