using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IGLListingDA
    {
        Task<List<GLTransaction>> SearchForGLListingAsync(string companyCode, string journalNo, string accountCode, string fromPostMonth, string toPostMonth);
    }
}
