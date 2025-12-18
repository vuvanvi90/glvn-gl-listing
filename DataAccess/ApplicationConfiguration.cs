using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace DataAccess
{
    public class ApplicationConfiguration
    {
        private static string fieldConnectionString = EncryptionHelper.DecryptStr(ConfigurationManager.ConnectionStrings["ConnectionString"].ToString());

        private static string fieldCompanyCode = ConfigurationManager.AppSettings["CompanyCode"];

        private static string fieldCompanyCodes = ConfigurationManager.AppSettings["CompanyCodes"];

        public static string ConnectionString
        {
            get
            {
                return ApplicationConfiguration.fieldConnectionString;
            }
            set
            {
                ApplicationConfiguration.fieldConnectionString = value;
            }
        }

        public static string CompanyCode
        {
            get
            {
                return ApplicationConfiguration.fieldCompanyCode;
            }
            set
            {
                ApplicationConfiguration.fieldCompanyCode = value;
            }
        }

        public static string CompanyCodes
        {
            get
            {
                return ApplicationConfiguration.fieldCompanyCodes;
            }
            set
            {
                ApplicationConfiguration.fieldCompanyCodes = value;
            }
        }

        private ApplicationConfiguration()
        {
            ApplicationConfiguration.fieldConnectionString = EncryptionHelper.DecryptStr(ConfigurationManager.ConnectionStrings["ConnectionString"].ToString());
            ApplicationConfiguration.fieldCompanyCode = ConfigurationManager.AppSettings["CompanyCode"];
            ApplicationConfiguration.fieldCompanyCodes = ConfigurationManager.AppSettings["CompanyCodes"];
        }

        /// <summary>
        /// Kiểm tra kết nối đến database một cách bất đồng bộ.
        /// </summary>
        /// <returns>True nếu kết nối thành công, False nếu thất bại.</returns>
        public static async Task<bool> CheckDatabaseConnectionAsync()
        {
            try
            {
                // Sử dụng 'using' để đảm bảo kết nối được đóng ngay lập tức
                // ConnectionString là thuộc tính 'public static string'
                // mà bạn đã có sẵn trong file này.
                using (var connection = new SqlConnection(ConnectionString))
                {
                    // Mở kết nối bất đồng bộ
                    await connection.OpenAsync();

                    // Nếu OpenAsync() thành công, trả về true
                    return true;
                }
            }
            catch (Exception)
            {
                // Bất kỳ lỗi nào (timeout, sai mật khẩu, server offline)
                // đều được coi là kết nối thất bại.
                return false;
            }
        }
    }
}
