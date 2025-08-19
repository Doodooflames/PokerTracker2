using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PokerTracker2
{
    public static class DialogConstraints
    {
        public static void AddConstrainedDragHandler(Border titleBar, Window dialog)
        {
            titleBar.Cursor = Cursors.Hand;
            titleBar.IsHitTestVisible = true;

            titleBar.MouseLeftButtonDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    // Use WPF's built-in DragMove for smooth, native dragging
                    dialog.DragMove();
                    
                    // Apply constraints after drag completes
                    ConstrainToParentWindow(dialog);
                }
            };
        }

        private static void ConstrainToParentWindow(Window dialog)
        {
            if (dialog.Owner is Window parentWindow)
            {
                // Get parent window bounds
                var parentBounds = new Rect(parentWindow.Left, parentWindow.Top, parentWindow.Width, parentWindow.Height);
                
                // Calculate maximum allowed position to keep dialog within parent bounds
                var maxLeft = parentBounds.Right - dialog.Width;
                var maxTop = parentBounds.Bottom - dialog.Height;
                var minLeft = parentBounds.Left;
                var minTop = parentBounds.Top;
                
                // Constrain the dialog position
                if (dialog.Left < minLeft) dialog.Left = minLeft;
                if (dialog.Left > maxLeft) dialog.Left = maxLeft;
                if (dialog.Top < minTop) dialog.Top = minTop;
                if (dialog.Top > maxTop) dialog.Top = maxTop;
            }
        }
    }
}
