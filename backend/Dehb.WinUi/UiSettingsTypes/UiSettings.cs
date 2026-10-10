using System.Text.Json;
using System.Text.Json.Serialization;

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
public sealed class UiSettings
{
	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
		AllowDuplicateProperties = false
	};

	public required int Version { get; init; }
	public required UiMainWindowSettings MainWindow { get; init; }
	public required UiAppearanceSettings Appearance { get; init; }
	public required UiGridSettings Currencies { get; init; }
	public required UiGridSettings Rates { get; init; }

	public static UiSettings Load(string path)
	{
		try
		{
			using FileStream stream = File.OpenRead(path);
			UiSettings settings = JsonSerializer.Deserialize<UiSettings>(stream, SerializerOptions) ?? throw new InvalidDataException("The root must be a settings object.");
			settings.Validate();
			return settings;
		}
		catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or InvalidDataException)
		{
			throw new InvalidDataException($"Cannot load UI settings from '{path}'. {exception.Message} Fix this file or manually copy uisettings.default.json to uisettings.json from the distribution package.", exception);
		}
	}

	private void Validate()
	{
		if (Version != 1)
		{
			throw new InvalidDataException("version must be 1.");
		}
		if (Appearance is null)
		{
			throw new InvalidDataException("appearance must be an object.");
		}
		if (MainWindow is null)
		{
			throw new InvalidDataException("mainWindow must be an object.");
		}
		ValidateColor(MainWindow.SideBarBackColor, "mainWindow.sideBarBackColor");
		ValidateSize(MainWindow.SideBarFontSize, "mainWindow.sideBarFontSize");
		ValidateSize(MainWindow.SideBarItemHeight, "mainWindow.sideBarItemHeight");
		ValidateSize(MainWindow.MinWidth, "mainWindow.minWidth");
		ValidateSize(MainWindow.MinHeight, "mainWindow.minHeight");
		ValidateSize(Appearance.FontSize, "appearance.fontSize");
		ValidateSize(Appearance.RowHeight, "appearance.rowHeight");
		if (Appearance.RowHeight < Appearance.FontSize)
		{
			throw new InvalidDataException("appearance.rowHeight must not be smaller than appearance.fontSize.");
		}
		ValidateColor(Appearance.BackgroundStartColor, "appearance.backgroundStartColor");
		ValidateColor(Appearance.BackgroundEndColor, "appearance.backgroundEndColor");
		ValidateColor(Appearance.SelectionBorderColor, "appearance.selectionBorderColor");
		ValidateColumns(Currencies, "currencies", ["code", "name", "symbol", "star"]);
		ValidateColumns(Rates, "rates", ["date", "rate", "description"]);
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
