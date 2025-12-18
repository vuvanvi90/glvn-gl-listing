using MaterialDesignThemes.Wpf;
using System.Windows;

namespace WpfApp.Services
{
    public interface IThemeService
    {
        void SetDarkMode(bool isDark);
        bool IsDarkMode { get; }
    }

    public class ThemeService : IThemeService
    {
        private readonly PaletteHelper _paletteHelper;

        public bool IsDarkMode { get; private set; }

        public ThemeService()
        {
            _paletteHelper = new PaletteHelper();
        }

        public void SetDarkMode(bool isDark)
        {
            IsDarkMode = isDark;
            ITheme theme = _paletteHelper.GetTheme();
            theme.SetBaseTheme(isDark ? Theme.Dark : Theme.Light);
            _paletteHelper.SetTheme(theme);
        }
    }
}