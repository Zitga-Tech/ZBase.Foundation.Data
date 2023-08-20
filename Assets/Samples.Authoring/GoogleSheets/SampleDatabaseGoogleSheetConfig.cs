using Cathei.BakingSheet.Unity;
using UnityEngine;
using ZBase.Foundation.Data.Authoring.Configs.GoogleSheets;

namespace Samples.Authoring.GoogleSheets
{
    [CreateAssetMenu(fileName = nameof(SampleDatabaseGoogleSheetConfig), menuName = "Sample Database Google Sheet Config", order = 0)]
    public partial class SampleDatabaseGoogleSheetConfig : DatabaseGoogleSheetConfig<DatabaseDefinition.SheetContainer>
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
}
