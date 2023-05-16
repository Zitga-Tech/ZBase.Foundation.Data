﻿// BakingSheet, Maxwell Keonwoo Kang <code.athei@gmail.com>, 2022

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cathei.BakingSheet;
using Cathei.BakingSheet.Internal;
using Cathei.BakingSheet.Raw;
using Microsoft.Extensions.Logging;

namespace ZBase.Foundation.Data.Authoring
{
    /// <summary>
    /// Generic sheet importer for cell-based Spreadsheet sources.
    /// </summary>
    public abstract class DatabaseRawSheetImporter : ISheetImporter, ISheetFormatter
    {
        private bool _isLoaded;

        public DatabaseRawSheetImporter(
              TimeZoneInfo timeZoneInfo
            , IFormatProvider formatProvider
            , int emptyRowStreakThreshold
        )
        {
            TimeZoneInfo = timeZoneInfo ?? TimeZoneInfo.Utc;
            FormatProvider = formatProvider ?? CultureInfo.InvariantCulture;
            EmptyRowStreakThreshold = emptyRowStreakThreshold;
        }

        protected abstract Task<bool> LoadData();

        protected abstract IEnumerable<IRawSheetImporterPage> GetPages(string sheetName);

        public TimeZoneInfo TimeZoneInfo { get; }

        public IFormatProvider FormatProvider { get; }

        public int EmptyRowStreakThreshold { get; }

        public virtual void Reset()
        {
            _isLoaded = false;
        }

        public async Task<bool> Import(SheetConvertingContext context)
        {
            if (_isLoaded == false)
            {
                var success = await LoadData();

                if (success == false)
                {
                    context.Logger.LogError("Failed to load data");
                    return false;
                }

                _isLoaded = true;
            }

            foreach (var pair in context.Container.GetSheetProperties())
            {
                string sheetName;
                NamingStrategy namingStrategy;

                var attribute = pair.Value.PropertyType.GetCustomAttribute<DataSheetNamingAttribute>();
                if (attribute != null)
                {
                    sheetName = attribute.SheetName;
                    namingStrategy = attribute.NamingStrategy;
                }
                else
                {
                    sheetName = pair.Key;
                    namingStrategy = NamingStrategy.PascalCase;
                }

                using (context.Logger.BeginScope(pair.Key))
                {
                    if (pair.Value.GetValue(context.Container) is not ISheet sheet)
                    {
                        // create new sheet
                        sheet = Activator.CreateInstance(pair.Value.PropertyType) as ISheet;
                        pair.Value.SetValue(context.Container, sheet);
                    }

                    if (sheet == null)
                    {
                        context.Logger.LogError("Failed to create sheet of type {SheetType}", pair.Value.PropertyType);
                        continue;
                    }

                    var namingMap = BuildNamingMap(sheet, sheetName, namingStrategy);
                    var pages = GetPages(namingMap.GetSerializedName(sheetName));

                    foreach (var page in pages.OrderBy(x => x.SubName))
                    {
                        ImportPage(page, context, sheet, namingMap);
                    }
                }
            }

            return true;
        }

        private void ImportPage(
              IRawSheetImporterPage page
            , SheetConvertingContext context
            , ISheet sheet
            , NamingMap namingMap
        )
        {
            var idColumnName = page.GetCell(0, 0);

            if (namingMap.Validate(nameof(ISheetRow.Id), idColumnName) == false)
            {
                context.Logger.LogError(
                      "First column \"{ColumnName}\" must be named \"{CorrectColumnName}\"."
                    , idColumnName
                    , namingMap.GetSerializedName(nameof(ISheetRow.Id))
                );
                return;
            }

            var columnNames = new List<string>();
            var headerRows = new List<string> {
                // first row is always header row
                null
            };

            var headerRow = 1;

            // if the column next to the id column is empty,
            // it means the id column is split
            // so the next row would be a part of header row too
            if (page.IsEmptyCell(1, 0))
            {
                headerRows.Add(null);
                headerRow = 2;
            }

            // if id column is empty they are split header row
            for (; page.IsEmptyCell(0, headerRow) && page.IsEmptyRow(headerRow) == false; headerRow++)
            {
                headerRows.Add(null);
            }

            for (int pageColumn = 0; ; ++pageColumn)
            {
                int lastValidRow = -1;

                for (int pageRow = 0; pageRow < headerRows.Count; ++pageRow)
                {
                    if (page.IsEmptyCell(pageColumn, pageRow) == false)
                    {
                        lastValidRow = pageRow;
                        var serializedName = page.GetCell(pageColumn, pageRow);
                        headerRows[pageRow] = namingMap.GetProperName(serializedName);
                    }
                }

                if (lastValidRow == -1)
                {
                    break;
                }

                columnNames.Add(string.Join(Config.IndexDelimiter, headerRows.Take(lastValidRow + 1)));
            }

            PropertyMap propertyMap = sheet.GetPropertyMap(context);

            ISheetRow sheetRow = null;
            string rowId = null;
            var vindex = 0;
            var emptyRowStreak = 0;

            for (int pageRow = headerRows.Count; emptyRowStreak <= EmptyRowStreakThreshold; ++pageRow)
            {
                string idCellValue = page.GetCell(0, pageRow);

                if (string.IsNullOrWhiteSpace(idCellValue) == false)
                {
                    if (idCellValue.StartsWith(Config.Comment))
                    {
                        continue;
                    }

                    rowId = idCellValue;
                    sheetRow = Activator.CreateInstance(sheet.RowType) as ISheetRow;
                    vindex = 0;
                }
                else if (page.IsEmptyRow(pageRow))
                {
                    emptyRowStreak++;
                    continue;
                }

                if (sheetRow == null)
                {
                    // skipping this row
                    continue;
                }

                using (context.Logger.BeginScope(rowId))
                {
                    try
                    {
                        ImportRow(page, context, sheetRow, propertyMap, columnNames, vindex, pageRow);
                    }
                    catch
                    {
                        // failed to convert, skip this row
                        sheetRow = null;
                        continue;
                    }

                    if (vindex == 0)
                    {
                        if (sheet.Contains(sheetRow.Id))
                        {
                            context.Logger.LogError("Already has row with id \"{RowId}\"", sheetRow.Id);
                        }
                        else
                        {
                            sheet.Add(sheetRow);
                        }
                    }

                    vindex++;
                }
            }
        }

        private void ImportRow(
              IRawSheetImporterPage page
            , SheetConvertingContext context
            , ISheetRow sheetRow
            , PropertyMap propertyMap
            , List<string> columnNames
            , int vindex
            , int pageRow
        )
        {
            for (int pageColumn = 0; pageColumn < columnNames.Count; ++pageColumn)
            {
                string columnValue = columnNames[pageColumn];

                if (columnValue.StartsWith(Config.Comment))
                {
                    continue;
                }

                using (context.Logger.BeginScope(columnValue))
                {
                    string cellValue = page.GetCell(pageColumn, pageRow);

                    // if cell is empty, value should not be set
                    // Property will keep it's default value
                    if (string.IsNullOrEmpty(cellValue))
                    {
                        continue;
                    }

                    try
                    {
                        propertyMap.SetValue(sheetRow, vindex, columnValue, cellValue, this);
                    }
                    catch (Exception ex)
                    {
                        // for Id column, throw and exclude whole column
                        if (pageColumn == 0)
                        {
                            context.Logger.LogError(ex, "Failed to set id \"{CellValue}\"", cellValue);
                            throw;
                        }

                        context.Logger.LogError(ex, "Failed to set value \"{CellValue}\"", cellValue);
                    }
                }
            }
        }

        private static NamingMap BuildNamingMap(ISheet sheet, string sheetName, NamingStrategy namingStrategy)
        {
            var map = new NamingMap(namingStrategy);
            var sheetType = sheet.GetType();
            var uniqueTypes = new HashSet<Type>(sheetType.GetNestedTypes());
            var typeQueue = new Queue<Type>(uniqueTypes);

            map.AddProperName(sheetType.Name);
            map.AddProperName(sheetName);

            while (typeQueue.TryDequeue(out var type))
            {
                foreach (var property in type.GetProperties())
                {
                    map.AddProperName(property.Name);

                    var propertyType = property.PropertyType;

                    if (uniqueTypes.Contains(propertyType)
                        || propertyType.IsPrimitive
                        || propertyType.IsEnum
                        || propertyType == typeof(decimal)
                        || propertyType == typeof(string)
                        || propertyType == typeof(object)
                    )
                    {
                        continue;
                    }

                    uniqueTypes.Add(propertyType);
                    typeQueue.Enqueue(propertyType);
                }
            }

            return map;
        }
    }
}
