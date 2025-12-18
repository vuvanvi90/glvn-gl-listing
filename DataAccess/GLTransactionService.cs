using System.Collections.Generic;
using System.Linq;

namespace DataAccess
{
    public class GLTransactionService : IGLTransactionService
    {
        public void PopulateCorrespondingAccounts(List<GLTransaction> transactions)
        {
            if (transactions == null || !transactions.Any()) return;

            var journalGroups = transactions.GroupBy(x => x.JournalNumber);

            foreach (var group in journalGroups)
            {
                // Debit Accounts (CrDr = false/0)
                var debitAccounts = group
                    .Where(x => !x.CrDr)
                    .Select(x => x.VASAccount?.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct()
                    .ToList();

                // Credit Accounts (CrDr = true/1)
                var creditAccounts = group
                    .Where(x => x.CrDr)
                    .Select(x => x.VASAccount?.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct()
                    .ToList();

                string debitString = string.Join(",", debitAccounts);
                string creditString = string.Join(",", creditAccounts);

                foreach (var item in group)
                {
                    if (!item.CrDr) // Debit row
                    {
                        item.CorrespondingAccount = creditString;
                    }
                    else // Credit row
                    {
                        item.CorrespondingAccount = debitString;
                    }
                }
            }
        }
    }
}
