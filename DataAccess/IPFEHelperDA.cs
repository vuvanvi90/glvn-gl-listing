using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IPFEHelperDA
    {
        Task<List<Phase>> GetPhaseAsync();
        Task<List<Lot>> GetLotAsync();
    }
}
