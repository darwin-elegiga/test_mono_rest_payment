using System.Collections.Generic;

namespace VPay.DocSys.Generation.Common
{
    public class FieldDictionary : Dictionary<string, FieldConfiguration>, IDictionary<string, FieldConfiguration>
    {
        public FieldDictionary() : base() { }
        public FieldDictionary(IDictionary<string, FieldConfiguration> rho) : base(rho) { }
    }
}
