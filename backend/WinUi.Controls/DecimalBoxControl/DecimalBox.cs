using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace WinUi.Controls.DecimalBoxControl;

/// <summary>
/// Combines exact-decimal text entry with always-visible inline spin buttons.
/// Value exposes the last committed decimal without floating-point conversion.
/// Precision controls commit rounding and the step of ten to its negative power.
/// Editing uses plain dot-decimal text and selects the full value on entry.
/// Valid unfocused values use the current Windows regional number format.
/// Invalid text and arithmetic overflow leave the value unchanged with an error.
/// Empty values are permitted and spinning starts them from zero.
/// Hosts own required-value and range validation and can make input read-only.
/// </summary>
public sealed partial class DecimalBox : UserControl, INotifyPropertyChanged
{
	private readonly TextBox _editor = new() { BorderThickness = new Thickness(0), TextAlignment = TextAlignment.Right };
	private readonly ContentPresenter _header = new();
	private readonly TextBlock _error = new() { TextWrapping = TextWrapping.Wrap };
	private readonly RepeatButton _increase;
	private readonly RepeatButton _decrease;
	private decimal? _value;
	private int _precision = 2;
	private bool _editing;
	private bool _formattingText;
	private string? _displayText;
	public event PropertyChangedEventHandler? PropertyChanged;

	public object? Header
	{
		get => _header.Content;
		set
		{
			_header.Content = value;
			_header.Visibility = value is null ? Visibility.Collapsed : Visibility.Visible;
			AutomationProperties.SetName(_editor, value?.ToString() ?? "Decimal value");
		}
	}

	public string Text { get => _editor.Text; set => _editor.Text = value; }
	public bool IsReadOnly
	{
		get => _editor.IsReadOnly;
		set
		{
			_editor.IsReadOnly = value;
			_increase.IsEnabled = _decrease.IsEnabled = !value;
		}
	}
	public decimal Step => DecimalBoxNumber.Step(Precision);

	public int Precision
	{
		get => _precision;
		set
		{
			if (value < 0 || value > 28)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "Precision must be between 0 and 28.");
			}
			if (_precision == value)
			{
				return;
			}
			_precision = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Precision)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Step)));
			Value = _value;
		}
	}

	public decimal? Value
	{
		get => _value;
		set
		{
			SetCommittedValue(value.HasValue ? decimal.Round(value.Value, Precision, MidpointRounding.ToEven) : null);
			ShowValue();
		}
	}

	public DecimalBox()
	{
		IsTabStop = false;
		HorizontalContentAlignment = HorizontalAlignment.Stretch;
		StackPanel layout = new() { Spacing = 4 };
		_header.Visibility = Visibility.Collapsed;
		layout.Children.Add(_header);
		Grid input = new();
		input.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		input.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		input.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		input.Children.Add(_editor);
		_increase = CreateSpinButton("\uE70E", "Increase", true);
		_decrease = CreateSpinButton("\uE70D", "Decrease", false);
		Grid.SetColumn(_increase, 1);
		Grid.SetColumn(_decrease, 2);
		input.Children.Add(_increase);
		input.Children.Add(_decrease);
		layout.Children.Add(new Border
		{
			Child = input, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4),
			BorderBrush = (Brush)Application.Current.Resources["TextControlBorderBrush"],
			Background = (Brush)Application.Current.Resources["TextControlBackground"]
		});
		layout.Children.Add(_error);
		Content = layout;
		_editor.GotFocus += (_, _) =>
		{
			_editing = true;
			if (_displayText is not null && Text == _displayText)
			{
				Text = DecimalBoxNumber.EditText(Value);
			}
			_displayText = null;
			_editor.SelectAll();
		};
		_editor.LostFocus += (_, _) =>
		{
			_editing = false;
			TryCommit();
		};
		_editor.BeforeTextChanging += (_, e) => e.Cancel = !_formattingText && !DecimalBoxNumber.IsAllowedInput(e.NewText);
		_editor.TextChanged += (_, _) => _error.Text = string.Empty;
		_editor.PreviewKeyDown += (_, e) =>
		{
			if (IsReadOnly)
			{
				return;
			}

			if (e.Key == VirtualKey.Decimal)
			{
				int position = _editor.SelectionStart;
				_editor.SelectedText = ".";
				_editor.Select(position + 1, 0);
				e.Handled = true;
			}
			else if (e.Key is VirtualKey.Up or VirtualKey.Down)
			{
				Spin(e.Key == VirtualKey.Up);
				e.Handled = true;
			}
		};
	}

	public new bool Focus(FocusState value) => _editor.Focus(value);

	private RepeatButton CreateSpinButton(string glyph, string name, bool increase)
	{
		RepeatButton button = new()
		{
			Content = new FontIcon { Glyph = glyph, FontSize = 12 },
			Width = 32, MinWidth = 0, Padding = new Thickness(0), BorderThickness = new Thickness(0),
			Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
			IsTabStop = false, AllowFocusOnInteraction = false, Delay = 400, Interval = 80
		};
		AutomationProperties.SetName(button, name);
		ToolTipService.SetToolTip(button, name);
		button.Click += (_, _) => Spin(increase);
		return button;
	}

	private void Spin(bool increase)
	{
		if (IsReadOnly || !TryCommit())
		{
			return;
		}

		if (!DecimalBoxNumber.TryStep(Value ?? 0, Precision, increase, out decimal next))
		{
			_error.Text = "This value cannot be stepped exactly at the selected precision.";
			return;
		}
		Value = next;
		_editor.Select(Text.Length, 0);
	}

	public bool TryCommit()
	{
		if (_displayText is not null && Text == _displayText)
		{
			return true;
		}

		if (!DecimalBoxNumber.TryParse(Text, Precision, out decimal? value))
		{
			_error.Text = "Enter a decimal number using a dot, without grouping or symbols.";
			return false;
		}
		SetCommittedValue(value);
		ShowValue();
		return true;
	}

	private void SetCommittedValue(decimal? value)
	{
		if (_value != value)
		{
			_value = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
		}
	}

	private void ShowValue()
	{
		_error.Text = string.Empty;
		_formattingText = true;
		try
		{
			Text = _editing ? DecimalBoxNumber.EditText(Value) : DecimalBoxNumber.DisplayText(Value, Precision);
		}
		finally
		{
			_formattingText = false;
		}
		_displayText = _editing ? null : Text;
	}
}
