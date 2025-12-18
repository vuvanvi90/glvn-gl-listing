using System;

namespace DataAccess
{
    public class GLTransaction
    {
        public int STT { get; set; }
        public string CompanyCode { get; set; }
        public string PostMonth { get; set; }
        public string Date { get; set; }
        public string JournalNumber { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public string VASAccount { get; set; }
        public bool CrDr { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Total { get; set; }
        public string Description { get; set; }
        public string Ref1 { get; set; }
        public string Ref2 { get; set; }
        public string Allocation1 { get; set; }
        public string Allocation2 { get; set; }
        public string Allocation3 { get; set; }
        public string Allocation4 { get; set; }
        public string Allocation5 { get; set; }
        public string SupplierCode { get; set; }
        public string SupplierName { get; set; }
        public string UserLastModified { get; set; }
        public string DateLastModified { get; set; }
        
        // Calculated property
        public string CorrespondingAccount { get; set; }
    }
}
