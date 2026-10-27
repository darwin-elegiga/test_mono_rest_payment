using System.Runtime.Serialization;
using FluentValidation;
using FluentValidation.Results;

namespace VPay.Gateway.Application;
[System.Serializable]
#pragma warning disable RCS1194 // Implement exception constructors.
public class NoPrsConfigurationValidationException : ValidationException
#pragma warning restore RCS1194 // Implement exception constructors.
{
    public NoPrsConfigurationValidationException(IEnumerable<ValidationFailure> errors)
        : base(errors) { }

    protected NoPrsConfigurationValidationException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
