using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using VPay.DocSys.Generation.Common;

namespace VPay.DocSys.Generation
{
    public class EobXmlParser : IEobXmlParser
    {
        
        public async Task<IDictionary<string, dynamic>> EobToDictionaryAsync(Stream stream)
        {
            IDictionary<string, dynamic> result = new Dictionary<string, dynamic>();
            var settings = new XmlReaderSettings()
            {
                Async = true,
                IgnoreWhitespace = true
            };

            using (var reader = XmlReader.Create(stream, settings))
            {
                var currentNode = result;
                List<dynamic> values = new List<dynamic>();
                while (await reader.ReadAsync())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        result = await ProcessNode(reader);
                    }
                }
            }

            return result;
        }

        private async Task<dynamic> ProcessNode(XmlReader reader)
        {
            IDictionary<string, dynamic> result = new Dictionary<string, dynamic>();
            
            var values = new List<dynamic>();
            while (await reader.ReadAsync())
            {
                string nodeName = reader.Name;
                if (reader.NodeType == XmlNodeType.Element)
                {
                    
                    // IsEmptyElement = self closing tag, so need to add the value to the result with a NULL value.
                    if (reader.IsEmptyElement)
                    {
                        if (reader.HasAttributes)
                        {
                            var attributeValues = ConvertAttributes(reader);
                            AddResults(result, nodeName, attributeValues);
                        }
                        else
                        {
                            AddResults(result, nodeName, new List<dynamic> {null});
                        }
                    }
                    else
                    {
                        var value = await ProcessNode(reader);
                        AddResults(result, nodeName, value);
                    }
                }
                else if (reader.NodeType == XmlNodeType.Text)
                {
                    values.Add(ConvertToValue(reader.Value));
                }
                else if (reader.NodeType == XmlNodeType.EndElement)
                {
                    if (values.Count > 0)
                    {
                        return values;
                    }
                    // if count == 0 and results is empty then the result should be NULL
                    if (!result.Any())
                    {
                        return new List<dynamic>() { null };
                    }

                    ProcessKeys(result);

                    return result;
                }
            }

            ProcessKeys(result);

            return result;
        }

        private static void AddResults(IDictionary<string, dynamic> result, string nodeName, dynamic value)
        {
            if (result.ContainsKey(nodeName))
            {
                // if the current dictionary value is not a list change it to a list
                if (!(result[nodeName] is List<dynamic>))
                {
                    result[nodeName] = new List<dynamic> {result[nodeName] };
                }

                // if the current value is a list then AddRange otherwise add a single item.
                if (value is List<dynamic>)
                {
                    result[nodeName].AddRange(value);
                }
                else
                {
                    result[nodeName].Add(value);
                }
            }
            else
            {
                result.Add(nodeName, value);
            }
        }

        private dynamic ConvertAttributes(XmlReader reader)
        {
            IDictionary<string, dynamic> result = new Dictionary<string, dynamic>();
            while (reader.MoveToNextAttribute())
            {
                result.Add(reader.Name, ConvertToValue(reader.Value));
            }

            reader.MoveToElement();
             return result;
        }

        private void ProcessKeys(IDictionary<string, dynamic> result)
        {
            // If key in the dictionary has a list with only 1 item then change to a single item
            var keys = new List<string>(result.Keys);

            foreach (var key in keys)
            {
                if (result[key] is List<dynamic> toList && toList.Count == 1)
                {
                    result[key] = toList[0];
                }
            }

        }


        public object ConvertToValue(string value)
        {
            decimal decValue;
            if (value != null && value.Contains(".") && decimal.TryParse(value, out decValue))
            {
                return decValue;
            }

            return value;
        }

    }
}
