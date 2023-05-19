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

    public partial struct HeroData : IData
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
    [global::ZBase.Foundation.Data.Authoring.Database]
    [global::UnityEngine.CreateAssetMenu(fileName = "SampleDatabase", menuName = "Sample Database", order = 0)]
    public partial class Database : global::UnityEngine.ScriptableObject
    {
        partial class SheetContainer
        {
        }
    }


    [global::ZBase.Foundation.Data.Authoring.Table(typeof(Heroes.HeroDataTableAsset), "Hero", global::ZBase.Foundation.Data.Authoring.NamingStrategy.SnakeCase)]
    [global::ZBase.Foundation.Data.Authoring.VerticalList(typeof(Heroes.HeroData), nameof(Heroes.HeroData.Multipliers), typeof(Heroes.HeroDataTableAsset))]
    partial class Database
    {
        partial class HeroDataSheet
        {
            partial class __HeroData
            {
                public void FillDataX(Heroes.HeroData data)
                {
                    this.Id.FillDataX(data.Id);
                    this.Name = data.Name;
                    this.Stat.FillDataX(data.Stat);
                    this.Values.AddRange(data.Values.Span.ToArray());
                    this.Floats.AddRange(data.Floats);
                    
                    foreach (var kv in data.StringMap)
                    {
                        this.StringMap[kv.Key] = kv.Value;
                    }

                    foreach (var item in data.Multipliers.ToArray())
                    {
                        var elem = new __StatMultiplierData();
                        elem.FillDataX(item);
                        this.Multipliers.Add(elem);
                    }

                    foreach (var item in data.Abc)
                    {
                        var elem = new __StatMultiplierData();
                        elem.FillDataX(item);
                        this.Abc.Add(elem);
                    }

                    foreach (var kv in data.StatMap)
                    {
                        var key = kv.Key;
                        var elem = new __StatMultiplierData();
                        elem.FillDataX(kv.Value);

                        this.StatMap[key] = elem;
                    }
                }
            }

            partial class __IdData
            {
                public void FillDataX(IdData data)
                {
                    this.Kind = data.Kind;
                    this.Id = data.Id;
                }
            }

            partial class __StatData
            {
                public void FillDataX(StatData data)
                {
                    this.Hp = data.Hp;
                    this.Atk = data.Atk;
                }
            }

            partial class __StatMultiplierData
            {
                public void FillDataX(StatMultiplierData data)
                {
                    this.Level = data.Level;
                    this.Hp = data.Hp;
                    this.Atk = data.Atk;
                }
            }
        }
    }


    [global::ZBase.Foundation.Data.Authoring.Table(typeof(Enemies.EnemyDataTableAsset), "Enemy", global::ZBase.Foundation.Data.Authoring.NamingStrategy.SnakeCase)]
    partial class Database
    {
        partial class EnemyDataSheet
        {
            public void FillDataX(Enemies.EnemyDataTableAsset mEnemyDataTableAsset)
            {
                if (mEnemyDataTableAsset == false) return;

                foreach (var row in mEnemyDataTableAsset.Rows.Span)
                {

                }
            }
        }
    }

}
#endif
