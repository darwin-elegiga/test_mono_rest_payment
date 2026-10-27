using FluentValidation.TestHelper;
using System.Collections.Generic;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Api.Validators;
using Xunit;

namespace VPay.DocSys.Generation.Api.Tests.ValidatorTests
{
    public class MergePdfParamValidatorTests
    {
        public MergePdfParamValidator validator;

        public MergePdfParamValidatorTests()
        {
            validator = new MergePdfParamValidator();
        }

        [Fact]
        public void OutputFileName_Empty_ShouldHaveValidationError()
        {
            var result = validator.TestValidate(new MergePdfParam { OutputFileName = "", Files = new List<string> { "file.pdf" } });
            result.ShouldHaveValidationErrorFor(x => x.OutputFileName);
        }

        [Fact]
        public void OutputFileName_Null_ShouldHaveValidationError()
        {
            var result = validator.TestValidate(new MergePdfParam { OutputFileName = null, Files = new List<string> { "file.pdf" } });
            result.ShouldHaveValidationErrorFor(x => x.OutputFileName);
        }

        [Fact]
        public void OutputFileName_SpecifiedValue_ShouldNotHaveValidationError()
        {
            var result = validator.TestValidate(new MergePdfParam { OutputFileName = "output.pdf", Files = new List<string> { "file.pdf" } });
            result.ShouldNotHaveValidationErrorFor(x => x.OutputFileName);
        }

        [Fact]
        public void Files_Empty_ShouldHaveValidationError()
        {
            var result = validator.TestValidate(new MergePdfParam { OutputFileName = "output.pdf", Files = new List<string>() });
            result.ShouldHaveValidationErrorFor(x => x.Files);
        }

        [Fact]
        public void Files_Null_ShouldHaveValidationError()
        {
            var result = validator.TestValidate(new MergePdfParam { OutputFileName = "output.pdf", Files = null });
            result.ShouldHaveValidationErrorFor(x => x.Files);
        }

        [Fact]
        public void Files_HasValues_ShouldHaveValidationError()
        {
            var result = validator.TestValidate(new MergePdfParam { OutputFileName = "output.pdf", Files = new List<string>() { "listvalue.pdf" } });
            result.ShouldNotHaveValidationErrorFor(x => x.Files);
        }
    }
}
