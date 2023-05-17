using UnityEngine;
using ZBase.Foundation.Data.Authoring;

namespace Samples.Authoring
{
    [CreateAssetMenu(fileName = "SampleDatabase", menuName = "Sample Database", order = 0)]
    [Database]
    public partial class SampleDatabase : ScriptableObject
    {

    }

    [Table(typeof(HeroDataTableAsset), "Hero", NamingStrategy.SnakeCase)]
    partial class SampleDatabase { }

    [Table(typeof(EnemyDataTableAsset), "Enemy", NamingStrategy.SnakeCase)]
    partial class SampleDatabase { }
}
