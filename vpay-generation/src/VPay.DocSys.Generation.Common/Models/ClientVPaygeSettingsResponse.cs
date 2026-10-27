using System.Collections.Generic;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Client VPayge Settings
    /// Configuration is read from XML files for a given TPA/BE
    /// </summary>
    public class ClientVPaygeSettingsResponse
    {
        /// <summary>
        /// <see cref="List{FieldConfiguration}"/> for Common
        /// </summary>
        public IList<FieldConfiguration> Common { get; set; }

        /// <summary>
        /// <see cref="List{FieldConfiguration}"/> for Virtual Card s(<see cref="PayType.CLVCP"/>)
        /// </summary>
        public IList<FieldConfiguration> VCard { get; set; }
    }
}
