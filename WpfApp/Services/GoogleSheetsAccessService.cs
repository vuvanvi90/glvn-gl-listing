using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WpfApp.Services
{
    public class GoogleSheetsAccessService : IAppAccessService
    {
        private const string SpreadsheetId = "1YYtNBKBEk5GSubUsvxaQI9tIY_c2B3ivblkdjdhRDPY";
        private const string Range = "Sheet1!A:G";

        public async Task<AppAccessResult> CheckAccessAsync()
        {
            var result = new AppAccessResult { IsActive = false, CompanyCode = "", CompanyCodes = "" };

            try
            {
                string machineId = GetMachineID();
                string resourceName = "WpfApp.credentials.json"; // Tên file nhúng từ bước bảo mật trước
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();

                GoogleCredential credential;
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return result;
                    credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
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
                        if (row.Count >= 4 && row[0].ToString() == machineId)
                        {
                            result.IsActive = row[3].ToString().ToUpper() == "TRUE"; // Cột D

                            // Lấy CompanyCode (Cột F - Index 5)
                            if (row.Count > 5) result.CompanyCode = row[5].ToString();

                            // Lấy CompanyCodes (Cột G - Index 6)
                            if (row.Count > 6) result.CompanyCodes = row[6].ToString();

                            return result;
                        }
                    }
                }

                // Nếu là máy mới, thêm vào với cấu hình mặc định
                await AddNewMachineToSheetAsync(service, machineId);

                result.IsActive = true;
                result.CompanyCode = "SGTT"; // Mặc định cho máy mới
                result.CompanyCodes = "'SGTT'";
                return result;
            }
            catch
            {
                return result;
            }
        }

        private async Task AddNewMachineToSheetAsync(SheetsService service, string machineId)
        {
            try
            {
                var oblist = new List<object>()
                {
                    machineId,
                    Environment.MachineName,
                    Environment.UserName,
                    "TRUE",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    "SGTT",                                        // Giá trị mặc định cho cột F
                    "'SGTT','DANXUAN','GPHSCJSC','TLREC','TRUONGTIN','GLBDCL','VLILC','GLNVI','PHUCDAT'" // Mặc định cột G
                };

                var valueRange = new ValueRange() { Values = new List<IList<object>> { oblist } };
                var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, Range);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                await appendRequest.ExecuteAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private string GetMachineID()
        {
            return $"{Environment.MachineName}-{Environment.UserName}";
        }
    }
}