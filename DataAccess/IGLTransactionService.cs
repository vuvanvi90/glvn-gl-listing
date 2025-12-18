using System.Collections.Generic;

namespace DataAccess
{
    public interface IGLTransactionService
    {
        void PopulateCorrespondingAccounts(List<GLTransaction> transactions);
    }
}
