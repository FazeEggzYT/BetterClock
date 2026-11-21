// Draws the Better Clock icon in code (used for the tray and the settings window).
// The .exe file icon comes from assets\icon.ico at build time.
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;
using Drawing2D = System.Drawing.Drawing2D;

namespace BetterClock
{
    public static class IconFactory
    {
        private static Drawing.Icon _icon;

        public static Drawing.Icon GetIcon()
        {
            if (_icon != null) return _icon;

            const int size = 32;
            using (var bmp = new Drawing.Bitmap(size, size))
            {
                using (Drawing.Graphics g = Drawing.Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias;
                    g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    g.Clear(Drawing.Color.Transparent);

                    var rect = new Drawing.Rectangle(1, 1, size - 2, size - 2);
                    using (var path = RoundedRect(rect, 8))
                    using (var brush = new Drawing2D.LinearGradientBrush(rect,
                        Drawing.Color.FromArgb(77, 124, 255), Drawing.Color.FromArgb(168, 92, 255), 45f))
                    {
                        g.FillPath(brush, path);
                    }

                    using (var font = new Drawing.Font("Segoe UI", 19, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel))
                    using (var sf = new Drawing.StringFormat())
                    {
                        sf.Alignment = Drawing.StringAlignment.Center;
                        sf.LineAlignment = Drawing.StringAlignment.Center;
                        g.DrawString("B", font, Drawing.Brushes.White, new Drawing.RectangleF(0, 0, size, size + 1), sf);
                    }

                    // little clock dot, bottom right
                    g.FillEllipse(new Drawing.SolidBrush(Drawing.Color.FromArgb(20, 20, 34)), 18, 18, 13, 13);
                    using (var pen = new Drawing.Pen(Drawing.Color.White, 1.6f))
                    {
                        g.DrawEllipse(pen, 18, 18, 13, 13);
                        g.DrawLine(pen, 24.5f, 24.5f, 24.5f, 20.5f);
                        g.DrawLine(pen, 24.5f, 24.5f, 27.5f, 24.5f);
                    }
                }
                _icon = Drawing.Icon.FromHandle(bmp.GetHicon());
            }
            return _icon;
        }

        public static BitmapSource GetImageSource()
        {
            try
            {
                return Imaging.CreateBitmapSourceFromHIcon(GetIcon().Handle, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Drawing2D.GraphicsPath RoundedRect(Drawing.Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new Drawing2D.GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
