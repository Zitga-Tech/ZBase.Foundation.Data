using ZBase.Foundation.Data.Authoring;

namespace ZBase.Foundation.Data.Samples.Authoring
{
    [Database(NamingStrategy.SnakeCase, typeof(FloatWrapperConverter))]
    public partial class DatabaseDefinition
    {
        [VerticalList(typeof(HeroData), nameof(HeroData.Multipliers))]
        [Table] public HeroDataTableAsset Heroes { get; }

        [Table] public EnemyDataTableAsset Enemies { get; }

        [Table] public MapRegionDataTableAsset MapRegions { get; }
    }
}
