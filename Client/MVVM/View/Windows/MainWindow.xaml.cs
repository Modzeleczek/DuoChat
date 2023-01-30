﻿using Client.MVVM.ViewModel;
using Shared.MVVM.View.Windows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace Client.MVVM.View.Windows
{
    public partial class MainWindow : DialogWindow
    {
        private bool serversAccountsVisible = false;

        public MainWindow(Window owner, MainViewModel dataContext)
            : base(owner, dataContext) { }

        protected override void Initialize() => InitializeComponent();

        private void Button_ToggleServersAccounts_Click(object sender, RoutedEventArgs e)
        {
            if (serversAccountsVisible)
            {
                ServersColumn.Width = new GridLength(0, GridUnitType.Pixel);
                AccountsColumn.Width = new GridLength(0, GridUnitType.Pixel);
                ToggleServersAccountsButton.Content = ">";
            }
            else
            {
                ServersColumn.Width = new GridLength(200, GridUnitType.Pixel);
                AccountsColumn.Width = new GridLength(200, GridUnitType.Pixel);
                ToggleServersAccountsButton.Content = "<";
            }
            serversAccountsVisible = !serversAccountsVisible;
        }

        private void Button_Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Button_Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState != WindowState.Maximized)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }

        #region ResizeWindow
        bool ResizeInProcess = false;
        private void Resize_Init(object sender, MouseButtonEventArgs e)
        {
            Rectangle senderRect = sender as Rectangle;
            if (senderRect != null)
            {
                ResizeInProcess = true;
                senderRect.CaptureMouse();
            }
        }

        private void Resize_End(object sender, MouseButtonEventArgs e)
        {
            Rectangle senderRect = sender as Rectangle;
            if (senderRect != null)
            {
                ResizeInProcess = false; ;
                senderRect.ReleaseMouseCapture();
            }
        }

        private void Resizing_Form(object sender, MouseEventArgs e)
        {
            if (ResizeInProcess)
            {
                Rectangle senderRect = sender as Rectangle;
                Window mainWindow = senderRect.Tag as Window;
                if (senderRect != null)
                {
                    double width = e.GetPosition(mainWindow).X;
                    double height = e.GetPosition(mainWindow).Y;
                    senderRect.CaptureMouse();
                    if (senderRect.Name.ToLower().Contains("right"))
                    {
                        width += 5;
                        if (width > 0)
                            mainWindow.Width = width;
                    }
                    if (senderRect.Name.ToLower().Contains("left"))
                    {
                        width -= 5;
                        mainWindow.Left += width;
                        width = mainWindow.Width - width;
                        if (width > 0)
                        {
                            mainWindow.Width = width;
                        }
                    }
                    if (senderRect.Name.ToLower().Contains("bottom"))
                    {
                        height += 5;
                        if (height > 0)
                            mainWindow.Height = height;
                    }
                    if (senderRect.Name.ToLower().Contains("top"))
                    {
                        height -= 5;
                        mainWindow.Top += height;
                        height = mainWindow.Height - height;
                        if (height > 0)
                        {
                            mainWindow.Height = height;
                        }
                    }
                }
            }
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
