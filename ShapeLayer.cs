// One layer of the pill: draws a shape (rounded box, circle, hexagon or slanted box)
// with a fill and a border (solid, dashed, dotted or double). Used for the background
// extras in v1.4.6: picture, texture, inner glow, glass and the custom shapes.
using System;
using System.Windows;
using System.Windows.Media;

namespace BetterClock
{
    public class ShapeLayer : FrameworkElement
    {
        private PillShape _kind = PillShape.Rounded;
        private double _tl, _tr, _br, _bl;
        private double _inset;
        private Brush _fill;
        private Brush _stroke;
        private double _thickness;
        private BorderLine _line = BorderLine.Solid;

        public ShapeLayer()
        {
            IsHitTestVisible = false;
        }

        public void Set(PillShape kind, double tl, double tr, double br, double bl, double inset,
                        Brush fill, Brush stroke, double thickness, BorderLine line)
        {
            _kind = kind;
            _tl = tl; _tr = tr; _br = br; _bl = bl;
            _inset = inset;
            _fill = fill;
            _stroke = stroke;
            _thickness = thickness;
            _line = line;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth;
            double h = ActualHeight;
            if (w < 2 || h < 2) return;
            Rect box = new Rect(_inset, _inset, w - 2 * _inset, h - 2 * _inset);
            if (box.Width < 2 || box.Height < 2) return;

            if (_fill != null)
            {
                Geometry g = Build(box, 0);
                if (g != null) dc.DrawGeometry(_fill, null, g);
            }

            if (_stroke != null && _thickness > 0.05)
            {
                if (_line == BorderLine.Double && _thickness >= 3)
                {
                    double t = _thickness / 3;
                    dc.DrawGeometry(null, MakePen(t, false), Build(box, t / 2));
                    dc.DrawGeometry(null, MakePen(t, false), Build(box, _thickness - t / 2));
                }
                else
                {
                    Pen pen = MakePen(_thickness, true);
                    dc.DrawGeometry(null, pen, Build(box, _thickness / 2));
                }
            }
        }

        private Pen MakePen(double thickness, bool allowDash)
        {
            var pen = new Pen(_stroke, thickness);
            pen.LineJoin = PenLineJoin.Round;
            if (allowDash && _line == BorderLine.Dashed)
            {
                pen.DashStyle = new DashStyle(new double[] { 3, 2 }, 0);
            }
            else if (allowDash && _line == BorderLine.Dotted)
            {
                pen.DashStyle = new DashStyle(new double[] { 0.01, 2.2 }, 0);
                pen.DashCap = PenLineCap.Round;
            }
            return pen;
        }

        // The outline of the shape, pulled in by "inset" on every side.
        private Geometry Build(Rect outer, double inset)
        {
            Rect r = new Rect(outer.X + inset, outer.Y + inset, Math.Max(1, outer.Width - 2 * inset), Math.Max(1, outer.Height - 2 * inset));
            if (_kind == PillShape.Hexagon)
            {
                double s = Math.Min(r.Height / 2, r.Width / 3);
                return Polygon(new Point[]
                {
                    new Point(r.Left + s, r.Top), new Point(r.Right - s, r.Top), new Point(r.Right, r.Top + r.Height / 2),
                    new Point(r.Right - s, r.Bottom), new Point(r.Left + s, r.Bottom), new Point(r.Left, r.Top + r.Height / 2)
                });
            }
            if (_kind == PillShape.Slanted)
            {
                double s = Math.Min(r.Height * 0.35, r.Width / 3);
                return Polygon(new Point[]
                {
                    new Point(r.Left + s, r.Top), new Point(r.Right, r.Top),
                    new Point(r.Right - s, r.Bottom), new Point(r.Left, r.Bottom)
                });
            }
            if (_kind == PillShape.Circle)
            {
                double d = Math.Min(r.Width, r.Height) / 2;
                return RoundRect(r, d, d, d, d);
            }
            return RoundRect(r, Math.Max(0, _tl - inset), Math.Max(0, _tr - inset), Math.Max(0, _br - inset), Math.Max(0, _bl - inset));
        }

        private static Geometry Polygon(Point[] pts)
        {
            var g = new StreamGeometry();
            using (StreamGeometryContext c = g.Open())
            {
                c.BeginFigure(pts[0], true, true);
                for (int i = 1; i < pts.Length; i++) c.LineTo(pts[i], true, true);
            }
            g.Freeze();
            return g;
        }

        private static Geometry RoundRect(Rect r, double tl, double tr, double br, double bl)
        {
            double max = Math.Min(r.Width, r.Height) / 2;
            tl = Math.Min(tl, max);
            tr = Math.Min(tr, max);
            br = Math.Min(br, max);
            bl = Math.Min(bl, max);
            var g = new StreamGeometry();
            using (StreamGeometryContext c = g.Open())
            {
                c.BeginFigure(new Point(r.Left + tl, r.Top), true, true);
                c.LineTo(new Point(r.Right - tr, r.Top), true, false);
                if (tr > 0.01) c.ArcTo(new Point(r.Right, r.Top + tr), new Size(tr, tr), 0, false, SweepDirection.Clockwise, true, false);
                c.LineTo(new Point(r.Right, r.Bottom - br), true, false);
                if (br > 0.01) c.ArcTo(new Point(r.Right - br, r.Bottom), new Size(br, br), 0, false, SweepDirection.Clockwise, true, false);
                c.LineTo(new Point(r.Left + bl, r.Bottom), true, false);
                if (bl > 0.01) c.ArcTo(new Point(r.Left, r.Bottom - bl), new Size(bl, bl), 0, false, SweepDirection.Clockwise, true, false);
                c.LineTo(new Point(r.Left, r.Top + tl), true, false);
                if (tl > 0.01) c.ArcTo(new Point(r.Left + tl, r.Top), new Size(tl, tl), 0, false, SweepDirection.Clockwise, true, false);
            }
            g.Freeze();
            return g;
        }
    }
}
