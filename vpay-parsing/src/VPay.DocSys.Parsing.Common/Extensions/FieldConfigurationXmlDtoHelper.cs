using System.Collections.Generic;
using System.Linq;

namespace VPay.DocSys.Parsing.Common
{
    public static class FieldConfigurationXmlDtoHelper
    {
        /// <summary>
        /// Consolidates all <paramref name="fields"/> by selecting the last <see cref="FieldConfigurationXmlDto"/> by <see cref="FieldConfigurationXmlDto.Name"/>
        /// </summary>
        /// <param name="fields"></param>
        /// <returns>array of unique <see cref="FieldConfigurationXmlDto"/>s</returns>
        public static List<FieldConfigurationXmlDto> ConsolidateFields(this IEnumerable<FieldConfigurationXmlDto> fields)
        {
            return fields
                .GroupBy(_ => _.Name)
                .Select(_ => _.Last())
                .OrderBy(_ => _.Name)
                .ToList();
        }
    }
}
