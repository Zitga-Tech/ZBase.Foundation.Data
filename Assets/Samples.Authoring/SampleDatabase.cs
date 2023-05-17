using ZBase.Foundation.Data.Authoring;

namespace Samples.Authoring
{
    [Database]
    public partial class SampleDatabase
    {

    }

    [Table(typeof(HeroDataTableAsset), "Hero", NamingStrategy.SnakeCase)]
    partial class SampleDatabase { }

    [Table(typeof(EnemyDataTableAsset), "Enemy", NamingStrategy.SnakeCase)]
    partial class SampleDatabase { }
}
