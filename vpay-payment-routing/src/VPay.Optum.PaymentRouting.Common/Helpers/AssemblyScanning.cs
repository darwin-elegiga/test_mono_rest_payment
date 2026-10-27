using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace VPay.Optum.PaymentRouting.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class AssemblyScanning
    {
        public static Assembly[] GetVPayAssemblies(Type entryType)
        {
            var checkedAssemblies = new HashSet<Assembly>();
            var toCheck = new Queue<Assembly>();
            toCheck.Enqueue(entryType.Assembly);

            while (toCheck.Count > 0)
            {
                var asm = toCheck.Dequeue();
                checkedAssemblies.Add(asm);

                var loaded = asm.GetReferencedAssemblies()
                    .Where(x => (x.Name?.StartsWith("VPay.") ?? false)
                        && !checkedAssemblies.Any(chx => AssemblyName.ReferenceMatchesDefinition(x, chx.GetName()))
                        && !toCheck.Any(chx => AssemblyName.ReferenceMatchesDefinition(x, chx.GetName())))
                    .Select(Assembly.Load);
                foreach (var loadedAsm in loaded)
                {
                    toCheck.Enqueue(loadedAsm);
                }
            }

            var allReferencedVPayAssemblies = checkedAssemblies.ToArray();
            if (allReferencedVPayAssemblies.Length == 0)
            {
                throw new InvalidOperationException("Entry Assembly not loaded properly.");
            }

            return allReferencedVPayAssemblies;
        }
    }
}
