using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

namespace HiddenWindowCaptureSafe
{
    class Program
    {
        // Win32 APIs
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint SWP_HIDEWINDOW = 0x0080;
        const uint SWP_SHOWWINDOW = 0x0040;

        const int SW_RESTORE = 9;
        const int SW_MINIMIZE = 6;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        static void Main(string[] args)
        {
            Console.Write("Enter exact window title: ");
            string windowTitle = Console.ReadLine();

            IntPtr hWnd = FindWindow(null, windowTitle);
            if (hWnd == IntPtr.Zero)
            {
                Console.WriteLine("❌ Window not found.");
                return;
            }

            // Restore window if minimized
            ShowWindow(hWnd, SW_RESTORE);

            // Move off-screen to hide from user
            SetWindowPos(hWnd, IntPtr.Zero, -2000, -2000, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            SetForegroundWindow(hWnd);
            Thread.Sleep(300); // Let it redraw

            // Capture
            if (!GetWindowRect(hWnd, out RECT rect))
            {
                Console.WriteLine("Failed to get window rect.");
                return;
            }

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            using (Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                using (Graphics gfxBmp = Graphics.FromImage(bmp))
                {
                    IntPtr hdcBitmap = gfxBmp.GetHdc();
                    bool success = PrintWindow(hWnd, hdcBitmap, 1);
                    gfxBmp.ReleaseHdc(hdcBitmap);

                    if (success)
                    {
                        string filePath = $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                        bmp.Save(filePath, ImageFormat.Png);
                        Console.WriteLine($"Capture saved: {filePath}");
                    }
                    else
                    {
                        Console.WriteLine("PrintWindow failed. App might be GPU-rendered.");
                    }
                }
            }

            // Optionally hide or minimize again
            //SetWindowPos(hWnd, HWND_BOTTOM, -2000, -2000, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE | SWP_HIDEWINDOW);
            ShowWindow(hWnd, SW_MINIMIZE); // SW_MINIMIZE

        }

    }
}
