using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data; // Cần thêm dòng này cho ValueRange
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WpfApp.Services
{
    public class GoogleSheetsAccessService : IAppAccessService
    {
        private const string SpreadsheetId = "1YYtNBKBEk5GSubUsvxaQI9tIY_c2B3ivblkdjdhRDPY";
        private const string Range = "Sheet1!A:E";

        public async Task<bool> CheckAccessAsync()
        {
            try
            {
                string machineId = GetMachineID();
                string credentialPath = "credentials.json";

                if (!File.Exists(credentialPath))
                    return false;

                GoogleCredential credential;
                using (var stream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(SheetsService.Scope.Spreadsheets);
                }

                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "GamudaGL Access Checker"
                });

                var request = service.Spreadsheets.Values.Get(SpreadsheetId, Range);
                var response = await request.ExecuteAsync();
                var values = response.Values;

                if (values != null)
                {
                    foreach (var row in values)
                    {
                        // Kiểm tra nếu tìm thấy MachineID (Cột A - index 0)
                        // Cột Status bây giờ là Cột D (index 3)
                        if (row.Count >= 4 && row[0].ToString() == machineId)
                        {
                            return row[3].ToString().ToUpper() == "TRUE";
                        }
                    }
                }

                // NẾU CHƯA CÓ TRONG DANH SÁCH -> GỌI HÀM THÊM MỚI
                // Mặc định ở đây mình đang để trạng thái thêm mới là TRUE (cho phép vào ngay)
                await AddNewMachineToSheetAsync(service, machineId);

                // Trả về true để người dùng mới có thể vào app luôn lần đầu
                // Nếu bạn muốn phải duyệt tay trên Sheets mới được vào, hãy đổi thành `return false;`
                return true;
            }
            catch
            {
                // Lỗi mạng hoặc lỗi API -> Khóa
                return false;
            }
        }

        private async Task AddNewMachineToSheetAsync(SheetsService service, string machineId)
        {
            try
            {
                // Chuẩn bị dữ liệu cho 1 dòng mới
                var oblist = new List<object>()
                {
                    machineId,                                     // Cột A: Machine ID
                    Environment.MachineName,                       // Cột B: Tên máy tính
                    Environment.UserName,                          // Cột C: Tài khoản Windows đang login
                    "TRUE",                                        // Cột D: Status (TRUE = Cho phép, FALSE = Khóa)
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")   // Cột E: Thời gian ghi nhận
                };

                var valueRange = new ValueRange();
                valueRange.Values = new List<IList<object>> { oblist };

                // Tạo request Append để thêm vào dòng trống tiếp theo của file Sheets
                var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, Range);

                // Chế độ USERENTERED giúp Google Sheets tự động format ngày tháng, chữ nghĩa cho đẹp
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

                await appendRequest.ExecuteAsync();
            }
            catch (Exception ex)
            {
                // Bỏ qua lỗi ghi (để không làm sập app), có thể log lại nếu cần
                System.Diagnostics.Debug.WriteLine($"Lỗi khi ghi máy mới: {ex.Message}");
            }
        }

        private string GetMachineID()
        {
            return $"{Environment.MachineName}-{Environment.UserName}";
        }
    }
}