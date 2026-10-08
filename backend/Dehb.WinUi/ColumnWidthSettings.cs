using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Dehb.WinUi;

/// <summary>
/// Stores this host's column preferences in the current Windows profile.
/// Each catalog and presentation mode uses a separate caller-provided key.
/// Values are percentages in the fixed column display order.
/// Reads return independent arrays for the control to validate.
/// Saving replaces the JSON file after writing a complete temporary file.
/// Existing view entries survive updates to another catalog's widths.
/// File and JSON failures propagate to the host for visible error reporting.
/// These UI preferences are local and are not synchronized or stored in the database.
/// </summary>
internal sealed class ColumnWidthSettings
{
	private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"DoubleEntryHomeBookkeeping", "column-widths.json");

	private Dictionary<string, double[]> Read()
	{
		return File.Exists(_path)
			? JsonSerializer.Deserialize<Dictionary<string, double[]>>(File.ReadAllText(_path))
				?? throw new InvalidDataException("Column settings contain no object.")
			: new Dictionary<string, double[]>();
	}

	public double[]? Load(string key) => Read().TryGetValue(key, out double[]? widths) ? widths : null;

	public void Save(string key, double[] widths)
	{
		Dictionary<string, double[]> settings = Read();
		settings[key] = widths;
		Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
		string temporary = _path + ".tmp";
		File.WriteAllText(temporary, JsonSerializer.Serialize(settings));
		File.Move(temporary, _path, true);
	}
}
