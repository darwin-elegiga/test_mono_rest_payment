using System.Collections;
using System.Collections.Generic;

namespace VPay.DocSys.Generation.Tests.TestModels
{
    public class GeneratePacketCustomPdfNotFoundData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new TestCustomPdfFileList { new TestCustomPdfFile { Name = "C://test//test1.pdf", Exists = false }, new TestCustomPdfFile { Name = "C://test//test2.pdf", Exists = false } } };
            yield return new object[] { new TestCustomPdfFileList { new TestCustomPdfFile { Name = "C://test//test1.pdf", Exists = true }, new TestCustomPdfFile { Name = "C://test//test2.pdf", Exists = false } } };
            yield return new object[] { new TestCustomPdfFileList { new TestCustomPdfFile { Name = "C://test//test1.pdf", Exists = true }, new TestCustomPdfFile { Name = "C://test//test2.pdf", Exists = true }, new TestCustomPdfFile { Name = "C://test//test3.pdf", Exists = false } } };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
