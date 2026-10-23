using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Contracts;

namespace Dehb.WinUi.UiSettingsTypes;

/// <summary>
/// Loads the portable application's user-authored appearance configuration.
/// The active file resides beside the executable and is read once at startup.
/// The distributed default file is only a source for manual restoration.
/// No fallback, automatic repair or write-back occurs while loading.
/// Required members and unknown-property rejection catch incomplete or misspelled settings.
/// Validation checks the version, dimensions, colors and named columns.
/// Failures include the active file path for the startup error message.
/// Business configuration and automatically saved window geometry remain separate.
/// </summary>
public static class UiSettingsLoader
{
	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
		AllowDuplicateProperties = false
	};


	public static UiSettings Load(IFileService fileService, string path)
	{
		try
		{
			using FileStream stream = fileService.OpenRead(path);
			UiSettings settings = JsonSerializer.Deserialize<UiSettings>(stream, SerializerOptions) ??
								  throw new InvalidDataException("The root must be a settings object.");
			Validate(settings);
			return settings;
		}
		catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or InvalidDataException)
		{
			throw new InvalidDataException($"Cannot load UI settings from '{path}'. {exception.Message} Fix this file or manually copy uisettings.default.json to uisettings.json from the distribution package.", exception);
		}
	}

	private static void Validate(UiSettings settings)
	{
		if (settings.Version != 1)
		{
			throw new InvalidDataException("version must be 1.");
		}
		if (settings.Appearance is null)
		{
			throw new InvalidDataException("appearance must be an object.");
		}
		if (settings.MainWindow is null)
		{
			throw new InvalidDataException("mainWindow must be an object.");
		}
		ValidateSize(settings.Appearance.ButtonWidth, "appearance.buttonWidth");
		ValidateSize(settings.Appearance.ButtonHeight, "appearance.buttonHeight");
		ValidateColor(settings.MainWindow.SideBarBackColor, "mainWindow.sideBarBackColor");
		ValidateSize(settings.MainWindow.SideBarFontSize, "mainWindow.sideBarFontSize");
		ValidateSize(settings.MainWindow.SideBarItemHeight, "mainWindow.sideBarItemHeight");
		ValidateSize(settings.MainWindow.Width, "mainWindow.width");
		ValidateSize(settings.MainWindow.MinHeight, "mainWindow.minHeight");
		ValidateSize(settings.Appearance.FontSize, "appearance.fontSize");
		ValidateSize(settings.Appearance.RowHeight, "appearance.rowHeight");
		if (settings.Appearance.RowHeight < settings.Appearance.FontSize)
		{
			throw new InvalidDataException("appearance.rowHeight must not be smaller than appearance.fontSize.");
		}
		ValidateColor(settings.Appearance.BackgroundStartColor, "appearance.backgroundStartColor");
		ValidateColor(settings.Appearance.BackgroundEndColor, "appearance.backgroundEndColor");
		ValidateColor(settings.Appearance.ReadonlyColor, "appearance.readonlyColor");
		ValidateColor(settings.Appearance.ErrorColor, "appearance.errorColor");
		ValidateColor(settings.Appearance.SelectionBorderColor, "appearance.selectionBorderColor");
		ValidateSize(settings.Appearance.SelectionBorderThickness, "appearance.selectionBorderThickness");
		ValidateColor(settings.Appearance.FocusBorderColor, "appearance.focusBorderColor");
		ValidateSize(settings.Appearance.FocusBorderThickness, "appearance.focusBorderThickness");
		ValidatePopup(settings.TransactionEditor, "transactionEditor");
		ValidateSize(settings.TransactionEditor.DateWidth, "transactionEditor.dateWidth");
		ValidateSize(settings.TransactionEditor.TimeWidth, "transactionEditor.timeWidth");
		ValidateSize(settings.TransactionEditor.AmountWidth, "transactionEditor.amountWidth");
		ValidateSize(settings.TransactionEditor.CurrencyWidth, "transactionEditor.currencyWidth");
		ValidateSize(settings.TransactionEditor.RateWidth, "transactionEditor.rateWidth");
		ValidateSize(settings.TransactionEditor.BaseAmountWidth, "transactionEditor.baseAmountWidth");
		ValidatePopup(settings.TemplateEditor, "templateEditor");
		ValidateSize(settings.TemplateEditor.AmountWidth, "templateEditor.amountWidth");
		ValidateSize(settings.TemplateEditor.CurrencyWidth, "templateEditor.currencyWidth");
		ValidatePopup(settings.HelpPopup, "helpPopup");
		ValidatePopup(settings.AccountEditor, "accountEditor");
		ValidatePopup(settings.ElementEditor, "elementEditor");
		ValidatePopup(settings.ElementTreeGridPopup, "elementTreeGridPopup");
		ValidatePopup(settings.GroupTreeGridPopup, "groupTreeGridPopup");
		ValidatePopup(settings.GroupEditor, "groupEditor");
		ValidatePopup(settings.AddCurrencyEditor, "addCurrencyEditor");
		ValidatePopup(settings.EditCurrencyEditor, "editCurrencyEditor");
		ValidatePopup(settings.CurrencyRateEditor, "currencyRateEditor");
		ValidatePopup(settings.DeleteCurrencyRateEditor, "deleteCurrencyRateEditor");
		ValidateColumns(settings.CatalogTreeGrid, "catalogTreeGrid", ["main", "description", "star"]);
		ValidateColumns(settings.AccountTreeGrid, "accountTreeGrid", ["main", "description", "currency", "star"]);
		ValidateColumns(settings.TransactionGrid, "transactionGrid", ["dateTime", "description", "state", "amount", "cumulativeAmount", "currency"]);
		ValidateSize(settings.TransactionGrid.DateWidth, "transactionGrid.dateWidth");
		ValidateSize(settings.TransactionGrid.RangeWidth, "transactionGrid.rangeWidth");
		ValidateSize(settings.TransactionGrid.AccountWidth, "transactionGrid.accountWidth");
		ValidateColumns(settings.CurrencyGrid, "currencyGrid", ["code", "name", "symbol", "star"]);
		ValidateColumns(settings.CurrencyRateGrid, "currencyRateGrid", ["date", "rate", "description"]);
	}

	private static void ValidatePopup(UiEditorPopupSettings? popup, string path)
	{
		if (popup is null)
		{
			throw new InvalidDataException($"{path} must be an object.");
		}
		ValidateSize(popup.Width, $"{path}.width");
		ValidateSize(popup.Height, $"{path}.height");
	}

	private static void ValidateColumns(UiGridSettings? grid, string path, string[] names)
	{
		if (grid?.Columns is null)
		{
			throw new InvalidDataException($"{path}.columns must be an object.");
		}
		foreach (string name in names)
		{
			if (!grid.Columns.TryGetValue(name, out double width))
			{
				throw new InvalidDataException($"{path}.columns.{name} is required.");
			}
			ValidateSize(width, $"{path}.columns.{name}");
		}
		foreach (string name in grid.Columns.Keys)
		{
			if (!names.Contains(name, StringComparer.Ordinal))
			{
				throw new InvalidDataException($"{path}.columns.{name} is not a supported setting.");
			}
		}
	}

	private static void ValidateSize(double value, string path)
	{
		if (!double.IsFinite(value) || value <= 0)
		{
			throw new InvalidDataException($"{path} must be a finite positive number.");
		}
	}

	private static void ValidateColor(string? value, string path)
	{
		if (value is null || value.Length != 7 || value[0] != '#' || !value.Skip(1).All(Uri.IsHexDigit))
		{
			throw new InvalidDataException($"{path} must be an RGB color such as #F3F7FE.");
		}
	}
}
