using System;
using Newtonsoft.Json.Serialization;

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

    public enum EntityKind
    {
        Hero,
        Enemy,
    }

    public partial struct EntityId : IData
    {
        [SerializeField]
        private EntityKind _kind;

        [SerializeField]
        private int _id;
    }

    public partial struct HeroData : IData
    {
        [SerializeField]
        private EntityId _id;

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

    public partial struct HeroStatMultiplier : IData
    {
        [SerializeField]
        private float _statMultiplier;

        [SerializeField]
        private int _requiredExp;

        [SerializeField]
        private string _requiredItem;
    }

    [DataSheetNaming("Hero", NamingStrategy.SnakeCase)]
    public partial class HeraDataTableAsset : DataTableAsset<EntityId, HeroData>
    {
    }
}

namespace RumbleDefense.Authoring
{
#pragma warning disable

    using System.Collections.Generic;
    using Cathei.BakingSheet;
    using Microsoft.Extensions.Logging;
    using RumbleDefense;
    using ZBase.Foundation.Data;
    using ZBase.Foundation.Data.Authoring.SourceGen;

    [GeneratedSheetContainer]
    public partial class DataSheetContainer : SheetContainerBase
    {
        protected DataSheetContainer(ILogger logger) : base(logger) { }

        public HeroDataSheet HeroDataSheet { get; set; }
    }

    [DataSheetNaming("Hero", NamingStrategy.SnakeCase)]
    [GeneratedSheet(typeof(EntityId), typeof(HeroData), typeof(HeraDataTableAsset))]
    public partial class HeroDataSheet : Sheet<HeroDataSheet.__EntityId, HeroDataSheet.__HeroData>
    {
        [GeneratedSheetRow(typeof(EntityId), typeof(HeroData))]
        public partial class __HeroData : SheetRow<__EntityId>
        {
            public string Name { get; set; }

            public int Strength { get; set; }

            public int Intelligence { get; set; }

            public int Vitality { get; set; }

            public VerticalList<__HeroStatMultiplier> Multipliers { get; set; }

            public List<string> Descriptions { get; set; }

            public HeroData ToHeroData()
            {
                var result = new HeroData();

                result.SetValues(
                      this.Id.ToEntityId()
                    , this.Name
                    , this.Strength
                    , this.Intelligence
                    , this.Vitality
                    , ToHeroStatMultiplierArray()
                    , this.Descriptions.ToArray()
                );

                return result;
            }

            private HeroStatMultiplier[] ToHeroStatMultiplierArray()
            {
                var rows = this.Multipliers;
                var count = rows.Count;
                var result = new HeroStatMultiplier[count];

                for (var i = 0; i < count; i++)
                {
                    result[i] = rows[i].ToHeroStatMultiplier();
                }

                return result;
            }
        }

        [GeneratedDataRow(typeof(EntityId))]
        public partial class __EntityId
        {
            private EntityKind Kind { get; set; }

            private int Id { get; set; }

            public EntityId ToEntityId()
            {
                var result = new EntityId();

                result.SetValues(
                      this.Kind
                    , this.Id
                );

                return result;
            }
        }

        [GeneratedDataRow(typeof(HeroStatMultiplier))]
        public partial class __HeroStatMultiplier
        {
            public float StatMultiplier { get; set; }

            public int RequiredExp { get; set; }

            public string RequiredItem { get; set; }

            public HeroStatMultiplier ToHeroStatMultiplier()
            {
                var result = new HeroStatMultiplier();

                result.SetValues(
                      this.StatMultiplier
                    , this.RequiredExp
                    , this.RequiredItem
                );

                return result;
            }
        }

        public HeroData[] ToHeroDataArray()
        {
            var rows = this.Items;
            var count = rows.Count;
            var array = new HeroData[count];

            for (var i = 0; i < count; i++)
            {
                array[i] = rows[i].ToHeroData();
            }

            return array;
        }
    }

#pragma warning enable
}
