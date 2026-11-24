// Settings sections for background, shape, border and the extras on the background (v1.4.6).
using System;
using System.Windows.Controls;

namespace BetterClock
{
    public partial class SettingsWindow
    {
        private void BuildShapeSections(StackPanel page, ModeStyle st, OverlayMode mode)
        {
            // ---- background ----
            var bg = new AnimatedSection("Background", false);
            AddEnumCombo(bg.Body, st, "Style", "BackgroundStyle", typeof(BackgroundStyle), null);
            AddColor(bg.Body, st, "Colour", "BackgroundColor", null);
            AddColor(bg.Body, st, "Second colour (gradient)", "BackgroundColor2", null);
            AddColor(bg.Body, st, "Middle colour (optional)", "BackgroundColor3", null);
            bg.Body.Children.Add(Hint("Leave the middle colour empty for a two-colour gradient. Fill it in to get three colours."));
            AddSlider(bg.Body, st, "Gradient angle", "GradientAngle", 0, 360, 5, " deg", null);
            AddSlider(bg.Body, st, "Opacity", "BackgroundOpacity", 0, 100, 5, " %", null);
            bg.Body.Children.Add(Hint("Style: Solid, Gradient (straight), Radial (round, from the centre) or None."));
            AddCopySection(bg, mode);
            page.Children.Add(bg);

            // ---- shape and corners ----
            var shape = new AnimatedSection("Shape and corners", false);
            AddEnumCombo(shape.Body, st, "Shape", "Shape", typeof(PillShape), null);
            shape.Body.Children.Add(Hint("Rounded is the normal box (make it a pill with a big roundness). Circle, Hexagon and Slanted are other shapes."));
            AddSlider(shape.Body, st, "Corner roundness", "CornerRadius", 0, 200, 1, " px", null);
            AddCheck(shape.Body, st, "Each corner on its own", "PerCorner", null);
            AddSlider(shape.Body, st, "Top left", "CornerTL", 0, 200, 1, " px", null);
            AddSlider(shape.Body, st, "Top right", "CornerTR", 0, 200, 1, " px", null);
            AddSlider(shape.Body, st, "Bottom right", "CornerBR", 0, 200, 1, " px", null);
            AddSlider(shape.Body, st, "Bottom left", "CornerBL", 0, 200, 1, " px", null);
            AddNumberBox(shape.Body, st, "Fixed width (0 = automatic)", "FixedWidth", 0, 3000, "px");
            AddNumberBox(shape.Body, st, "Fixed height (0 = automatic)", "FixedHeight", 0, 3000, "px");
            AddNumberBox(shape.Body, st, "Never narrower than", "MinWidthPx", 0, 3000, "px");
            AddSlider(shape.Body, st, "Padding left / right", "PaddingH", 0, 120, 1, " px", null);
            AddSlider(shape.Body, st, "Padding top / bottom", "PaddingV", 0, 80, 1, " px", null);
            AddCopySection(shape, mode);
            page.Children.Add(shape);

            // ---- border ----
            var border = new AnimatedSection("Border", false);
            AddColor(border.Body, st, "Colour", "BorderColor", null);
            AddSlider(border.Body, st, "Thickness", "BorderThickness", 0, 20, 1, " px", null);
            AddEnumCombo(border.Body, st, "Line", "BorderLineStyle", typeof(BorderLine), null);
            AddSlider(border.Body, st, "Opacity", "BorderOpacity", 0, 100, 5, " %", null);
            AddCheck(border.Body, st, "Gradient border", "BorderGradient", null);
            AddColor(border.Body, st, "Gradient end colour", "BorderColor2", null);
            AddSlider(border.Body, st, "Gradient angle", "BorderGradientAngle", 0, 360, 15, " deg", null);
            AddCopySection(border, mode);
            page.Children.Add(border);

            // ---- glow and shadow of the whole pill ----
            var glow = new AnimatedSection("Glow and shadow of the pill", false);
            AddCheck(glow.Body, st, "Glow / shadow around it", "ShadowEnabled", null);
            AddSlider(glow.Body, st, "Glow size", "ShadowBlur", 0, 60, 1, " px", null);
            AddColor(glow.Body, st, "Colour", "ShadowColor", null);
            AddSlider(glow.Body, st, "Move left / right", "ShadowOffsetX", -40, 40, 1, " px", null);
            AddSlider(glow.Body, st, "Move up / down", "ShadowOffsetY", -40, 40, 1, " px", null);
            AddSlider(glow.Body, st, "Opacity", "ShadowOpacity", 0, 100, 5, " %", null);
            AddCopySection(glow, mode);
            page.Children.Add(glow);

            // ---- extras on the background ----
            var extras = new AnimatedSection("Inner glow, glass and texture", false);
            AddSlider(extras.Body, st, "Inner glow size", "InnerGlowSize", 0, 100, 1, " %", null);
            AddColor(extras.Body, st, "Inner glow colour", "InnerGlowColor", null);
            AddSlider(extras.Body, st, "Inner glow opacity", "InnerGlowOpacity", 0, 100, 5, " %", null);
            AddCheck(extras.Body, st, "Glass shine", "Glass", null);
            AddSlider(extras.Body, st, "Glass strength", "GlassStrength", 0, 100, 5, " %", null);
            AddEnumCombo(extras.Body, st, "Texture", "Texture", typeof(TextureStyle), null);
            AddColor(extras.Body, st, "Texture colour", "TextureColor", null);
            AddSlider(extras.Body, st, "Texture opacity", "TextureOpacity", 0, 100, 5, " %", null);
            AddSlider(extras.Body, st, "Texture size", "TextureSize", 3, 40, 1, " px", null);
            extras.Body.Children.Add(Hint("Glass is a shine on the top half. It is not a real see-through blur, because Windows does not let an overlay blur what is behind it."));
            AddCopySection(extras, mode);
            page.Children.Add(extras);

            // ---- picture ----
            var pic = new AnimatedSection("Picture behind the numbers", false);
            AddFilePicker(pic.Body, st, "Picture", "BackgroundImagePath", "Pictures|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*");
            AddSlider(pic.Body, st, "Picture opacity", "BackgroundImageOpacity", 0, 100, 5, " %", null);
            AddEnumCombo(pic.Body, st, "Picture fit", "BackgroundImageFit", typeof(ImageFit), null);
            AddSlider(pic.Body, st, "Zoom", "BackgroundImageZoom", 50, 400, 5, " %", null);
            AddSlider(pic.Body, st, "Move left / right", "BackgroundImageX", -100, 100, 1, " %", null);
            AddSlider(pic.Body, st, "Move up / down", "BackgroundImageY", -100, 100, 1, " %", null);
            AddColor(pic.Body, st, "Tint colour", "BackgroundImageTint", null);
            AddSlider(pic.Body, st, "Tint strength", "BackgroundImageTintAmount", 0, 100, 5, " %", null);
            pic.Body.Children.Add(Hint("Cover fills the whole shape, Fit shows all of it, Stretch squeezes it. The tint puts a colour over the picture (leave it empty for none)."));
            AddCopySection(pic, mode);
            page.Children.Add(pic);
        }
    }
}
