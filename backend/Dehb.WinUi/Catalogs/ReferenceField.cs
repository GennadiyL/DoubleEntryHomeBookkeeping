using Dehb.WinUi.Common;
using Dehb.WinUi.Dialogs;
using Dehb.WinUi.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Business.Contracts.Services.Trees;
using Microsoft.UI.Xaml.Automation.Peers;
using Windows.System;

namespace Dehb.WinUi.Catalogs;

internal sealed partial class ReferenceField : UserControl
{
	private readonly UiSession _session;
	private readonly string _catalog;
	private readonly bool _groups;
	private readonly Button _pick;
	private readonly Button? _clear;
	private readonly TextBox? _display;
	public Guid? Id { get; private set; }
	public Func<Guid, bool> Eligible { get; set; } = _ => true;
	public ReferenceField(UiSession session, EditorWindow owner, string catalog, bool groups, bool optional, Guid? id, bool readOnlyGroups, bool textBoxPicker = false)
	{
		_session = session;
		_catalog = catalog;
		_groups = groups;
		Id = id;
		_pick = Ui.AsyncButton("Select", async () =>
		{
			Guid? chosen = await session.Pick(owner, catalog, groups, Id, Eligible, readOnlyGroups);
			if (chosen.HasValue)
			{
				Id = chosen;
			}

			Refresh();
		}, owner.Error);
		StackPanel row = Ui.Row(_pick);
		Button? clear = null;
		if (optional)
		{
			clear = Ui.Button("Ã—", () => { Id = null; Refresh(); }, owner.Error);
			row.Children.Add(clear);
		}

		_clear = clear;
		if (textBoxPicker)
		{
			HorizontalContentAlignment = HorizontalAlignment.Stretch;
			_display = new TextBox { IsReadOnly = true, MinWidth = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
			AutomationProperties.SetName(_display, "Selected " + catalog);
			row.Children.Clear();
			_pick.Content = "...";
			ToolTipService.SetToolTip(_pick, "Choose " + catalog);
			AutomationProperties.SetName(_pick, "Choose " + catalog);
			Grid picker = new() { ColumnSpacing = 8 };
			picker.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			picker.Children.Add(_display);
			if (clear is not null)
			{
				clear.Content = "x";
				ToolTipService.SetToolTip(clear, "Clear selection");
				AutomationProperties.SetName(clear, "Clear selection");
				picker.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
				Grid.SetColumn(clear, 1);
				picker.Children.Add(clear);
			}
			picker.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			Grid.SetColumn(_pick, picker.ColumnDefinitions.Count - 1);
			picker.Children.Add(_pick);
			foreach (Button button in picker.Children.OfType<Button>())
			{
				button.MinWidth = button.MinHeight = 0;
				button.Padding = new Thickness(0);
				button.VerticalAlignment = VerticalAlignment.Center;
				button.HorizontalContentAlignment = HorizontalAlignment.Center;
				button.VerticalContentAlignment = VerticalAlignment.Center;
				_display.SizeChanged += (_, args) => button.Width = button.Height = args.NewSize.Height;
			}
			Content = picker;
		}
		else
		{
			Content = row;
		}
		owner.Activated += (_, _) => { try { Refresh(); } catch (Exception e) { owner.Error.Text = e.Message; } };
		Refresh();
	}
	public void AddShortcuts(EditorWindow owner, VirtualKey key, string keyText, string label)
	{
		owner.AddShortcut(_pick, key, VirtualKeyModifiers.Control, $"Choose {label} (Ctrl+{keyText})",
			() => new ButtonAutomationPeer(_pick).Invoke());
		if (_clear is not null)
		{
			owner.AddShortcut(_clear, key, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, $"Clear {label} (Ctrl+Shift+{keyText})",
				() => new ButtonAutomationPeer(_clear).Invoke());
		}
	}

	public void Refresh()
	{
		if (Id is null)
		{ SetDisplayName(null); return; }
		TreeInfo tree = CatalogSource.For(_session.Host, _catalog).Read();
		GroupInfo? selectedGroup = _groups ? tree.Groups.SingleOrDefault(g => g.Id == Id) : null;
		string? name = _groups ? selectedGroup is { IsRoot: true } ? _catalog : selectedGroup?.Name : tree.Elements.SingleOrDefault(e => e.Id == Id)?.Name;
		if (name is null)
		{
			Id = null;
		}

		SetDisplayName(name);
	}
	private void SetDisplayName(string? name)
	{
		if (_display is not null)
		{
			_display.Text = name ?? "";
			_display.PlaceholderText = "Select " + _catalog;
		}
		else
		{
			_pick.Content = name ?? "Select " + _catalog;
		}
	}
}
