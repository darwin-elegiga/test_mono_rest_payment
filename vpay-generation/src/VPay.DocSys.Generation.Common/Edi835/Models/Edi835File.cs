using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VPay.DocSys.Generation.Common.Edi835
{
    public abstract class Edi835File : IDisposable
    {
        public virtual string Filename { get; set; }
        public virtual string Content { get; set; }

        protected Stream _contentStream;
        public virtual Stream ContentAsStream(  )
        {
            _contentStream?.Dispose();
            _contentStream = new MemoryStream(Encoding.UTF8.GetBytes(Content));
            return _contentStream;
        }

        public void Dispose()
        {
            _contentStream?.Dispose();
        }
    }
}
