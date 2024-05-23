using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace FileWorker
{
    public class ExcelFileWorker : IFileWorker
    {
        private List<ExcelDocumentData> _sheets = null;
        private ExcelPackage _package = null;
        private int _currentRow;
        private string _filePath;

        public ExcelFileWorker(string filePath, List<ExcelDocumentData> sheets)
        {
            _sheets = sheets;
            _filePath = filePath;
            _currentRow = 1;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        private void Init(string filePath = null)
        {
            if (filePath == null)
                _package = new ExcelPackage();
            else
            {
                if (!File.Exists( filePath ))
                    throw new Exception($"Не найден файл по заданному пути: {filePath}!");

                var fileInfo = new FileInfo(filePath);
                _package = new ExcelPackage( fileInfo );
            }
        }

        public void SaveFile()
        {
            FileStream stream = File.Create( _filePath );

            stream.Close();

            File.WriteAllBytes( _filePath, _package.GetAsByteArray() );
        }

        public void SetCurrentRowToStartPosition()
        {
            _currentRow = 1;
        }

        public void ExportRow(string text, string sheetName = null)
        {
            if (_package == null)
                Init();

            ExcelWorksheet workSheet = null;

            if (sheetName != null)
            {
                workSheet = _package?.Workbook?.Worksheets?.FirstOrDefault( w => w.Name == sheetName );

                if(workSheet == null)
                    workSheet = _package.Workbook.Worksheets.Add( sheetName );
            }
            else
            {
                workSheet = _package.Workbook.Worksheets.FirstOrDefault();
            }

            if (workSheet == null)
                throw new Exception( "Не задана страница для экспорта." );


            workSheet.Cells[_currentRow, 1].Value = text;

            _currentRow++;
        }

        public void ExportData()
        {
            if (_sheets == null)
                throw new Exception( "Не добавлены листы и объекты для экспортирования!" );

            if(_package == null)
                Init();

            foreach (var sheet in _sheets)
            {
                if(!(_package?.Workbook?.Worksheets?.Any(w => w.Name == sheet.SheetName) ?? false))
                    _package.Workbook.Worksheets.Add( sheet.SheetName );
            }

            foreach (var sheet in _sheets)
            {
                if (sheet == null)
                    continue;

                var workSheet = _package.Workbook.Worksheets[sheet.SheetName];

                var data = sheet.Data;

                if (data == null)
                    throw new Exception($"Не инициализированы данные для страницы {sheet.SheetName}");

                if(sheet.HeadHorizontalAlignment != null)
                    workSheet.Row( _currentRow ).Style.HorizontalAlignment = (ExcelHorizontalAlignment)sheet.HeadHorizontalAlignment;

                if (sheet.HeadVerticalAlignment != null)
                    workSheet.Row( _currentRow ).Style.VerticalAlignment = (ExcelVerticalAlignment)sheet.HeadVerticalAlignment;

                workSheet.DefaultRowHeight = sheet.DefaultRowHeight;
                workSheet.DefaultColWidth = sheet.DefaultRowWidth;

                var columns = sheet.GetColumns();

                int currentColumn = 1;

                foreach (var col in columns)
                {
                    col.ColumnNumber = currentColumn;
                    currentColumn++;
                }

                if (sheet.ExportColumnNames)
                {
                    if (sheet.HeadRowHeight == 0)
                        sheet.HeadRowHeight = sheet.DefaultRowHeight;

                    workSheet.Row( _currentRow ).Height = sheet.HeadRowHeight;

                    foreach (var col in columns)
                    {
                        workSheet.Cells[_currentRow, col.ColumnNumber].Value = col.ColumnName;
                    }

                    if (sheet.HeadRowAutoFilter)
                    {
                        var minColumnNumber = columns.Min(c => c.ColumnNumber);
                        var maxColumnNumber = columns.Max(c => c.ColumnNumber);
                        workSheet.Cells[_currentRow, minColumnNumber, _currentRow, maxColumnNumber].AutoFilter = true;
                    }

                    _currentRow++;
                }

                foreach (var row in data)
                {
                    workSheet.Row( _currentRow ).Height = sheet.DefaultRowHeight;

                    foreach(var col in columns)
                    {
                        var value = sheet.GetPropertyValueByName( row, col.PropertyName, col.Type );
                        workSheet.Cells[_currentRow, col.ColumnNumber].Value = value;

                        if (col.Type == ExcelType.DateTime)
                            workSheet.Cells[_currentRow, col.ColumnNumber].Style.Numberformat.Format = "DD.MM.YY";
                    }

                    _currentRow++;
                }
            }
        }

        public void ImportData<T>() where T:class, new()
        {
            if (_filePath == null)
                throw new Exception( "Не указан путь к файлу!" );

            if(_sheets == null || _sheets?.Count == 0)
                throw new Exception( "Не заданы данные для импорта!" );

            if(_package == null)
                Init(_filePath);

            var sheet = _sheets.First();

            ExcelWorksheet workSheet;

            if(sheet.SheetName != null)
                workSheet = _package.Workbook.Worksheets[sheet.SheetName];
            else
                workSheet = _package.Workbook.Worksheets.FirstOrDefault();

            if (workSheet == null)
                throw new Exception( "В файле нет страницы для импорта." );

            int rowCount = workSheet.Dimension.End.Row;

            var columns = sheet.GetColumns();

            int columnCount = workSheet.Dimension.End.Column;

            for(int col = 1; col <= columnCount; col++)
            {
                var column = columns.FirstOrDefault( c => c.ColumnName == (string)workSheet.Cells[_currentRow, col].Value );

                if(column != null)
                    column.ColumnNumber = col;
            }

            columns = columns.Where( c => c.ColumnNumber > 0 ).ToList();

            var result = new T[rowCount - _currentRow];
            _currentRow++;

            int i = 0;
            while(_currentRow <= rowCount)
            {
                result[i] = new T();
                foreach (var col in columns)
                {
                    sheet.SetPropertyValue<T>( result[i], col.PropertyName, workSheet.Cells[_currentRow, col.ColumnNumber].Value );
                }

                i++;
                _currentRow++;
            }

            sheet.Data = result;
        }
    }
}
