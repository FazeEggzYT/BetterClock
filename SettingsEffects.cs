// Settings sections for the text effects (v1.4.5).
using System;
using System.Windows.Controls;

namespace BetterClock
{
    public partial class SettingsWindow
    {
        private void BuildEffectSections(StackPanel page, ModeStyle st, OverlayMode mode)
        {
            // ---- outline and gradient ----
            var look = new AnimatedSection("Outline and gradient", false);
            AddSlider(look.Body, st, "Outline thickness", "OutlineSize", 0, 12, 1, " px", null);
            AddColor(look.Body, st, "Outline colour", "OutlineColor", null);
            AddCheck(look.Body, st, "Gradient colour on the numbers", "NumberGradient", null);
            AddColor(look.Body, st, "Gradient end colour", "NumberColor2", null);
            AddSlider(look.Body, st, "Gradient direction", "NumberGradientAngle", 0, 360, 15, " deg", null);
            look.Body.Children.Add(Hint("The gradient starts at the normal number colour and fades to the end colour. 0 = left to right, 90 = top to bottom."));
            look.Body.Children.Add(Hint("Outline, gradient, letter spacing and line height work with the Normal and Terminal number styles."));
            AddCopySection(look, mode);
            page.Children.Add(look);

            // ---- glow and shadow ----
            var glow = new AnimatedSection("Glow and shadow", false);
            AddSlider(glow.Body, st, "Glow size", "NumberGlowSize", 0, 40, 1, " px", null);
            AddColor(glow.Body, st, "Glow colour", "NumberGlowColor", null);
            glow.Body.Children.Add(Hint("An empty glow colour uses the number colour. Glow size 0 turns the glow off."));
            AddSlider(glow.Body, st, "Glow strength", "NumberGlowStrength", 1, 3, 1, "", null);
            AddCheck(glow.Body, st, "Shadow behind the numbers", "TextShadow", null);
            AddColor(glow.Body, st, "Shadow colour", "TextShadowColor", null);
            AddSlider(glow.Body, st, "Shadow blur", "TextShadowBlur", 0, 30, 1, " px", null);
            AddSlider(glow.Body, st, "Shadow distance", "TextShadowDistance", 0, 30, 1, " px", null);
            AddSlider(glow.Body, st, "Shadow direction", "TextShadowAngle", 0, 360, 15, " deg", null);
            glow.Body.Children.Add(Hint("Direction: 0 = right, 90 = up, 180 = left, 270 = down. The default 315 falls to the lower right."));
            AddSlider(glow.Body, st, "Shadow opacity", "TextShadowOpacity", 0, 100, 5, " %", null);
            AddCopySection(glow, mode);
            page.Children.Add(glow);

            // ---- spacing, case and font file ----
            var spacing = new AnimatedSection("Spacing, letters and font file", false);
            AddSlider(spacing.Body, st, "Letter spacing", "LetterSpacing", 0, 30, 1, " px", null);
            AddSlider(spacing.Body, st, "Line height", "LineHeight", 50, 200, 5, " %", null);
            AddEnumCombo(spacing.Body, st, "Capital letters", "TextCase", typeof(LetterCase), null);
            spacing.Body.Children.Add(Hint("Capital letters change the label, the text above and below, the date and the extra lines."));
            AddFilePicker(spacing.Body, st, "Your own font file", "FontFile", "Fonts|*.ttf;*.otf|All files|*.*");
            AddCheck(spacing.Body, st, "Use the font file for the small texts too", "FontFileForAll", null);
            spacing.Body.Children.Add(Hint("Pick a .ttf or .otf file. It is copied into the Better Clock folder, so it keeps working if you move the original. It replaces the Font in the Text section."));
            AddCopySection(spacing, mode);
            page.Children.Add(spacing);

            // ---- separator and leading zero ----
            var sep = new AnimatedSection("Separator and leading zero", false);
            AddEnumCombo(sep.Body, st, "Separator", "Separator", typeof(DigitSeparator), null);
            AddCheck(sep.Body, st, "Leading zero (05:00 instead of 5:00)", "LeadingZero", null);
            sep.Body.Children.Add(Hint("Works with every number style except the analog face."));
            AddCopySection(sep, mode);
            page.Children.Add(sep);
        }
    }
}
