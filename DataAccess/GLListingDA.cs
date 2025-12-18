using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq; // Cần thiết cho .Split() và .ToList()
using System.Text;
using System.Threading.Tasks; // Cần cho Async

namespace DataAccess
{
    public class GLListingDA : IGLListingDA
    {
        // Tìm kiếm Sổ cái 
        public async Task<List<GLTransaction>> SearchForGLListingAsync(string companyCode, string journalNo, string accountCode, string fromPostMonth, string toPostMonth)
        {
            var resultList = new List<GLTransaction>();
            var parameters = new List<SqlParameter>();

            // 1. Lấy BaseCompanyCode
            //string baseCompanyCode = ApplicationConfiguration.CompanyCode;
            //if (string.IsNullOrEmpty(baseCompanyCode))
            if (string.IsNullOrEmpty(companyCode))
                {
                throw new Exception("Please select Company before searching data");
            }
            //parameters.Add(new SqlParameter("@BaseCompanyCode", baseCompanyCode));
            parameters.Add(new SqlParameter("@BaseCompanyCode", companyCode));

            // 2. Câu SQL đã BỎ ReAccount và LedgerAcc.CrDr
            string query = $@"
                SELECT LedgerAcc.Company AS CompanyCode, 
                       (LEFT(POSTMONTH, 4) + '-' + RIGHT(POSTMONTH, 2)) AS PostMonth, 
                       CONVERT(NVARCHAR, TransactionDate, 103) AS Date, 
                       LedgerAcc.JournalNumber, 
                       LedgerAcc.AccountCode, 
                       AccountCode.Name AS AccountName, 
                       ISNULL(AccountCode.Analysis1, '') AS VASAccount, 
                       LedgerAcc.CrDr,
                       CASE WHEN LedgerAcc.CrDr = 0 THEN LedgerAcc.OriginalAmtLocal ELSE 0 END AS Debit, 
                       CASE WHEN LedgerAcc.CrDr = 1 THEN LedgerAcc.OriginalAmtLocal ELSE 0 END AS Credit, 
                       LedgerAcc.OriginalAmtLocal AS Total, 
                       LedgerAcc.[Description], 
                       LedgerAcc.ReferenceNo1 AS Ref1, 
                       LedgerAcc.ReferenceNo2 AS Ref2,
                       LedgerAcc.Allocation1, 
                       LedgerAcc.Allocation2, 
                       LedgerAcc.Allocation3, 
                       ISNULL(JnlAllocation4.Name, '') AS Allocation4, 
                       ISNULL(JnlAllocation5.Name, '') AS Allocation5, 
                       Supp.Code AS SupplierCode, 
                       Supp.Name AS SupplierName, 
                       LedgerAcc.UserLastModified, CONVERT(NVARCHAR(20), 
                       LedgerAcc.DateLastModified, 120) AS DateLastModified 
                FROM   LedgerAcc 
                       LEFT JOIN AccountCode ON (LedgerAcc.AccountCode = AccountCode.Code) AND (AccountCode.Company = @BaseCompanyCode) 
                       LEFT JOIN JnlAllocation4 ON (LedgerAcc.Allocation4 = JnlAllocation4.Code AND JnlAllocation4.Company = @BaseCompanyCode) 
                       LEFT JOIN JnlAllocation5 ON (LedgerAcc.Allocation5 = JnlAllocation5.Code AND JnlAllocation5.Company = @BaseCompanyCode) 
                       LEFT JOIN JournalChildAp ON (LedgerAcc.MatchingNo=JournalChildAP.MatchingNo) AND (LedgerAcc.JournalNumber=JournalChildAP.JournalNumber) 
                       LEFT JOIN Common..supplier Supp on (JournalChildAP.AccountCode = Supp.Code) AND (Supp.Company = JournalChildAP.Company)
            ";

            // 3. Xây dựng mệnh đề WHERE (Đã sửa logic CompanyCode)
            var whereClauses = new List<string>();

            // --- Lọc Công ty ---
            if (string.IsNullOrEmpty(companyCode))
            {
                // Yêu cầu mới: Nếu không chọn, trả về bảng trống
                return resultList; // Thoát ngay lập tức
            }
            else
            {
                whereClauses.Add("LedgerAcc.Company = @CompanyCode");
                parameters.Add(new SqlParameter("@CompanyCode", companyCode));
            }

            // ... (Phần còn lại của code xây dựng WHERE ... y hệt phiên bản cũ) ...

            // --- Lọc Số Bút toán (JournalNo) ---
            if (!string.IsNullOrEmpty(journalNo))
            {
                whereClauses.Add("LedgerAcc.JournalNumber = @JournalNo");
                parameters.Add(new SqlParameter("@JournalNo", journalNo));
            }

            // --- Lọc Mã Tài khoản (AccountCode) ---
            if (!string.IsNullOrEmpty(accountCode))
            {
                if (accountCode.Contains(';'))
                {
                    List<string> accountList = accountCode.Split(';')
                        .Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a))
                        .ToList();

                    if (accountList.Count > 0)
                    {
                        var accountParamNames = new List<string>();
                        for (int i = 0; i < accountList.Count; i++)
                        {
                            string paramName = $"@Account{i}";
                            accountParamNames.Add(paramName);
                            parameters.Add(new SqlParameter(paramName, accountList[i]));
                        }
                        whereClauses.Add($"LedgerAcc.AccountCode IN ({string.Join(", ", accountParamNames)})");
                    }
                }
                else
                {
                    whereClauses.Add("LedgerAcc.AccountCode = @AccountCode");
                    parameters.Add(new SqlParameter("@AccountCode", accountCode));
                }
            }

            // --- Lọc Kỳ Hạch toán (PostMonth) ---
            if (!string.IsNullOrEmpty(fromPostMonth))
            {
                whereClauses.Add("PostMonth >= @FromPostMonth");
                parameters.Add(new SqlParameter("@FromPostMonth", fromPostMonth));
            }
            if (!string.IsNullOrEmpty(toPostMonth))
            {
                whereClauses.Add("PostMonth <= @ToPostMonth");
                parameters.Add(new SqlParameter("@ToPostMonth", toPostMonth));
            }

            // 4. Gắn mệnh đề WHERE
            if (whereClauses.Count > 0)
            {
                query += " WHERE " + string.Join(" AND ", whereClauses);
            }
            query += " ORDER BY PostMonth, TransactionDate, LedgerAcc.JournalNumber";

            // 5. Thực thi bất đồng bộ
            try
            {
                using (var connection = new SqlConnection(ApplicationConfiguration.ConnectionString))
                {
                    await connection.OpenAsync(); // Mở Async

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddRange(parameters.ToArray());

                        // Dùng ExecuteReaderAsync
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var item = new GLTransaction
                                {
                                    CompanyCode = reader["CompanyCode"] != DBNull.Value ? (string)reader["CompanyCode"] : null,
                                    PostMonth = reader["PostMonth"] != DBNull.Value ? (string)reader["PostMonth"] : null,
                                    Date = reader["Date"] != DBNull.Value ? (string)reader["Date"] : null,
                                    JournalNumber = reader["JournalNumber"] != DBNull.Value ? (string)reader["JournalNumber"] : null,
                                    AccountCode = reader["AccountCode"] != DBNull.Value ? (string)reader["AccountCode"] : null,
                                    AccountName = reader["AccountName"] != DBNull.Value ? (string)reader["AccountName"] : null,
                                    VASAccount = reader["VASAccount"] != DBNull.Value ? (string)reader["VASAccount"] : null,
                                    CrDr = reader["CrDr"] != DBNull.Value ? (bool)reader["CrDr"] : false,
                                    Debit = reader["Debit"] != DBNull.Value ? Convert.ToDecimal(reader["Debit"]) : 0,
                                    Credit = reader["Credit"] != DBNull.Value ? Convert.ToDecimal(reader["Credit"]) : 0,
                                    Total = reader["Total"] != DBNull.Value ? Convert.ToDecimal(reader["Total"]) : 0,
                                    Description = reader["Description"] != DBNull.Value ? (string)reader["Description"] : null,
                                    Ref1 = reader["Ref1"] != DBNull.Value ? (string)reader["Ref1"] : null,
                                    Ref2 = reader["Ref2"] != DBNull.Value ? (string)reader["Ref2"] : null,
                                    Allocation1 = reader["Allocation1"] != DBNull.Value ? (string)reader["Allocation1"] : null,
                                    Allocation2 = reader["Allocation2"] != DBNull.Value ? (string)reader["Allocation2"] : null,
                                    Allocation3 = reader["Allocation3"] != DBNull.Value ? (string)reader["Allocation3"] : null,
                                    Allocation4 = reader["Allocation4"] != DBNull.Value ? (string)reader["Allocation4"] : null,
                                    Allocation5 = reader["Allocation5"] != DBNull.Value ? (string)reader["Allocation5"] : null,
                                    SupplierCode = reader["SupplierCode"] != DBNull.Value ? (string)reader["SupplierCode"] : null,
                                    SupplierName = reader["SupplierName"] != DBNull.Value ? (string)reader["SupplierName"] : null,
                                    UserLastModified = reader["UserLastModified"] != DBNull.Value ? (string)reader["UserLastModified"] : null,
                                    DateLastModified = reader["DateLastModified"] != DBNull.Value ? (string)reader["DateLastModified"] : null
                                };
                                resultList.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Query Error GLListingDA: {ex.Message}", ex);
            }

            return resultList;
        }
    }
}