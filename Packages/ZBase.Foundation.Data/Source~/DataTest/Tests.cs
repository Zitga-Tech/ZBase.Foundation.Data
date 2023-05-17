using ZBase.Foundation.Data.Authoring;

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
    public partial class HeroDataTableAsset : DataTableAsset<EntityId, HeroData>
    {
    }
}

namespace RumbleDefense.Authoring
{
    [Database]
    [Table(typeof(HeroDataTableAsset))]
    public partial class Database
    {

    }
}
