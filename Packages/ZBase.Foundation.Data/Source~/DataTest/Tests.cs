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
    using System;
    using Newtonsoft.Json;
    using UnityEngine;
    using ZBase.Foundation.Data;

    public enum EntityKind
    {
        Hero,
        Enemy,
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class FieldAttribute : Attribute { }

    public partial class IdData : IData
    {
        [DataProperty]
        [field: Field]
        public EntityKind Kind => Get_Kind();

        [DataProperty]
        public int Id => Get_Id();
    }

    public partial class StatData : IData
    {
        [SerializeField]
        private int _hp;

        [JsonProperty]
        private int _atk;
    }

    public partial class GenericData<T> : IData
    {
        [DataProperty]
        public int Id => Get_Id();
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
    using System;

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

        [DataProperty]
        public ReadOnlyMemory<StatMultiplierData> Multipliers => Get_Multipliers();

        [DataProperty]
        public ReadOnlyMemory<StatMultiplierData> MultipliersX => Get_MultipliersX();

        [SerializeField]
        private List<StatMultiplierData> _abc;

        [SerializeField]
        private Dictionary<StatKind, StatMultiplierData> _statMap;
    }

    public partial class HeroDataTableAsset : DataTableAsset<IdData, HeroData>
    {
    }

    public partial class NewHeroData : HeroData
    {
        [DataProperty]
        [field: SerializeField]
        public ReadOnlyMemory<int> NewValues => Get_NewValues();
    }

    public partial class NewHeroDataTableAsset : DataTableAsset<IdData, NewHeroData>
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

    public abstract class GenericDataTableAsset<T> : DataTableAsset<int, GenericData<T>>
    {
    }

    public partial class GenericDataTableAsset : GenericDataTableAsset<int> { }
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

    [Table(typeof(Heroes.NewHeroDataTableAsset), "NewHero", NamingStrategy.SnakeCase)]
    partial class Database
    {

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
