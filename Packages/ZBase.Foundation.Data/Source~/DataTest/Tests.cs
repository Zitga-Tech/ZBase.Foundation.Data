﻿namespace DataTest
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
    using System.Runtime.InteropServices;
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

    public interface IConvert<in TFrom, out TTo>
    {
        TTo Convert(TFrom value);
    }

    [Serializable]
    public struct IntWrapper : IConvert<int, IntWrapper>
    {
        public int value;

        public IntWrapper(int value)
        {
            this.value = value;
        }

        public readonly IntWrapper Convert(int value) => new(value);
    }

    [Serializable]
    public struct FloatWrapper : IConvert<float, FloatWrapper>
    {
        public float value;

        public FloatWrapper(float value)
        {
            this.value = value;
        }

        public readonly FloatWrapper Convert(float value) => new(value);
    }

    public struct IntWrapperConverter
    {
        public static IntWrapper Convert(int value) => new(value);
    }

    public struct FloatWrapperConverter
    {
        public readonly FloatWrapper Convert(float value) => new(value);
    }

    public struct WrapperConverter<TFrom, TTo> where TTo : struct, IConvert<TFrom, TTo>
    {
        public readonly TTo Convert(TFrom value) => default(TTo).Convert(value);
    }

    public partial class StatData : IData
    {
        [DataProperty, DataConverter(typeof(WrapperConverter<float, FloatWrapper>))]
        public FloatWrapper Hp => Get_Hp();

        [JsonProperty, DataConverter(typeof(FloatWrapperConverter))]
        private FloatWrapper _atk;
    }

    public partial class GenericData<T> : IData
    {
        [DataProperty]
        public int Id { get => Get_Id(); init => Set_Id(value); }

        public int X { get; init; }

        public bool Equals(GenericData<T> other)
        {
            return false;
        }
    }

    [StructLayout(LayoutKind.Auto)]
    public partial struct StatMultiplierData : IData
    {
        [SerializeField]
        private IntWrapper _level;

        [SerializeField]
        private FloatWrapper _hp;

        [DataProperty]
        public readonly float Atk => Get_Atk();
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

    [DataMutable(true)]
    public partial class MutableData : IData
    {
        [SerializeField]
        private int _intValue;

        [SerializeField]
        private int[] _arrayValue;

        [DataProperty]
        public ReadOnlyMemory<float> Multipliers => Get_Multipliers();
    }

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

    public partial class HeroDataTableAsset : DataTableAsset<IdData, HeroData>, IDataTableAsset
    {
    }

    public partial class NewHeroData : HeroData, IData
    {
        [DataProperty]
        [field: SerializeField]
        public ReadOnlyMemory<int> NewValues => Get_NewValues();
    }

    public partial class NewHeroDataTableAsset : DataTableAsset<IdData, NewHeroData>, IDataTableAsset
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

    public abstract class EnemyDataTableAsset<T> : DataTableAsset<IdData, T>, IDataTableAsset where T : IDataWithId<IdData>
    {
    }

    public partial class EnemyDataTableAsset : EnemyDataTableAsset<EnemyData>, IDataTableAsset
    {
    }

    public partial class NewEnemyDataTableAsset : EnemyDataTableAsset<EnemyData>, IDataTableAsset
    {
    }

    public abstract class GenericDataTableAsset<T> : DataTableAsset<int, GenericData<T>>, IDataTableAsset
    {
    }

    public partial class GenericDataTableAsset : GenericDataTableAsset<int>, IDataTableAsset { }
}

#if UNITY_EDITOR
namespace MyGame.Authoring
{
    using ZBase.Foundation.Data.Authoring;
    using MyGame.Heroes;
    using MyGame.Enemies;

    [Database(NamingStrategy.SnakeCase, typeof(IntWrapperConverter))]
    public partial class Database : UnityEngine.ScriptableObject
    {
        partial class SheetContainer
        {
        }
    }

    partial class Database
    {
        [VerticalList(typeof(HeroData), nameof(HeroData.Multipliers))]
        [Table] public HeroDataTableAsset Hero { get; }

        partial class HeroDataTableAsset_HeroDataSheet
        {
        }
    }

    partial class Database
    {
        [VerticalList(typeof(NewHeroData), nameof(NewHeroData.Multipliers))]
        [Table] public NewHeroDataTableAsset NewHero { get; }
    }

    partial class Database
    {
        [Table] public EnemyDataTableAsset Enemy { get; }

        [Table] public NewEnemyDataTableAsset NewEnemy { get; }

        partial class EnemyDataTableAsset_EnemyDataSheet
        {
        }
    }

}
#endif
