using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Dehb.WinUi.Common;

/// <summary>
/// Applies the configured background to read-only text inputs.
/// The application TextBox style supplies the brush through an attached property.
/// Includes TextBox controls hosted inside DecimalBox controls.
/// Observes IsReadOnly so changing an account updates rate-field appearance.
/// Keeps the same background while hovered or focused.
/// Restores original backgrounds when the field becomes editable again.
/// Does not disable the control or alter selection and clipboard commands.
/// Subscriptions belong to the control and do not retain closed windows globally.
/// </summary>
internal static class ReadOnlyTextBoxAppearance
{
	public static readonly DependencyProperty BrushProperty = DependencyProperty.RegisterAttached(
		"Brush", typeof(Brush), typeof(ReadOnlyTextBoxAppearance), new PropertyMetadata(null, OnBrushChanged));
	private static readonly DependencyProperty UpdateProperty = DependencyProperty.RegisterAttached(
		"Update", typeof(object), typeof(ReadOnlyTextBoxAppearance), new PropertyMetadata(null));
	private static readonly string[] BorderKeys = ["TextControlBorderBrush", "TextControlBorderBrushPointerOver"];
	private static readonly string[] BackgroundKeys = ["TextControlBackground", "TextControlBackgroundPointerOver", "TextControlBackgroundFocused"];

	private static void OnBrushChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		if (sender is not TextBox textBox) { return; }
		if (textBox.GetValue(UpdateProperty) is Action existing) { existing(); return; }
		object background = textBox.ReadLocalValue(Control.BackgroundProperty);
		object border = textBox.ReadLocalValue(Control.BorderBrushProperty);
		Dictionary<string, object> resources = BackgroundKeys.Concat(BorderKeys).Where(textBox.Resources.ContainsKey)
			.ToDictionary(key => key, key => textBox.Resources[key]);
		bool applied = false;
		void Update()
		{
			if (textBox.IsReadOnly && textBox.GetValue(BrushProperty) is Brush brush)
			{
				foreach (string key in BackgroundKeys) { textBox.Resources[key] = brush; }
				textBox.Background = brush;
				Brush borderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"];
				foreach (string key in BorderKeys) { textBox.Resources[key] = borderBrush; }
				textBox.BorderBrush = borderBrush;
				applied = true;
			}
			else if (applied)
			{
				foreach (string key in BackgroundKeys.Concat(BorderKeys))
				{
					if (resources.TryGetValue(key, out object? value)) { textBox.Resources[key] = value; }
					else { textBox.Resources.Remove(key); }
				}
				if (background == DependencyProperty.UnsetValue) { textBox.ClearValue(Control.BackgroundProperty); }
				else { textBox.SetValue(Control.BackgroundProperty, background); }
				if (border == DependencyProperty.UnsetValue) { textBox.ClearValue(Control.BorderBrushProperty); }
				else { textBox.SetValue(Control.BorderBrushProperty, border); }
				applied = false;
			}
		}
		textBox.SetValue(UpdateProperty, (Action)Update);
		textBox.RegisterPropertyChangedCallback(TextBox.IsReadOnlyProperty, (_, _) => Update());
		Update();
	}
}
