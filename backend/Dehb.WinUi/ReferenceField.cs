using Microsoft.UI.Xaml.Controls;
using Business.Contracts.Services.Trees;

namespace Dehb.WinUi;

internal sealed partial class ReferenceField : UserControl
{
	private readonly UiSession _session;
	private readonly string _catalog;
	private readonly bool _groups;
	private readonly Button _pick;
	public Guid? Id { get; private set; }
	public Func<Guid, bool> Eligible { get; set; } = _ => true;
	public ReferenceField(UiSession session, EditorWindow owner, string catalog, bool groups, bool optional, Guid? id, bool readOnlyGroups)
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
		if (optional)
		{
			row.Children.Add(Ui.Button("×", () => { Id = null; Refresh(); }, owner.Error));
		}

		Content = row;
		owner.Activated += (_, _) => { try { Refresh(); } catch (Exception e) { owner.Error.Text = e.Message; } };
		Refresh();
	}
	public void Refresh()
	{
		if (Id is null)
		{ _pick.Content = "Select " + _catalog; return; }
		TreeInfo tree = CatalogSource.For(_session.Host, _catalog).Read();
		string? name = _groups ? tree.Groups.SingleOrDefault(g => g.Id == Id)?.Name : tree.Elements.SingleOrDefault(e => e.Id == Id)?.Name;
		if (name is null)
		{
			Id = null;
		}

		_pick.Content = name ?? "Select " + _catalog;
	}
}
