using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface ICompanyDA
    {
        Task<List<Company>> GetCompanyAsync();
    }
}
