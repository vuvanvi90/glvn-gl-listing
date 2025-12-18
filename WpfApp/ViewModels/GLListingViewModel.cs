using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataAccess;
using WpfApp.Services;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
//using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace WpfApp.ViewModels
{
    public partial class GLListingViewModel : ObservableObject
    {
        private readonly IGLListingDA _glListingDA;
        private readonly ICompanyDA _companyDA;
        private readonly IGLTransactionService _glTransactionService;
        private readonly IThemeService _themeService;
        private readonly IExcelExportService _excelExportService;
        private readonly IMessageBoxService _messageBoxService;

        #region Observable Properties

        [ObservableProperty]
        private ObservableCollection<Company> _companies = new();

        [ObservableProperty]
        private Company? _selectedCompany;

        [ObservableProperty]
        private ObservableCollection<MonthItem> _months = new();

        [ObservableProperty]
        private ObservableCollection<int> _years = new();

        [ObservableProperty]
        private MonthItem? _selectedFromMonth;

        [ObservableProperty]
        private MonthItem? _selectedToMonth;

        [ObservableProperty]
        private int _selectedFromYear;

        [ObservableProperty]
        private int _selectedToYear;

        [ObservableProperty]
        private string _journalNo = string.Empty;

        [ObservableProperty]
        private string _accountCode = string.Empty;

        [ObservableProperty]
        private ObservableCollection<GLTransaction> _transactions = new();

        [ObservableProperty]
        private ObservableCollection<GLTransaction> _rawTransactions = new();

        [ObservableProperty]
        private string _statusMessage = "Initializing...";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
        [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
        [NotifyCanExecuteChangedFor(nameof(ExportRawCommand))]
        private bool _isConnected;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
        [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
        [NotifyCanExecuteChangedFor(nameof(ExportRawCommand))]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isDarkMode;

        #endregion

        public GLListingViewModel(
            IGLListingDA glListingDA,
            ICompanyDA companyDA,
            IGLTransactionService glTransactionService,
            IThemeService themeService,
            IExcelExportService excelExportService,
            IMessageBoxService messageBoxService)
        {
            _glListingDA = glListingDA;
            _companyDA = companyDA;
            _glTransactionService = glTransactionService;
            _themeService = themeService;
            _excelExportService = excelExportService;
            _messageBoxService = messageBoxService;

            // Initialize data
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                StatusMessage = "Connecting to wiz...";

                // Check connection
                bool isConnected = await ApplicationConfiguration.CheckDatabaseConnectionAsync();
                IsConnected = isConnected;

                if (isConnected)
                {
                    StatusMessage = "Successfully connected to wiz";
                    await LoadCompaniesAsync();
                    LoadMonths();
                    LoadYears();
                }
                else
                {
                    StatusMessage = "Wiz connection failed";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Initialization error: {ex.Message}";
                _messageBoxService.ShowError($"{ex.Message}", "Error");
            }
        }

        private async Task LoadCompaniesAsync()
        {
            try
            {
                var companyList = await _companyDA.GetCompanyAsync();
                
                Companies.Clear();
                Companies.Add(new Company { Code = "", Name = "-- Select Company --" });
                
                foreach (var company in companyList)
                {
                    Companies.Add(company);
                }

                SelectedCompany = Companies.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowError($"{ex.Message}", "Error");
            }
        }

        private void LoadMonths()
        {
            Months.Clear();
            for (int i = 1; i <= 12; i++)
            {
                Months.Add(new MonthItem { Value = i, Text = i.ToString("D2") });
            }

            SelectedFromMonth = Months.FirstOrDefault(m => m.Value == DateTime.Now.Month);
            SelectedToMonth = Months.FirstOrDefault(m => m.Value == DateTime.Now.Month);
        }

        private void LoadYears()
        {
            Years.Clear();
            for (int i = DateTime.Now.Year - 15; i <= DateTime.Now.Year + 15; i++)
            {
                Years.Add(i);
            }

            SelectedFromYear = DateTime.Now.Year;
            SelectedToYear = DateTime.Now.Year;
        }

        private List<GLTransaction> TransformGLData(IEnumerable<GLTransaction> source)
        {
            var result = new List<GLTransaction>();

            // 1. Gom nhóm theo Company, Month, JournalNumber để xử lý từng bút toán
            var groupedTransactions = source.GroupBy(t => new { t.CompanyCode, t.PostMonth, t.JournalNumber });

            foreach (var group in groupedTransactions)
            {
                var transactions = group.ToList();

                // Tách riêng các dòng Nợ (Debit > 0) và Có (Credit < 0)
                // Lưu ý: Logic này giả định dòng Nợ có Debit > 0, dòng Có có Credit < 0 (như trong file excel của bạn)
                var debits = transactions.Where(t => t.Debit > 0).ToList();
                var credits = transactions.Where(t => t.Credit < 0).ToList();

                // TRƯỜNG HỢP 1: 1 dòng Có đối ứng nhiều dòng Nợ (Giống file before.xlsx của bạn)
                if (debits.Count > 0 && credits.Count == 1)
                {
                    var creditRow = credits.First();
                    foreach (var debitRow in debits)
                    {
                        // A. Giữ nguyên dòng Nợ, cập nhật TK đối ứng
                        debitRow.CorrespondingAccount = creditRow.VASAccount;
                        result.Add(debitRow);

                        // B. Tạo ra dòng Có mới (tách từ dòng Có gốc) để khớp với dòng Nợ này
                        var newCreditRow = CloneTransaction(creditRow);
                        newCreditRow.Debit = 0;
                        newCreditRow.Credit = -debitRow.Debit; // Gán giá trị Credit bằng đúng số âm của Debit
                        newCreditRow.Total = newCreditRow.Credit;
                        newCreditRow.CorrespondingAccount = debitRow.VASAccount; // TK đối ứng là TK Nợ
                        
                        result.Add(newCreditRow);
                    }
                }
                // TRƯỜNG HỢP 2: 1 dòng Nợ đối ứng nhiều dòng Có (Ngược lại)
                else if (credits.Count > 0 && debits.Count == 1)
                {
                    var debitRow = debits.First();
                    foreach (var creditRow in credits)
                    {
                        // A. Tạo dòng Nợ mới để khớp với dòng Có này
                        var newDebitRow = CloneTransaction(debitRow);
                        newDebitRow.Debit = -creditRow.Credit; // Gán giá trị Debit bằng số dương của Credit
                        newDebitRow.Credit = 0;
                        newDebitRow.Total = newDebitRow.Debit;
                        newDebitRow.CorrespondingAccount = creditRow.VASAccount;
                        
                        result.Add(newDebitRow);

                        // B. Giữ nguyên dòng Có, cập nhật TK đối ứng
                        creditRow.CorrespondingAccount = debitRow.VASAccount;
                        result.Add(creditRow);
                    }
                }
                // TRƯỜNG HỢP 3: 1-1 hoặc Nhiều-Nhiều (Giữ nguyên hoặc xử lý đơn giản)
                else
                {
                    // Nếu 1-1 thì gán chéo TK đối ứng
                    if (debits.Count == 1 && credits.Count == 1)
                    {
                        debits[0].CorrespondingAccount = credits[0].VASAccount;
                        credits[0].CorrespondingAccount = debits[0].VASAccount;
                    }
                    
                    // Thêm tất cả vào danh sách (không tách dòng vì không biết tỉ lệ)
                    result.AddRange(transactions);
                }
            }

            return result;
        }

        // Hàm hỗ trợ copy object (Deep Clone)
        private GLTransaction CloneTransaction(GLTransaction original)
        {
            // Sử dụng JSON Serialization để copy toàn bộ dữ liệu sang object mới
            // Đảm bảo bạn đã cài gói Newtonsoft.Json hoặc dùng System.Text.Json
            var json = JsonConvert.SerializeObject(original);
            return JsonConvert.DeserializeObject<GLTransaction>(json);
        }

        [RelayCommand(CanExecute = nameof(CanSearch))]
        private async Task SearchAsync()
        {
            IsLoading = true;
            StatusMessage = "Searching...";

            try
            {
                string companyCode = SelectedCompany?.Code ?? "";
                string fromPostMonth = $"{SelectedFromYear}{SelectedFromMonth?.Text}";
                string toPostMonth = $"{SelectedToYear}{SelectedToMonth?.Text}";

                // 1. Lấy dữ liệu thô
                var rawData = await _glListingDA.SearchForGLListingAsync(
                    companyCode,
                    JournalNo,
                    AccountCode,
                    fromPostMonth,
                    toPostMonth);

                RawTransactions.Clear();
                Transactions.Clear();

                // Gán dữ liệu thô vào List thứ 2 trước khi Transform
                int rawIndex = 1;
                foreach (var item in rawData)
                {
                    // Clone object để tránh tham chiếu 2 lưới bị dính nhau nếu có sửa đổi
                    var rawItem = CloneTransaction(item); 
                    rawItem.STT = rawIndex++;
                    // Xử lý xóa xuống dòng trong Description
                    if (!string.IsNullOrEmpty(rawItem.Description))
                    {
                        // Thay thế ký tự xuống dòng (\r và \n) bằng khoảng trắng
                        rawItem.Description = rawItem.Description.Replace("\r\n", " ")
                                                                 .Replace("\n", " ")
                                                                 .Replace("\r", " ");
                    }
                    RawTransactions.Add(rawItem);
                }

                // 2. Transform dữ liệu: Tách dòng và xử lý TK đối ứng
                // Hàm này thay thế cho _glTransactionService.PopulateCorrespondingAccounts(data);
                var transformedData = TransformGLData(rawData);

                // 3. Hiển thị dữ liệu đã transform
                int index = 1;
                foreach (var item in transformedData)
                {
                    item.STT = index++; // Gán số thứ tự thủ công
                    // Xử lý xóa xuống dòng trong Description
                    if (!string.IsNullOrEmpty(item.Description))
                    {
                        // Thay thế ký tự xuống dòng (\r và \n) bằng khoảng trắng
                        item.Description = item.Description.Replace("\r\n", " ")
                                                           .Replace("\n", " ")
                                                           .Replace("\r", " ");
                    }
                    Transactions.Add(item);
                }

                StatusMessage = $"Found {RawTransactions.Count} records (GL Listing), transformed to {Transactions.Count} records (VAS).";
                
                // Notify that Export command can execute state may have changed
                ExportCommand.NotifyCanExecuteChanged();
                ExportRawCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                StatusMessage = "Search failed.";
                _messageBoxService.ShowError($"{ex.Message}", "Search Error");
            }
            finally
            {
                IsLoading = false;
                SearchCommand.NotifyCanExecuteChanged();
                ExportCommand.NotifyCanExecuteChanged();
                ExportRawCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanSearch() => IsConnected && !IsLoading;

        [RelayCommand(CanExecute = nameof(CanExport))]
        private async Task ExportAsync()
        {
            if (Transactions.Count == 0)
            {
                _messageBoxService.ShowInformation("No data to export.", "Information");
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = "GL Listing - VAS.xlsx",
                Title = "Save as",
                RestoreDirectory = true
            };

            if (saveFileDialog.ShowDialog() != true)
                return;

            IsLoading = true;
            StatusMessage = "Exporting...";
            // SearchCommand.NotifyCanExecuteChanged();
            // ExportCommand.NotifyCanExecuteChanged();
            // ExportRawCommand.NotifyCanExecuteChanged();

            try
            {
                string filePath = saveFileDialog.FileName;
                
                await Task.Run(() =>
                {
                    // Create export options
                    var options = new ExcelExportOptions
                    {
                        EnableAutoFilter = true,        // Keep filter
                        RemoveTableFormat = true,       // Remove table format
                        FreezeHeaderRow = true,         // Freeze header
                        BoldHeader = true,              // Bold header
                        HeaderBackgroundColor = XLColor.LightGray, // Light gray background
                        AutoFitColumns = true,
                        MinColumnWidth = 8,
                        MaxColumnWidth = 60,
                        FormatNumbers = true,           // Format Debit/Credit/Total
                        NumberFormat = "#,##0_);(#,##0)"       // Number format
                    };

                    // Use service to export
                    _excelExportService.ExportVASTransactions(
                        Transactions.ToList(), 
                        filePath, 
                        options);
                });

                StatusMessage = "Export completed.";
                _messageBoxService.ShowSuccess("File exported successfully!", "Export Completed");
                
                // Open file
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                StatusMessage = "Export failed.";
                _messageBoxService.ShowError($"{ex.Message}", "Export Error");
            }
            finally
            {
                IsLoading = false;
                SearchCommand.NotifyCanExecuteChanged();
                ExportCommand.NotifyCanExecuteChanged();
                ExportRawCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand(CanExecute = nameof(CanExportRaw))]
        private async Task ExportRawAsync()
        {
            if (RawTransactions.Count == 0)
            {
                _messageBoxService.ShowInformation("No data to export.", "Information");
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = "GL Listing - Raw.xlsx",
                Title = "Save as",
                RestoreDirectory = true
            };

            if (saveFileDialog.ShowDialog() != true)
                return;

            IsLoading = true;
            StatusMessage = "Exporting...";
            SearchCommand.NotifyCanExecuteChanged();
            ExportCommand.NotifyCanExecuteChanged();
            ExportRawCommand.NotifyCanExecuteChanged();

            try
            {
                string filePath = saveFileDialog.FileName;
                
                await Task.Run(() =>
                {
                    // Create export options
                    var options = new ExcelExportOptions
                    {
                        EnableAutoFilter = true,        // Keep filter
                        RemoveTableFormat = true,       // Remove table format
                        FreezeHeaderRow = true,         // Freeze header
                        BoldHeader = true,              // Bold header
                        HeaderBackgroundColor = XLColor.LightGray, // Light gray background
                        AutoFitColumns = true,
                        MinColumnWidth = 8,
                        MaxColumnWidth = 60,
                        FormatNumbers = true,           // Format Debit/Credit/Total
                        NumberFormat = "#,##0_);(#,##0)"       // Number format
                    };

                    // Use service to export
                    _excelExportService.ExportGLTransactions(
                        RawTransactions.ToList(), 
                        filePath, 
                        options);
                });

                StatusMessage = "Export completed.";
                _messageBoxService.ShowSuccess("File exported successfully!", "Export Completed");
                
                // Open file
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                StatusMessage = "Export failed.";
                _messageBoxService.ShowError($"{ex.Message}", "Export Error");
            }
            finally
            {
                IsLoading = false;
                SearchCommand.NotifyCanExecuteChanged();
                ExportCommand.NotifyCanExecuteChanged();
                ExportRawCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanExport() => IsConnected && !IsLoading && Transactions.Count > 0;

        private bool CanExportRaw() => IsConnected && !IsLoading && RawTransactions.Count > 0;

        [RelayCommand]
        private void ToggleTheme()
        {
            // Toggle the boolean first
            IsDarkMode = !IsDarkMode;
            
            // Then apply to theme service
            _themeService.SetDarkMode(IsDarkMode);
        }
        
        // Helper method to be called when IsDarkMode changes externally
        partial void OnIsDarkModeChanged(bool value)
        {
            _themeService.SetDarkMode(value);
        }
    }

    // Helper class
    public class MonthItem
    {
        public int Value { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}