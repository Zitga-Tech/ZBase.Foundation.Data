using Cathei.BakingSheet.Unity;
using UnityEngine;
using ZBase.Foundation.Data.Authoring;
using ZBase.Foundation.Data.Authoring.GoogleSheets;

namespace Samples.Authoring
{
    [CreateAssetMenu(fileName = "SampleDatabaseConfig", menuName = "Sample Database Config", order = 0)]
    public partial class SampleDatabaseConfig : GoogleSheetConfig<DatabaseDefinition.SheetContainer>
    {
        protected override DatabaseDefinition.SheetContainer CreateSheetContainer()
        {
            return new DatabaseDefinition.SheetContainer(UnityLogger.Default);
        }

        protected override string GetDatabaseAssetName()
        {
            return "SampleDatabaseAsset";
        }
    }

    [Database]
    public partial class DatabaseDefinition { }


    [Table(typeof(HeroDataTableAsset), "Heroes", NamingStrategy.SnakeCase)]
    [VerticalList(typeof(HeroData), nameof(HeroData.Multipliers))]
    partial class DatabaseDefinition { }


    [Table(typeof(EnemyDataTableAsset), "Enemies", NamingStrategy.SnakeCase)]
    partial class DatabaseDefinition { }
}
