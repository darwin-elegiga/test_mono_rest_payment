using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using VPay.DocSys.Parsing.Tests.Helpers;
using VPay.DocSys.Parsing.Tests.TestModels;
using Xunit;

namespace VPay.DocSys.Parsing.Tests
{
    public class OTBParserServiceTests
    {

        [Fact]
        public void OtbBustUpAsync_WithValidFile_ShouldReturnDictionaryByBillingEntity()
        {

            var logger = TestLogger.Create<OTBParserService>();

            var sut = new OTBParserService(logger);

            IDictionary<string, IDictionary<string, string>> actual;

            var fileObjects = OtbFileModel.GenerateOtbObjects(5);

            var fileString = ToFileFormat(fileObjects);

            using (var steam = GenerateStreamFromString(fileString))
            using (var reader = new StreamReader(steam, Encoding.UTF8, true))
            {
                actual = sut.ParseOTBData(reader);
            }

            var expected = ToDictionary(fileObjects);

            actual.Should().BeEquivalentTo(expected);
        }


        public static MemoryStream GenerateStreamFromString(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
        }


        private static string ToFileFormat<T>(IEnumerable<T> objectlist)
        {
            Type t = typeof(T);
            FieldInfo[] fields = t.GetFields();

            string header = GetOtbHeader<T>();

            StringBuilder csvdata = new StringBuilder();
            csvdata.AppendLine(header);

            csvdata.Append(ConvertToLines(objectlist));

            csvdata.Append("99|$Id$");

            return csvdata.ToString();
        }

        private static string GetOtbHeader<T>()
        {
            Type t = typeof(T);
            PropertyInfo[] fields = t.GetProperties();

            StringBuilder csvdata = new StringBuilder();

            string header = String.Join('|', fields.Select(f => f.Name).ToArray());

            return $"00|{header}|";
        }

        private static string ConvertToLines<T>(IEnumerable<T> objectlist)
        {

            Type t = typeof(T);
            PropertyInfo[] fields = t.GetProperties();

            StringBuilder csvdata = new StringBuilder();

            foreach (var o in objectlist)
            {
                var line = ToCsvFields(fields, o);
                csvdata.AppendLine($"42|{line}|");
            }

            return csvdata.ToString();
        }

        private static string ToCsvFields<T>(PropertyInfo[] fields, T o)
        {
            StringBuilder linie = new StringBuilder();

            foreach (var f in fields)
            {
                if (linie.Length > 0)
                    linie.Append("|");

                var x = f.GetValue(o);

                if (x != null)
                    linie.Append(x.ToString());
            }

            return linie.ToString();
        }

        public static IDictionary<string, IDictionary<string, string>> ToDictionary(IEnumerable<OtbFileModel> models)
        {
            Type t = typeof(OtbFileModel);
            PropertyInfo[] fields = t.GetProperties();

            var result = new Dictionary<string, IDictionary<string, string>>();

            foreach (var model in models)
            {
                var dict = new Dictionary<string, string>();

                foreach (var field in fields)
                {
                    dict.Add(field.Name, field.GetValue(model).ToString());
                }

                result.Add(model.BillingEntity, dict);
            }

            return result;
        }
    }
}
