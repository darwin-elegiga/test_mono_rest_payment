using System;
using System.Linq;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace VPay.DocSys.Generation.Api
{
    public static class ActionDescriptorExtensions
    {
        public static ApiVersionModel GetApiVersion(this ActionDescriptor actionDescriptor)
        {
            return actionDescriptor?.Properties
              .Where((kvp) => ((Type)kvp.Key).Equals(typeof(ApiVersionModel)))
              .Select(kvp => kvp.Value as ApiVersionModel).FirstOrDefault();
        }
    }
}
