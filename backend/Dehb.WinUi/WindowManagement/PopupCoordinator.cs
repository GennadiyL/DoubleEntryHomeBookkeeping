using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices;

namespace Dehb.WinUi.WindowManagement;

internal sealed class PopupCoordinator
{
	private readonly List<(string Key, Window Window)> _windows = new();
	private readonly Dictionary<object, HashSet<Guid>> _accounts = new();
	public string? DockedCatalog { get; set; }
	public bool IsActive(string key) => _windows.Any(item => item.Key == key) || key == DockedCatalog + ".Tree";
	public void Protect(object editor, IEnumerable<Guid> accounts) => _accounts[editor] = accounts.ToHashSet();
	public void Release(object editor) => _accounts.Remove(editor);
	public void CheckAccountDelete(Guid id)
	{
		if (_accounts.Values.Any(ids => ids.Contains(id)))
		{
			throw new InvalidOperationException("Account is used by an open editor.");
		}
	}
	public void Push(string key, Window window) => _windows.Add((key, window));
	public void Pop(Window window) => _windows.RemoveAll(item => item.Window == window);
	public Window Top(Window main) => _windows.LastOrDefault().Window ?? main;
}
