using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ardalis.SmartEnum;
using Ardalis.SmartEnum.JsonNet;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace VPay.Optum.PaymentRouting.Api.Configuration
{
    /// <summary>
    ///     This is used by swagger to change any property that has <see cref="SmartEnumNameConverter{TEnum,TValue}" />
    ///     to use a string format.
    /// </summary>
    public class SmartEnumSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            var type = context.Type;

            List<KeyValuePair<PropertyInfo, KeyValuePair<string, OpenApiSchema>>> propertyMappings = type
                .GetProperties()
                .Join(
                    schema.Properties ?? new Dictionary<string, OpenApiSchema>(),
                    x => x.Name.ToLower(),
                    x => x.Key.ToLower(),
                    (x, y) => new KeyValuePair<PropertyInfo, KeyValuePair<string, OpenApiSchema>>(x, y))
                .ToList();

            foreach (KeyValuePair<PropertyInfo, KeyValuePair<string, OpenApiSchema>> propertyMapping in propertyMappings
            )
            {
                var propertyInfo = propertyMapping.Key;
                KeyValuePair<string, OpenApiSchema> propertyNameToSchemaKvp = propertyMapping.Value;

                foreach (var attribute in propertyInfo.GetCustomAttributes())
                    SetSchemaDetails(propertyNameToSchemaKvp, attribute);
            }
        }

        private static void SetSchemaDetails(KeyValuePair<string, OpenApiSchema> propertyNameToSchemaKvp,
            object propertyAttribute)
        {
            var schema = propertyNameToSchemaKvp.Value;

            if (propertyAttribute is JsonConverterAttribute attribute)
            {
                var dataType = attribute.ConverterType;
                if (dataType.IsGenericType)
                {
                    Type[] typeArguments = dataType.GetGenericArguments();
                    if (typeArguments.Length == 2)
                    {
                        var smartEnumNameConverterType = typeof(SmartEnumNameConverter<,>);
                        if (dataType == smartEnumNameConverterType.MakeGenericType(typeArguments))
                        {
                            schema.Type = "string";
                            schema.Reference = null;

                            var smartEnumType = typeof(SmartEnum<,>).MakeGenericType(typeArguments);

                            var list = (IEnumerable) smartEnumType
                                .GetProperty("List", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);

                            var enumValues = new List<IOpenApiAny>();
                            foreach (var item in list)
                                enumValues.Add(new OpenApiString(item.GetType().GetProperty("Name").GetValue(item, null)
                                    .ToString()));

                            schema.Enum = enumValues;
                        }
                    }
                }
            }
        }
    }
}