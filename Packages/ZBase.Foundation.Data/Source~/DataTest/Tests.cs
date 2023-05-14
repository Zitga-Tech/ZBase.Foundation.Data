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
        [SerializeField]
        private HeroId _id;

        [SerializeField]
        private string _name;

        [SerializeField]
        private int _strength;

        [SerializeField]
        private int _intelligence;

        [SerializeField]
        private int _vitality;

        [SerializeField, VerticalArray]
        private HeroStatMultiplier[] _multipliers;

        [SerializeField]
        private string[] _descriptions;
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
    public partial struct HeroDataTable : IDataTable<HeroId, HeroData>
    {
    }
}

//namespace DataSourceGenCodeDesign
//{
//#pragma warning disable

//    using System.Collections.Generic;
//    using Cathei.BakingSheet;
//    using RumbleDefense;
//    using ZBase.Foundation.Data.Authoring.SourceGen;

//    [GeneratedSheet(typeof(HeroDataTable), typeof(HeroId), typeof(HeroData))]
//    public partial class HeroDataTableSheet : Sheet<HeroId, HeroDataTableSheet.HeroDataSheetRow>
//    {
//        [GeneratedSheetRow(typeof(HeroId), typeof(HeroData))]
//        public partial class HeroDataSheetRow : SheetRow<HeroId>
//        {
//            public string Name { get; set; }

//            public int Strength { get; set; }

//            public int Intelligence { get; set; }

//            public int Vitality { get; set; }

//            public VerticalList<HeroStatMultiplierListElement> Multipliers { get; set; }

//            public List<string> Descriptions { get; set; }
//        }

//        [GeneratedListElement(typeof(HeroStatMultiplier))]
//        public partial class HeroStatMultiplierListElement
//        {
//            public float StatMultiplier { get; set; }

//            public int RequiredExp { get; set; }

//            public string RequiredItem { get; set; }
//        }

//        private static HeroStatMultiplier[] ToArray(VerticalList<HeroStatMultiplierListElement> list)
//        {
//            var count = list.Count;
//            var array = new HeroStatMultiplier[count];

//            for (var i = 0; i < count; i++)
//            {
//                var element = list[i];
//                ref var item = ref array[i];
//                item = new HeroStatMultiplier();

//                item.SetValues(
//                    element.StatMultiplier
//                    , element.RequiredExp
//                    , element.RequiredItem
//                );
//            }

//            return array;
//        }

//        public HeroDataTable ToHeroDataTable()
//        {
//            var sheetRows = this.Items;
//            var count = sheetRows.Count;

//            var dataTable = new HeroDataTable();
//            var rows = new HeroData[count];
            
//            for (var i = 0; i < count; i++)
//            {
//                var sheetRow = sheetRows[i];
//                ref var row = ref rows[i];
//                row = new HeroData();

//                row.SetValues(
//                      sheetRow.Id
//                    , sheetRow.Name
//                    , sheetRow.Strength
//                    , sheetRow.Intelligence
//                    , sheetRow.Vitality
//                    , ToArray(sheetRow.Multipliers)
//                    , sheetRow.Descriptions.ToArray()
//                );
//            }

//            dataTable.SetRows(rows);
//            return dataTable;
//        }
//    }

//#pragma warning enable
//}

//namespace RumbleDefense
//{
//    using System.Runtime.CompilerServices;
//    using UnityEngine;

//    partial struct HeroData
//    {
//        public HeroId Id
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => this._id;
//        }

//        public string Name
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => this._name;
//        }

//        public int Strength
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => this._strength;
//        }

//        public int Intelligence
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => this._intelligence;
//        }

//        public int Vitality
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => this._vitality;
//        }

//        public ReadOnlyMemory<HeroStatMultiplier> Multipliers
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => this._multipliers;
//        }

//        public ReadOnlyMemory<string> Descriptions
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => this._descriptions;
//        }

//#if UNITY_EDITOR
//        [Obsolete("This method is not intended to be used directly by user code.")]
//        internal void SetValues(
//              HeroId _id
//            , string _name
//            , int _strength
//            , int _intelligence
//            , int _vitality
//            , HeroStatMultiplier[] _multipliers
//            , string[] _descriptions
//        )
//        {
//            this._id = _id;
//            this._name = _name;
//            this._strength = _strength;
//            this._intelligence = _intelligence;
//            this._vitality = _vitality;
//            this._multipliers = _multipliers;
//            this._descriptions = _descriptions;
//        }
//#endif
//    }

//    partial struct HeroStatMultiplier
//    {
//        public float StatMultiplier
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => this._statMultiplier;
//        }

//        public int RequiredExp
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => this._requiredExp;
//        }

//        public string RequiredItem
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => this._requiredItem;
//        }

//#if UNITY_EDITOR
//        [Obsolete("This method is not intended to be used directly by user code.")]
//        internal void SetValues(
//              float _statMultiplier
//            , int _requiredExp
//            , string _requiredItem
//        )
//        {
//            this._statMultiplier = _statMultiplier;
//            this._requiredExp = _requiredExp;
//            this._requiredItem = _requiredItem;
//        }
//#endif
//    }

//    partial struct HeroDataTable
//    {
//        [SerializeField]
//        private HeroData[] _rows;

//        public ReadOnlyMemory<HeroData> Rows
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => this._rows;
//        }

//#if UNITY_EDITOR
//        [Obsolete("This method is not intended to be used directly by user code.")]
//        internal void SetRows(
//              HeroData[] _rows
//        )
//        {
//            this._rows = _rows;
//        }
//#endif
//    }
//}