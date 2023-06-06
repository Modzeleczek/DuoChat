using Shared.MVVM.Core;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;

namespace Client.MVVM.View.Themes
{
    public partial class ResizableWindow : ResourceDictionary
    {
        #region ResizeWindows
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
                double temp = 0;
                Rectangle senderRect = sender as Rectangle;
                Window mainWindow = senderRect.Tag as Window;
                if (senderRect != null)
                {
                    double width = e.GetPosition(mainWindow).X;
                    double height = e.GetPosition(mainWindow).Y;
                    senderRect.CaptureMouse();
                    if (senderRect.Name.Contains("right", StringComparison.OrdinalIgnoreCase))
                    {
                        width += 5;
                        if (width > 0)
                            mainWindow.Width = width;
                    }
                    if (senderRect.Name.Contains("left", StringComparison.OrdinalIgnoreCase))
                    {
                        width -= 5;
                        temp = mainWindow.Width - width;
                        if ((temp > mainWindow.MinWidth) && (temp < mainWindow.MaxWidth))
                        {
                            mainWindow.Width = temp;
                            mainWindow.Left += width;
                        }
                    }
                    if (senderRect.Name.Contains("bottom", StringComparison.OrdinalIgnoreCase))
                    {
                        height += 5;
                        if (height > 0)
                            mainWindow.Height = height;
                    }
                    if (senderRect.Name.ToLower().Contains("top", StringComparison.OrdinalIgnoreCase))
                    {
                        height -= 5;
                        temp = mainWindow.Height - height;
                        if ((temp > mainWindow.MinHeight) && (temp < mainWindow.MaxHeight))
                        {
                            mainWindow.Height = temp;
                            mainWindow.Top += height;
                        }
                    }
                }
            }
        }
        #endregion
    }
}
