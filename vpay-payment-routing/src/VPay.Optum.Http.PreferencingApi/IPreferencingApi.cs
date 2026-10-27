using System.Threading;
using System.Threading.Tasks;
using Refit;
using VPay.Optum.Http.PreferencingApi.Models;

namespace VPay.Optum.Http.PreferencingApi
{
    public interface IPreferencingApi
    {
        [Get("/api/optum-ach-enrollments/search")]
        Task<OptumAchEnrollmentDto> GetOptumAchPayorEnrollmentsSearch(
          [Query] OptumAchEnrollmentSearchParam param,
            CancellationToken token = default);
    }
}
