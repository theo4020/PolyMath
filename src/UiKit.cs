using System;
using System.Collections.Generic;
using Godot;

namespace MathsPower;

// Widgets d'UI partagés et cohérents entre tous les panneaux/menus.
// Centralise le style pour un HUD uniforme.
public static class UiKit
{
    // En-tête de section : barre d'accent colorée + libellé.
    // `labels` / `bars` collectent les nœuds pour le re-thématisage.
    // Retourne la rangée (utile pour masquer une section contextuellement).
    public static HBoxContainer SectionHeader(
        VBoxContainer parent, string text, List<Label> labels, List<ColorRect> bars)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var bar = new ColorRect
        {
            CustomMinimumSize = new Vector2(4, 15),
            Color = Palette.Current().SectionLabel,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        row.AddChild(bar);
        bars.Add(bar);

        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 14);
        row.AddChild(label);
        labels.Add(label);

        parent.AddChild(row);
        return row;
    }

    // Rangée slider : libellé · curseur · valeur. Retourne (slider, valeur, rangée).
    public static (HSlider slider, Label value, HBoxContainer row) SliderRow(
        VBoxContainer parent, string labelText, double min, double max, double step,
        double initial, string tooltip, Action<double> onChanged,
        List<Label> textLabels, List<Label> valueLabels, float labelWidth = 86f)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);

        var name = new Label { Text = labelText, CustomMinimumSize = new Vector2(labelWidth, 0), TooltipText = tooltip };
        row.AddChild(name);
        textLabels.Add(name);

        var slider = new HSlider
        {
            MinValue = min, MaxValue = max, Step = step,
            TooltipText = tooltip, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        slider.SetValueNoSignal(initial);

        bool isInt = step >= 1.0;
        var value = new Label
        {
            Text = Format(initial, isInt),
            CustomMinimumSize = new Vector2(44, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        slider.ValueChanged += (double v) => { value.Text = Format(v, isInt); onChanged(v); };
        row.AddChild(slider);
        row.AddChild(value);
        valueLabels.Add(value);

        parent.AddChild(row);
        return (slider, value, row);
    }

    private static string Format(double v, bool isInt) => isInt ? ((int)v).ToString() : v.ToString("0.00");
}
