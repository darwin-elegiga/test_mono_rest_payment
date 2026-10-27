using FluentValidation.TestHelper;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Api.Validators;
using Xunit;

namespace VPay.DocSys.Generation.Api.Tests.ValidatorTests
{
    public class StampPdfParamValidatorTests
    {
        public StampPdfParamValidator validator;

        public StampPdfParamValidatorTests()
        {
            validator = new StampPdfParamValidator();
        }

        [Fact]
        public void OutputFileName_Empty_ShouldHaveValidationError()
        {
            var model = new StampPdfParam { OutputFileName = "" };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.OutputFileName);
        }

        [Fact]
        public void OutputFileName_Null_ShouldHaveValidationError()
        {
            var model = new StampPdfParam { OutputFileName = null };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.OutputFileName);
        }

        [Fact]
        public void OutputFileName_SpecifiedValue_ShouldNotHaveValidationError()
        {
            var model = new StampPdfParam { OutputFileName = "output.pdf" };
            var result = validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.OutputFileName);
        }

        [Fact]
        public void FileName_Empty_ShouldHaveValidationError()
        {
            var model = new StampPdfParam { FileName = "" };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.FileName);
        }

        [Fact]
        public void FileName_Null_ShouldHaveValidationError()
        {
            var model = new StampPdfParam { FileName = null };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.FileName);
        }

        [Fact]
        public void FileName_SpecifiedValue_ShouldNotHaveValidationError()
        {
            var model = new StampPdfParam { FileName = "file.pdf" };
            var result = validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.FileName);
        }

        [Fact]
        public void StampFileName_Empty_ShouldHaveValidationError()
        {
            var model = new StampPdfParam { StampFileName = "" };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.StampFileName);
        }

        [Fact]
        public void StampFileName_Null_ShouldHaveValidationError()
        {
            var model = new StampPdfParam { StampFileName = null };
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.StampFileName);
        }

        [Fact]
        public void StampFileName_SpecifiedValue_ShouldNotHaveValidationError()
        {
            var model = new StampPdfParam { StampFileName = "stampFile.pdf" };
            var result = validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.StampFileName);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void UseAllPagesOfStampFile_ShouldNotHaveValidationError(bool? value)
        {
            var model = new StampPdfParam { UseAllPagesOfStampFile = value };
            var result = validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.UseAllPagesOfStampFile);
        }
    }
}
