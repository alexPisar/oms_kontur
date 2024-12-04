using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileWorker
{
    public class ExcelDocumentData : IDocumentData
    {
        private Array _data;
        private List<ExcelColumn> _columns;

        public ExcelDocumentData(ExcelColumnCollection columnCollection, Array data, string sheetName = null) : base()
        {
            _data = data;
            _columns = columnCollection.GetColumns();
            DefaultRowHeight = 12;
            DefaultRowWidth = 20;
            TabColor = System.Drawing.Color.Black;
            Bold = false;
            HeadRowBold = true;
            ExportColumnNames = true;
        }

        public ExcelDocumentData(ExcelColumnCollection columnCollection, string sheetName = null) : base()
        {
            _columns = columnCollection.GetColumns();
            DefaultRowHeight = 12;
            DefaultRowWidth = 20;
            TabColor = System.Drawing.Color.Black;
            Bold = false;
            HeadRowBold = true;
            ExportColumnNames = true;
        }

        public Array Data
        {
            get 
            {
                return _data;
            }
            set 
            {
                _data = value;
            }
        }

        public bool HeadRowBold { get; set; }

        public bool Bold { get; set; }

        public bool ExportColumnNames{ get; set; }

        public double HeadRowHeight { get; set; }

        public bool HeadRowAutoFilter { get; set; }

        public double DefaultRowHeight { get; set; }

        public double DefaultRowWidth { get; set; }

        public System.Drawing.Color TabColor { get; set; }

        public OfficeOpenXml.Style.ExcelHorizontalAlignment? HeadHorizontalAlignment { get; set; } = null;

        public OfficeOpenXml.Style.ExcelVerticalAlignment? HeadVerticalAlignment { get; set; } = null;

        public string SheetName{ get; set; }

        internal List<ExcelColumn> GetColumns()
        {
            return _columns;
        }

        private T GetResultByValue<T>(object value)
        {
            T result;

            result = (T)Convert.ChangeType( value, typeof( T ) );

            return result;
        }

        public object GetPropertyValueByName(object obj, string name, ExcelType type)
        {
            var value = obj?.GetType()?.GetProperty( name )?.GetValue(obj, null);

            if (value == null)
                return null;

            bool valueIsString = value.GetType() == typeof( string );

            if (type == ExcelType.String)
            {
                if (valueIsString)
                    return (string)value;

                return value.ToString();
            }
            else if (type == ExcelType.Int16)
            {
                short result;

                if (valueIsString)
                    short.TryParse( (string)value, out result );
                else
                    result = GetResultByValue<short>( value );

                return result;
            }
            else if (type == ExcelType.Int32)
            {
                int result;

                if (valueIsString)
                    int.TryParse( (string)value, out result );
                else
                    result = GetResultByValue<int>( value );

                return result;
            }
            else if (type == ExcelType.Int64)
            {
                long result;

                if (valueIsString)
                    long.TryParse( (string)value, out result );
                else
                    result = GetResultByValue<long>( value );

                return result;
            }
            else if (type == ExcelType.Double)
            {
                double result;

                if (valueIsString)
                {
                    if(!double.TryParse((string)value, out result))
                    {
                        try
                        {
                            result = double.Parse((string)value, System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch(Exception ex)
                        {

                        }
                    }
                }
                else
                    result = GetResultByValue<double>( value );

                return result;
            }

            return value;
        }

        public void SetPropertyValue<T>(T obj, string name, object newValue)
        {
            Type type = obj?.GetType()?.GetProperty( name )?.PropertyType;

            if (type != null && type.IsGenericType && Nullable.GetUnderlyingType(type) != null)
            {
                if (newValue != null)
                    type = Nullable.GetUnderlyingType(type);
                else
                    type = null;
            }

            if (type != null)
            {
                var value = Convert.ChangeType( newValue, type );

                obj?.GetType()?.GetProperty( name )?.SetValue( obj, value );
            }
            else
            {
                obj?.GetType()?.GetProperty(name)?.SetValue(obj, null);
            }
        }
    }
}
