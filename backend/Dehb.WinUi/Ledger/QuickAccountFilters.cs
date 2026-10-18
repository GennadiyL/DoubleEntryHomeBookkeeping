namespace Dehb.WinUi.Ledger;

/// <summary>
/// Keeps five account shortcuts for the lifetime of the ledger view.
/// Empty slots are filled in ascending order before any replacement.
/// Once full, replacement advances from the last stored slot and wraps.
/// Clearing a slot leaves the replacement cursor unchanged.
/// Remembering an existing account updates its name without moving the cursor.
/// Activating a shortcut does not change insertion order.
/// Account existence is checked by the view before applying a shortcut.
/// This class performs no persistence, database access or UI operations.
/// </summary>
internal sealed class QuickAccountFilters
{
	public const int Count = 5;
	private readonly Guid?[] _ids = new Guid?[Count];
	private readonly string?[] _names = new string?[Count];
	private int _lastStored = -1;

	public Guid? GetId(int slot) => _ids[slot];
	public string? GetName(int slot) => _names[slot];

	public int Remember(Guid id, string name)
	{
		int slot = Array.IndexOf(_ids, (Guid?)id);
		if (slot < 0)
		{
			slot = Array.IndexOf(_ids, null);
			if (slot < 0)
			{
				slot = (_lastStored + 1) % Count;
			}
			_ids[slot] = id;
			_lastStored = slot;
		}
		_names[slot] = name;
		return slot;
	}

	public void Clear(int slot)
	{
		_ids[slot] = null;
		_names[slot] = null;
	}
}
