using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp.Services
{
    public class AccessMonitorService : BackgroundService
    {
        private readonly IAppAccessService _accessService;
        private int _failureCount = 0; // Biến đếm số lần check thất bại
        private const int MaxFailures = 3; // Số lần thất bại tối đa cho phép

        public AccessMonitorService(IAppAccessService accessService)
        {
            _accessService = accessService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Đợi 1 phút sau khi mở app rồi mới bắt đầu check lần đầu
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                bool hasAccess = await _accessService.CheckAccessAsync();

                if (!hasAccess)
                {
                    _failureCount++; // Tăng biến đếm nếu không có quyền hoặc rớt mạng

                    if (_failureCount >= MaxFailures)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // 1. Ẩn màn hình chính ngay lập tức để chặn mọi thao tác của người dùng
                            if (Application.Current.MainWindow != null)
                            {
                                Application.Current.MainWindow.Hide();
                            }

                            // 2. Hiện thông báo (Code sẽ tạm dừng ở đây chờ người dùng bấm OK)
                            MessageBox.Show("Không thể kết nối WIZ, Vui lòng liên hệ Vị Vũ DDU để yêu cầu hỗ trợ.",
                                            "Access Revoked",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Warning);

                            // 3. Ép buộc "giết" toàn bộ tiến trình ứng dụng ngay lập tức
                            Environment.Exit(0);
                        });

                        break; // Thoát vòng lặp
                    }
                }
                else
                {
                    // Nếu check thành công, reset lại biến đếm về 0
                    _failureCount = 0;
                }

                // Chờ 1 phút trước khi check lại lần tiếp theo (Có thể đổi thành 5 phút khi mang vào sử dụng thực tế)
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}