using System.IO;
using System.Threading.Tasks;
using VPay.Optum.PaymentRouting.Data.Sql.Models;

namespace VPay.Optum.PaymentRouting.Data.Sql.FinSeg
{
    public interface IFinSegFileWriter
    {
        Task BuildFile(TextWriter writer, FinSegFile file);
    }
}
