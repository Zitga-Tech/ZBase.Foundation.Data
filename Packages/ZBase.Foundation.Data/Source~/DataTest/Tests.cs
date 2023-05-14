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
    public partial class HeraDataTableAsset : DataTableAsset<HeroId, HeroData>
    {
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected override HeroId GetId(HeroData row)
            => row.Id;
    }
}

namespace DataSourceGenCodeDesign
{
#pragma warning disable

    using System.Collections.Generic;
    using Cathei.BakingSheet;
    using RumbleDefense;
    using ZBase.Foundation.Data.Authoring.SourceGen;

    [GeneratedSheet(typeof(HeroId), typeof(HeroData), typeof(HeraDataTableAsset))]
    public partial class HeroDataTableSheet : Sheet<HeroId, HeroDataTableSheet.HeroDataSheetRow>
    {
        [GeneratedSheetRow(typeof(HeroId), typeof(HeroData))]
        public partial class HeroDataSheetRow : SheetRow<HeroId>
        {
            public string Name { get; set; }

            public int Strength { get; set; }

            public int Intelligence { get; set; }

            public int Vitality { get; set; }

            public VerticalList<HeroStatMultiplierListElement> Multipliers { get; set; }

            public List<string> Descriptions { get; set; }
        }

        [GeneratedListElement(typeof(HeroStatMultiplier))]
        public partial class HeroStatMultiplierListElement
        {
            public float StatMultiplier { get; set; }

            public int RequiredExp { get; set; }

            public string RequiredItem { get; set; }
        }

        private static HeroStatMultiplier[] ToArray(VerticalList<HeroStatMultiplierListElement> list)
        {
            var count = list.Count;
            var array = new HeroStatMultiplier[count];

            for (var i = 0; i < count; i++)
            {
                var element = list[i];
                ref var item = ref array[i];
                item = new HeroStatMultiplier();

                item.SetValues(
                    element.StatMultiplier
                    , element.RequiredExp
                    , element.RequiredItem
                );
            }

            return array;
        }

        public HeroData[] ToHeroDataRows()
        {
            var sheetRows = this.Items;
            var count = sheetRows.Count;
            var rows = new HeroData[count];

            for (var i = 0; i < count; i++)
            {
                var sheetRow = sheetRows[i];
                ref var row = ref rows[i];
                row = new HeroData();

                row.SetValues(
                      sheetRow.Id
                    , sheetRow.Name
                    , sheetRow.Strength
                    , sheetRow.Intelligence
                    , sheetRow.Vitality
                    , ToArray(sheetRow.Multipliers)
                    , sheetRow.Descriptions.ToArray()
                );
            }

            return rows;
        }
    }

#pragma warning enable
}
