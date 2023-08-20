using ZBase.Foundation.Data.Authoring;

namespace Samples.Authoring
{
    [Database]
    public partial class DatabaseDefinition { }


    [Table(typeof(HeroDataTableAsset), "Heroes", NamingStrategy.SnakeCase)]
    [VerticalList(typeof(HeroData), nameof(HeroData.Multipliers))]
    partial class DatabaseDefinition { }


    [Table(typeof(EnemyDataTableAsset), "Enemies", NamingStrategy.SnakeCase)]
    partial class DatabaseDefinition { }
}
