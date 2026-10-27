namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Field Configuration
    /// </summary>
    public class FieldConfiguration
    {
        /// <summary>
        /// Name of the field
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of Field
        /// </summary>
        public TypeEnum Type { get; set; }

        /// <summary>
        /// Field Source
        /// </summary>
        public SourceEnum Source { get; set; }

        /// <summary>
        /// Value of the Field
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Source of the Field
        /// </summary>
        /// <remarks>
        /// All <see cref="SourceEnum.Variable"/> should resolve to a <see cref="SourceEnum.Literal"/> <see cref="FieldConfiguration"/>
        /// </remarks>
        public enum SourceEnum
        {
            Literal,
            Variable
        }

        /// <summary>
        /// Type of Field
        /// </summary>
        /// <remarks>
        /// Currently only dealing with <see cref="TypeEnum.String"/>s
        /// </remarks>
        public enum TypeEnum
        {
            Boolean,
            Dict,
            Float,
            Integer,
            List,
            String,
            Tuple,
            Currency,
        }
    }
}
