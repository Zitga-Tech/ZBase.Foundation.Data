using System;

namespace DataTest
{
    public class Program
    {
        public static void Main()
        {
        }
    }
}

namespace RumbleDefense
{
    using ZBase.Foundation.Data;
    using UnityEngine;

    public enum HeroId
    {
        ID0001,
        ID0002,
        ID0003,
        ID0004,
        ID0005,
    }

    [RuntimeImmutable]
    public partial struct HeroData : IData
    {
        [SerializeField, DataId]
        private HeroId _id;

        [SerializeField]
        private string _name;

        [SerializeField]
        private int _strength;

        [SerializeField]
        private int _intelligence;

        [SerializeField]
        private int _vitality;

        [SerializeField]
        private HeroStatMultiplier[] _multipliers;
    }

    [RuntimeImmutable]
    public partial struct HeroStatMultiplier : IData
    {
        [SerializeField]
        private float _statMultiplier;

        [SerializeField]
        private int _requiredExp;

        [SerializeField]
        private string _requiredItem;
    }

    [RuntimeImmutable]
    public partial struct HeroDataTable : IDataTable<HeroData>
    {
    }
}

namespace RumbleDefense
{
    using System.Runtime.CompilerServices;
    using UnityEngine;

    partial struct HeroData
    {
        public HeroId Id
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this._id;
        }

        public string Name
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this._name;
        }

        public int Strength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this._strength;
        }

        public int Intelligence
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this._intelligence;
        }

        public int Vitality
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this._vitality;
        }

        public ReadOnlyMemory<HeroStatMultiplier> Multipliers
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this._multipliers;
        }

#if UNITY_EDITOR
        [Obsolete("This method is not intended to be used directly by user code.")]
        internal void SetValues(
              HeroId _id
            , string _name
            , int _strength
            , int _intelligence
            , int _vitality
            , HeroStatMultiplier[] _multipliers
        )
        {
            this._id = _id;
            this._name = _name;
            this._strength = _strength;
            this._intelligence = _intelligence;
            this._vitality = _vitality;
            this._multipliers = _multipliers;
        }
#endif
    }

    partial struct HeroStatMultiplier
    {
        public float StatMultiplier
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this._statMultiplier;
        }

        public int RequiredExp
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this._requiredExp;
        }

        public string RequiredItem
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this._requiredItem;
        }

#if UNITY_EDITOR
        [Obsolete("This method is not intended to be used directly by user code.")]
        internal void SetValues(
              float _statMultiplier
            , int _requiredExp
            , string _requiredItem
        )
        {
            this._statMultiplier = _statMultiplier;
            this._requiredExp = _requiredExp;
            this._requiredItem = _requiredItem;
        }
#endif
    }

    partial struct HeroDataTable
    {
        [SerializeField]
        private HeroData[] _rows;

        public ReadOnlyMemory<HeroData> Rows
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this._rows;
        }

#if UNITY_EDITOR
        [Obsolete("This method is not intended to be used directly by user code.")]
        internal void SetValues(
              HeroData[] _rows
        )
        {
            this._rows = _rows;
        }
#endif
    }
}

namespace DataSourceGenCodeDesign
{
    using Cathei.BakingSheet;
    using RumbleDefense;
    using ZBase.Foundation.Data.Authoring.SourceGen;

    [GeneratedSheet(typeof(HeroId), typeof(HeroDataTable))]
    public partial class HeroDataTableSheet : Sheet<HeroId, HeroDataTableSheet.HeroDataRowArray>
    {
        public HeroDataTable ToHeroDataTable()
        {
            var dataTable = new HeroDataTable();
            
            return dataTable;
        }

        [GeneratedSheetRowArray(typeof(HeroId), typeof(HeroData), typeof(HeroStatMultiplier))]
        public partial class HeroDataRowArray : SheetRowArray<HeroId, HeroStatMultiplierElem>
        {
            public string Name { get; set; }

            public int Strength { get; set; }

            public int Intelligence { get; set; }

            public int Vitality { get; set; }
        }

        [GeneratedSheetRowElem(typeof(HeroStatMultiplier))]
        public partial class HeroStatMultiplierElem : SheetRowElem
        {
            public float StatMultiplier { get; set; }

            public int RequiredExp { get; set; }

            public string RequiredItem { get; set; }
        }
    }
}