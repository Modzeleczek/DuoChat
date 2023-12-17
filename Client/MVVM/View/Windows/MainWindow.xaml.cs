using Client.MVVM.ViewModel;
using Shared.MVVM.Core;
using Shared.MVVM.View.Windows;
using System.Runtime.InteropServices;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace Client.MVVM.View.Windows
{
    public partial class MainWindow : DialogWindow
    {
        private bool serversAccountsVisible = false;

        public MainWindow(Window owner, MainViewModel dataContext)
            : base(owner, dataContext)
        {
            InitializeComponent();
            InitializeColumns();
        }

        private void Button_ToggleServersAccounts_Click(object sender, RoutedEventArgs e)
        {
            if (serversAccountsVisible)
                HideColumns();
            else
                ShowColumns();
            serversAccountsVisible = !serversAccountsVisible;
        }

        private void InitializeColumns()
        {
            SetWidth(ref ConversationsColumn, 0.16);
            HideColumns();
        }

        private void HideColumns()
        {
            SetWidth(ref ServersColumn, 0);
            SetWidth(ref AccountsColumn, 0);
            SetWidth(ref ChatColumn, 0.84);
            ToggleServersAccountsButton.Content = ">";
        }

        private void ShowColumns()
        {
            SetWidth(ref ServersColumn, 0.16);
            SetWidth(ref AccountsColumn, 0.16);
            SetWidth(ref ChatColumn, 0.52);
            ToggleServersAccountsButton.Content = "<";
        }

        private void SetWidth(ref ColumnDefinition column, double value)
        {
            column.Width = new GridLength(value, GridUnitType.Star);
        }

        private void Button_Minimize_Click(object sender, RoutedEventArgs e)
        {
            Minimize();
        }

        private void Button_Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState != WindowState.Maximized)
            {
                // Chowamy Grida do rozciągania okna.
                ResizableGrid.Visibility = Visibility.Hidden;
                Maximize();
            }
            else
            {
                // Przywracamy Grida do rozciągania okna.
                ResizableGrid.Visibility = Visibility.Visible;
                Restore();
            }
        }

        #region Maxi- and minimization
        // https://stackoverflow.com/a/67228878

        // Rectangle (used by MONITORINFOEX)
        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // Monitor information (used by GetMonitorInfo())
        [StructLayout(LayoutKind.Sequential)]
        private class MonitorInfoEx
        {
            public int cbSize;
            public Rect rcMonitor; // Total area
            public Rect rcWork; // Working area
            public int dwFlags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
            public char[] szDevice = null!;
        }

        // To get a handle to the specified monitor
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

        // To get the working area of the specified monitor
        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(HandleRef hmonitor, [In, Out] MonitorInfoEx monitorInfo);

        private static MonitorInfoEx GetMonitorInfo(Window window, IntPtr monitorPtr)
        {
            var monitorInfo = new MonitorInfoEx();

            monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
            GetMonitorInfo(new HandleRef(window, monitorPtr), monitorInfo);

            return monitorInfo;
        }

        private void Minimize()
        {
            WindowState = WindowState.Minimized;
        }

        private void Restore()
        {
            WindowState = WindowState.Normal;
            ResizeMode = ResizeMode.CanResizeWithGrip;
        }

        private void Maximize()
        {
            ResizeMode = ResizeMode.NoResize;

            // Get handle for nearest monitor to this window
            var wih = new WindowInteropHelper(this);

            // Nearest monitor to window
            const int MONITOR_DEFAULTTONEAREST = 2;
            var hMonitor = MonitorFromWindow(wih.Handle, MONITOR_DEFAULTTONEAREST);

            // Get monitor info
            var monitorInfo = GetMonitorInfo(this, hMonitor);

            // Create working area dimensions, converted to DPI-independent values
            var source = HwndSource.FromHwnd(wih.Handle);

            if (source?.CompositionTarget == null)
            {
                return;
            }

            var matrix = source.CompositionTarget.TransformFromDevice;
            var workingArea = monitorInfo.rcWork;

            var dpiIndependentSize =
                matrix.Transform(
                    new Point(workingArea.Right - workingArea.Left,
                              workingArea.Bottom - workingArea.Top));

            // Maximize the window to the device-independent working area ie
            // the area without the taskbar.
            Top = workingArea.Top;
            Left = workingArea.Left;

            MaxWidth = dpiIndependentSize.X;
            MaxHeight = dpiIndependentSize.Y;

            WindowState = WindowState.Maximized;
        }
        #endregion

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /* potrzebne, aby po nieudanej próbie połączenia z serwerem,
            kliknięte konto na liście kont zostało odznaczone */
            var listView = (ListView)sender;
            if (listView.SelectedItem == null)
                listView.UnselectAll();
        }
    }
}
