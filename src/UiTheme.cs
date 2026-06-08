using Godot;

namespace MathsPower;

// Helpers de stylage partagés entre TopBar et UiPanel.
public static class UiTheme
{
    // Applique le thème des contrôles à la racine d'un panneau : pose le thème
    // (qui se propage aux enfants), le rattache aux popups des listes déroulantes
    // (qui ne l'héritent pas toujours) et fixe la couleur de texte des boutons.
    public static void ApplyControls(Control root, Palette palette)
    {
        var theme = BuildControlTheme(palette);
        root.Theme = theme;
        StyleSubtree(root, theme, palette.Text, palette.TextStrong);
    }

    private static void StyleSubtree(Node node, Theme theme, Color text, Color strong)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is OptionButton option)
                option.GetPopup().Theme = theme;
            if (child is Button btn)
            {
                btn.AddThemeColorOverride("font_color", text);
                btn.AddThemeColorOverride("font_hover_color", strong);
                btn.AddThemeColorOverride("font_pressed_color", strong);
                btn.AddThemeColorOverride("font_focus_color", strong);
            }
            StyleSubtree(child, theme, text, strong);
        }
    }

    public static StyleBoxFlat PanelStyle(Palette palette, int cornerRadius)
    {
        var style = new StyleBoxFlat
        {
            BgColor = palette.BgPanel,
            BorderColor = palette.Border,
        };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(cornerRadius);
        return style;
    }

    // Thème cohérent pour les contrôles (boutons, listes déroulantes, popups,
    // curseurs). Sans cela, Godot leur applique son thème par défaut (gris
    // foncé), illisible sur un panneau clair. Appliqué à la racine de chaque
    // panneau, il se propage à tous les enfants.
    public static Theme BuildControlTheme(Palette p)
    {
        bool dark = ThemeState.IsDark;
        var white = new Color(1f, 1f, 1f);
        Color accent = p.SectionLabel;

        Color controlBg    = dark ? p.BgPanel.Lerp(white, 0.10f) : white.Lerp(p.BgPanel, 0.40f);
        Color controlHover = dark ? p.BgPanel.Lerp(white, 0.18f) : white.Lerp(accent, 0.12f);
        Color controlPress = dark ? accent.Lerp(p.BgPanel, 0.30f) : white.Lerp(accent, 0.22f);
        Color borderHover  = p.Border.Lerp(accent, 0.55f);

        var theme = new Theme();

        foreach (var type in new[] { "Button", "OptionButton" })
        {
            theme.SetStylebox("normal", type, Box(controlBg, p.Border, 5, 1, 10, 6));
            theme.SetStylebox("hover", type, Box(controlHover, borderHover, 5, 1, 10, 6));
            theme.SetStylebox("pressed", type, Box(controlPress, borderHover, 5, 1, 10, 6));
            theme.SetStylebox("focus", type, Box(controlBg, accent, 5, 1, 10, 6));
            theme.SetStylebox("disabled", type, Box(controlBg, p.Border, 5, 1, 10, 6));
            theme.SetColor("font_color", type, p.Text);
            theme.SetColor("font_hover_color", type, p.TextStrong);
            theme.SetColor("font_pressed_color", type, p.TextStrong);
            theme.SetColor("font_focus_color", type, p.TextStrong);
            theme.SetColor("font_hover_pressed_color", type, p.TextStrong);
            theme.SetColor("font_disabled_color", type, p.TextDim);
        }

        // Liste déroulante ouverte.
        Color popupHover = dark ? accent.Lerp(p.BgPanel, 0.55f) : accent.Lerp(white, 0.78f);
        theme.SetStylebox("panel", "PopupMenu", Box(p.BgPanel, p.Border, 6, 1, 6, 6));
        theme.SetStylebox("hover", "PopupMenu", Box(popupHover, popupHover, 4, 0, 6, 4));
        theme.SetColor("font_color", "PopupMenu", p.Text);
        theme.SetColor("font_hover_color", "PopupMenu", p.TextStrong);

        // Curseurs.
        Color track = dark ? p.BgPanel.Lerp(white, 0.05f) : p.BgPanel.Lerp(p.Border, 0.75f);
        theme.SetStylebox("slider", "HSlider", Box(track, track, 3, 0, 0, 2));
        theme.SetStylebox("grabber_area", "HSlider", Box(accent, accent, 3, 0, 0, 2));
        theme.SetStylebox("grabber_area_highlight", "HSlider", Box(accent.Lerp(white, 0.2f), accent, 3, 0, 0, 2));

        // Séparateurs.
        theme.SetStylebox("separator", "HSeparator", new StyleBoxLine { Color = p.Border, Thickness = 1 });

        // Barre de défilement : la LARGEUR de la barre verticale vient des marges
        // de contenu du stylebox « scroll ». Sans marges, la barre se réduit à un
        // trait invisible — d'où la largeur explicite (padH) ci-dessous.
        Color scrollTrack = dark ? p.BgPanel.Lerp(white, 0.04f) : p.BgPanel.Lerp(p.Border, 0.55f);
        Color grabber = dark ? p.BgPanel.Lerp(white, 0.30f) : p.Border.Lerp(p.Text, 0.20f);
        foreach (var bar in new[] { "VScrollBar", "HScrollBar" })
        {
            theme.SetStylebox("scroll", bar, Box(scrollTrack, scrollTrack, 5, 0, 5, 5));
            theme.SetStylebox("grabber", bar, Box(grabber, grabber, 5, 0, 5, 5));
            theme.SetStylebox("grabber_highlight", bar, Box(borderHover, borderHover, 5, 0, 5, 5));
            theme.SetStylebox("grabber_pressed", bar, Box(accent, accent, 5, 0, 5, 5));
        }

        // Cases à cocher : texte lisible (l'icône reste celle de Godot).
        theme.SetColor("font_color", "CheckBox", p.Text);
        theme.SetColor("font_hover_color", "CheckBox", p.TextStrong);
        theme.SetColor("font_pressed_color", "CheckBox", p.TextStrong);

        return theme;
    }

    private static StyleBoxFlat Box(Color bg, Color border, int radius, int borderWidth, int padH, int padV)
    {
        var s = new StyleBoxFlat { BgColor = bg };
        if (borderWidth > 0)
        {
            s.BorderColor = border;
            s.SetBorderWidthAll(borderWidth);
        }
        s.SetCornerRadiusAll(radius);
        s.ContentMarginLeft = padH;
        s.ContentMarginRight = padH;
        s.ContentMarginTop = padV;
        s.ContentMarginBottom = padV;
        return s;
    }
}
