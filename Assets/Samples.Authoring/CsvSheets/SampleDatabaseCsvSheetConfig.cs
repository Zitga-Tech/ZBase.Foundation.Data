using System;
using Cathei.BakingSheet.Unity;
using UnityEngine;
using ZBase.Foundation.Data.Authoring.Configs.CsvSheets;

namespace ZBase.Foundation.Data.Samples.Authoring.Csv
{
    [Obsolete("SampleDatabaseCsvSheetConfig is deprecated. Use SampleDatabaseConfig instead.", false)]
    [CreateAssetMenu(fileName = nameof(SampleDatabaseCsvSheetConfig), menuName = "Sample Database Csv Sheet Config", order = 0)]
    public partial class SampleDatabaseCsvSheetConfig : DatabaseCsvSheetConfig<DatabaseDefinition.SheetContainer>
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
