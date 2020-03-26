using System.Runtime.InteropServices;

namespace Mops.Client
{
    public static class WindowsService
    {
        [DllImport("user32.dll")]
        private static extern int ShowCursor(bool bShow);

        public static void ShowCursor()
        {
            ShowCursor(true);
        }

        public static void HideCursor()
        {
            ShowCursor(false);
        }
    }
}
