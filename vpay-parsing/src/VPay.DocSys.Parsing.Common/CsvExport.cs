using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Reflection;
using System.Text;

namespace VPay.DocSys.Parsing.Common
{

    public class CsvExport<T> where T : class
    {
        // Lifted from https://stackoverflow.com/questions/2422212/how-to-create-csv-excel-file-c
        public List<T> Objects;

        public CsvExport(List<T> objects)
        {
            Objects = objects;
        }



        public string Export(bool includeHeaderLine = true)
        {

            StringBuilder sb = new StringBuilder();
            //Get properties using reflection.
            IList<PropertyInfo> propertyInfos = typeof(T).GetProperties();

            if (includeHeaderLine)
            {
                //add header line.
                foreach (PropertyInfo propertyInfo in propertyInfos)
                {
                    sb.Append(propertyInfo.Name).Append(",");
                }
                sb.Remove(sb.Length - 1, 1).AppendLine();
            }

            //add value for each property.
            foreach (T obj in Objects)
            {
                foreach (PropertyInfo propertyInfo in propertyInfos)
                {
                    sb.Append(MakeValueCsvFriendly(propertyInfo.GetValue(obj, null))).Append(",");
                }
                sb.Remove(sb.Length - 1, 1).AppendLine();
            }

            return sb.ToString();
        }

        //export to a file.
        public void ExportToFile(string path, bool includeHeaderLine = true)
        {
            File.WriteAllText(path, Export(includeHeaderLine));
        }

        //export as binary data.
        public byte[] ExportToBytes(bool includeHeaderLine = true)
        {
            return Encoding.UTF8.GetBytes(Export(includeHeaderLine));
        }

        //get the csv value for field.
        private static string MakeValueCsvFriendly(object value)
        {
            if (value == null)
            {
                return "";
            }
            if (value is INullable { IsNull: true })
            {
                return "";
            }

            if (value is DateTime time)
            {
                if (time.TimeOfDay.TotalSeconds == 0)
                {
                    return time.ToString("yyyy-MM-dd");
                }
                return time.ToString("yyyy-MM-dd HH:mm:ss");
            }
            string output = value.ToString();

            if (output.Contains(",") || output.Contains("\""))
            {
                output = '"' + output.Replace("\"", "\"\"") + '"';
            }
            return output;

        }
    }

}
