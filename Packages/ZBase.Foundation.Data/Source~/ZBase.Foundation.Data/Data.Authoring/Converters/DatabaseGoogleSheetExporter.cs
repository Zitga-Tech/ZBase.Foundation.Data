using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using NReco.Csv;

namespace ZBase.Foundation.Data.Authoring
{
    public class DatabaseGoogleSheetExporter
    {
        private readonly string _gsheetAddress;
        private readonly ICredential _credential;
        private readonly IExtendedFileSystem _fileSystem;

        private Spreadsheet _spreadsheet;
        private bool _isLoaded;

        public DatabaseGoogleSheetExporter(
              string gsheetAddress
            , string credential
            , IExtendedFileSystem fileSystem = null
        )
        {
            _gsheetAddress = gsheetAddress;
            _credential = GoogleCredential
                .FromJson(credential)
                .CreateScoped(new[] { DriveService.Scope.DriveReadonly });

            _fileSystem = fileSystem ?? new DatabaseFileSystem();
        }

        public async Task Export(
              string savePath
            , bool spreadsheetAsFolder = true
            , bool cleanOutputFolder = true
        )
        {
            if (_isLoaded == false)
            {
                using (var service = new SheetsService(new BaseClientService.Initializer() {
                    HttpClientInitializer = _credential
                }))
                {
                    var sheetReq = service.Spreadsheets.Get(_gsheetAddress);
                    sheetReq.Fields = "properties,sheets(properties,data.rowData.values.formattedValue)";
                    _spreadsheet = await sheetReq.ExecuteAsync();
                }

                _isLoaded = true;
            }

            var outputPath = spreadsheetAsFolder
                ? Path.Combine(savePath, SheetUtility.ToFileName(_spreadsheet.Properties.Title))
                : savePath;

            var fileSystem = _fileSystem;
            
            if (cleanOutputFolder && fileSystem.Exists(outputPath))
            {
                fileSystem.Delete(outputPath);
            }
            
            fileSystem.CreateDirectory(outputPath);

            var sheets = _spreadsheet.Sheets;
            var sheetCount = sheets.Count;

            for (var i = 0; i < sheetCount; i++)
            {
                var gSheet = sheets[i];
                var data = gSheet.Data.FirstOrDefault();

                if (data == null)
                {
                    continue;
                }

                var fileName = SheetUtility.ToFileName(gSheet.Properties.Title, i);
                var file = Path.Combine(outputPath, $"{fileName}.csv");

                using (var stream = fileSystem.OpenWrite(file))
                using (var writer = new StreamWriter(stream))
                {
                    var rows = data.RowData;
                    var rowCount = rows?.Count ?? 0;

                    if (rowCount < 1)
                    {
                        continue;
                    }

                    var totalColCount = CountColumns(rows);

                    if (totalColCount < 1)
                    {
                        continue;
                    }

                    var csv = new CsvWriter(writer);

                    for (var r = 0; r < rowCount; r++)
                    {
                        var row = rows[r];
                        var cols = row.Values;
                        var colCount = cols?.Count ?? 0;

                        for (var c = 0; c < colCount; c++)
                        {
                            csv.WriteField(cols[c]?.FormattedValue);
                        }

                        var emptyCount = totalColCount - colCount;

                        for (var c = 0; c <= emptyCount; c++)
                        {
                            csv.WriteField(string.Empty);
                        }

                        csv.NextRecord();
                    }
                }
            }
        }

        private static int CountColumns([NotNull] IList<RowData> rows)
        {
            var result = 0;

            foreach (var row in rows)
            {
                var cols = row.Values;
                var colCount = cols?.Count ?? 0;

                if (colCount > result)
                {
                    result = colCount;
                }
            }

            return result;
        }
    }
}
