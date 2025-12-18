using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    public class CompanyDA : ICompanyDA
    {
        public DataTable GetCompany(ref string strErrMessage)
        {
            DataSet dataSet = new DataSet();

            // 1. Lấy chuỗi thô từ config, ví dụ: "'SGTT', 'XYZ', 'ABC'"
            string rawCompanyCodes = ApplicationConfiguration.CompanyCodes;

            // 2. "Làm sạch" và "Tách" chuỗi thành một danh sách (List)
            //    Biến "'SGTT', 'XYZ'" thành một List<string> chứa ["SGTT", "XYZ"]
            List<string> companyList = rawCompanyCodes
                .Replace("'", "")  // Bỏ tất cả dấu nháy đơn
                .Split(',')       // Tách bằng dấu phẩy
                .Select(code => code.Trim()) // Xóa mọi khoảng trắng thừa
                .Where(code => !string.IsNullOrEmpty(code)) // Bỏ các mục rỗng
                .ToList();

            // Nếu không có công ty nào trong config, trả về bảng trống
            if (companyList.Count == 0)
            {
                strErrMessage = "Không có mã công ty hợp lệ nào được cấu hình trong App.config.";
                return new DataTable();
            }

            // 3. Xây dựng động các tham số (@p0, @p1, @p2...)
            //    Đây là phần "thần kỳ" để vá lỗi SQL Injection
            var parameters = new List<SqlParameter>();
            var paramNames = new List<string>();

            for (int i = 0; i < companyList.Count; i++)
            {
                string paramName = $"@p{i}"; // Ví dụ: @p0, @p1
                paramNames.Add(paramName);
                parameters.Add(new SqlParameter(paramName, companyList[i]));
            }

            // 4. Tạo câu lệnh SQL với các tham số động
            //    Kết quả sẽ là: "... WHERE ... Code IN (@p0, @p1, @p2)"
            string query = $"SELECT RTRIM(Code) AS Code, Name FROM [Common].[dbo].[Company] " +
                           $"WHERE [Common].[dbo].[Company].Code IN ({string.Join(", ", paramNames)}) " +
                           $"ORDER BY Code";

            try
            {
                // 5. Sử dụng khối 'using' để quản lý tài nguyên
                using (var connection = new SqlConnection(ApplicationConfiguration.ConnectionString))
                using (var command = new SqlCommand(query, connection))
                using (var adapter = new SqlDataAdapter(command))
                {
                    // 6. Thêm danh sách tham số đã tạo vào Command
                    command.Parameters.AddRange(parameters.ToArray());

                    adapter.Fill(dataSet);
                }
            }
            catch (Exception ex)
            {
                strErrMessage = ex.Message;
            }

            // Trả về...
            if (dataSet.Tables.Count > 0)
            {
                return dataSet.Tables[0];
            }
            return new DataTable();
        }

        public async Task<List<Company>> GetCompanyAsync()
        {
            var companyList = new List<Company>(); // Khởi tạo danh sách trả về

            // ... (Phần logic lấy companyList từ config y hệt như cũ) ...
            string rawCompanyCodes = ApplicationConfiguration.CompanyCodes;
            List<string> configuredCodes = rawCompanyCodes
                .Replace("'", "")
                .Split(',')
                .Select(code => code.Trim())
                .Where(code => !string.IsNullOrEmpty(code))
                .ToList();

            if (configuredCodes.Count == 0)
            {
                throw new Exception("Please config Company list!!!");
            }

            var parameters = new List<SqlParameter>();
            var paramNames = new List<string>();
            for (int i = 0; i < configuredCodes.Count; i++)
            {
                string paramName = $"@p{i}";
                paramNames.Add(paramName);
                parameters.Add(new SqlParameter(paramName, configuredCodes[i]));
            }

            string query = $"SELECT RTRIM(Code) AS Code, Name FROM [Common].[dbo].[Company] " +
                           $"WHERE [Common].[dbo].[Company].Code IN ({string.Join(", ", paramNames)}) " +
                           $"ORDER BY Code";

            try
            {
                using (var connection = new SqlConnection(ApplicationConfiguration.ConnectionString))
                {
                    await connection.OpenAsync(); // 1. Mở kết nối Async

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddRange(parameters.ToArray());

                        // 2. Thực thi bằng ExecuteReaderAsync (thay vì Fill)
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // 3. Đọc từng dòng (row-by-row) một cách bất đồng bộ
                            while (await reader.ReadAsync())
                            {
                                // 4. Tạo đối tượng Company mới
                                var company = new Company
                                {
                                    // 5. Đọc dữ liệu (an toàn về kiểu)
                                    Code = reader.GetString(reader.GetOrdinal("Code")),
                                    Name = reader.GetString(reader.GetOrdinal("Name"))
                                };
                                // 6. Thêm vào danh sách
                                companyList.Add(company);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Ném lỗi để tầng UI bắt
                throw new Exception($"Query Error CompanyDA: {ex.Message}", ex);
            }

            return companyList; // Trả về danh sách
        }
    }
}
