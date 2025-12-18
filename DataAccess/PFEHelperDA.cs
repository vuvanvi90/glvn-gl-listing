using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace DataAccess
{
    public class PFEHelperDA : IPFEHelperDA
    {
        public async Task<List<Phase>> GetPhaseAsync()
        {
            var list = new List<Phase>();
            string query = "SELECT Code, Name FROM PFE_VN.dbo.Phase WHERE Company = @Company ORDER BY Code";

            try
            {
                using (var connection = new SqlConnection(ApplicationConfiguration.ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(new SqlParameter("@Company", ApplicationConfiguration.CompanyCode));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(new Phase
                                {
                                    Code = reader["Code"] != DBNull.Value ? (string)reader["Code"] : null,
                                    Name = reader["Name"] != DBNull.Value ? (string)reader["Name"] : null
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting Phase: {ex.Message}", ex);
            }

            return list;
        }

        public async Task<List<Lot>> GetLotAsync()
        {
            var list = new List<Lot>();
            string query = "SELECT Code, Name FROM PFE_VN.dbo.Lot WHERE Company = @Company ORDER BY Code";

            try
            {
                using (var connection = new SqlConnection(ApplicationConfiguration.ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(new SqlParameter("@Company", ApplicationConfiguration.CompanyCode));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(new Lot
                                {
                                    Code = reader["Code"] != DBNull.Value ? (string)reader["Code"] : null,
                                    Name = reader["Name"] != DBNull.Value ? (string)reader["Name"] : null
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting Lot: {ex.Message}", ex);
            }

            return list;
        }
    }
}
