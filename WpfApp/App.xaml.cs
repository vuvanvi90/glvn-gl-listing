using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows;
using DataAccess;
using WpfApp.ViewModels;
using WpfApp.Views;
using WpfApp.Services;
using MaterialDesignThemes.Wpf;

namespace WpfApp
{
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .Build();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register DataAccess services
            services.AddTransient<ICompanyDA, CompanyDA>();
            services.AddTransient<IGLListingDA, GLListingDA>();
            services.AddTransient<IPFEHelperDA, PFEHelperDA>();
            services.AddTransient<IGLTransactionService, GLTransactionService>();

            // Register Application services
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IExcelExportService, ExcelExportService>();
            services.AddSingleton<IMessageBoxService, CustomMessageBoxService>();

            services.AddSingleton<IAppAccessService, GoogleSheetsAccessService>();
            services.AddHostedService<AccessMonitorService>();

            // Register ViewModels
            services.AddTransient<GLListingViewModel>();

            // Register Views
            services.AddTransient<GLListingView>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            try
            {
                var accessService = _host.Services.GetRequiredService<IAppAccessService>();
                bool hasAccess = await accessService.CheckAccessAsync();

                if (!hasAccess)
                {
                    MessageBox.Show("Không thể kết nối WIZ, Vui lòng liên hệ Vị Vũ DDU để yêu cầu hỗ trợ.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Stop);
                    Shutdown(); // Tắt app
                    return;
                }

                // Gọi lại logic kiểm tra kết nối
                // Đảm bảo bạn đã reference namespace chứa ApplicationConfiguration
                bool isConnected = await DataAccess.ApplicationConfiguration.CheckDatabaseConnectionAsync();

                if (!isConnected)
                {
                    MessageBox.Show("Wiz connection failed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown(); // Tắt app nếu không kết nối được
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization error: {ex.Message}");
                return;
            }

            var mainWindow = _host.Services.GetRequiredService<GLListingView>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync();
            }

            base.OnExit(e);
        }
    }
}