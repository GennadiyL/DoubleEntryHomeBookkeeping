using System;
using System.Linq;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUi.Controls.TreeGridControl;

namespace Dehb.WinUi;

/// <summary>
/// Hosts the first interactive catalog TreeGrid demonstration.
/// Accounts include a currency column; correspondents use the common layout.
/// Sample hierarchies are kept separately when switching catalog views.
/// Expansion, star and checkbox values remain attached to their rows.
/// User actions update a status message so control behavior can be checked.
/// This temporary host does not read or modify the Local database.
/// Business editors and persistence will be connected in a later increment.
/// The window owns its sample data for the duration of the session.
/// </summary>
public sealed partial class MainWindow : Window
{
	private readonly ObservableCollection<TreeGridNode> _accounts = CreateAccounts();
	private readonly ObservableCollection<TreeGridNode> _correspondents = CreateCorrespondents();

	public MainWindow()
	{
		InitializeComponent();
		AppWindow.Resize(new Windows.Graphics.SizeInt32(1000, 720));
		ShowCatalog();
	}

	private void OnCatalogChanged(object sender, SelectionChangedEventArgs e)
	{
		if (CatalogTree is not null)
		{
			ShowCatalog();
		}
	}

	private void ShowCatalog()
	{
		CatalogTree.SelectedNode = null;
		CatalogTree.Columns.Clear();
		if (CatalogSelector.SelectedIndex == 0)
		{
			CatalogTree.NameHeader = "Account";
			CatalogTree.Columns.Add(new TreeGridColumn { Header = "Currency", BindingPath = "Data", Width = 2 });
			CatalogTree.ItemsSource = _accounts;
		}
		else
		{
			CatalogTree.NameHeader = "Correspondent";
			CatalogTree.ItemsSource = _correspondents;
		}
	}

	private void OnCheckboxesToggled(object sender, RoutedEventArgs e)
	{
		if (CatalogTree is not null)
		{
			CatalogTree.ShowCheckboxes = ((ToggleSwitch)sender).IsOn;
		}
	}

	private void OnRowActivated(object? sender, TreeGridNode node) => StatusText.Text = $"Activated: {node.Name}";
	private void OnStarChanged(object? sender, TreeGridNode node) => StatusText.Text = $"{node.Name}: star {(node.IsStarred ? "on" : "off")}";
	private void OnCheckStateChanged(object? sender, TreeGridNode node) => StatusText.Text = $"{node.Name}: {(node.IsChecked == true ? "checked" : "unchecked")}";

	private void OnDropValidating(object? sender, TreeGridDropValidationEventArgs e)
	{
		TreeGridDropRequest request = e.Request;
		if (request.Operation == TreeGridDropOperation.Move
			&& request.DestinationParent.Children.Any(child => child.IsGroup == request.Source.IsGroup
				&& string.Equals(child.Name, request.Source.Name, StringComparison.OrdinalIgnoreCase)
				&& (child.IsGroup || Equals(child.Data, request.Source.Data))))
		{
			e.Cancel = true;
			e.Reason = "A matching name already exists in this group.";
		}
	}

	private async void OnDropRequested(object? sender, TreeGridDropRequest request)
	{
		try
		{
			if (request.Operation == TreeGridDropOperation.Merge)
			{
				ContentDialog confirmation = new()
				{
					XamlRoot = CatalogTree.XamlRoot,
					Title = "Merge groups",
					Content = $"Move all items from '{request.Source.Name}' into '{request.Target.Name}' and remove the source group?",
					PrimaryButtonText = "Merge", CloseButtonText = "Cancel",
					DefaultButton = ContentDialogButton.Close
				};
				if (await confirmation.ShowAsync() != ContentDialogResult.Primary)
				{
					return;
				}
			}
			CatalogTree.UpdateRows(() => ApplyDemoDrop(request));
			CatalogTree.RevealNode(request.Operation == TreeGridDropOperation.Merge ? request.Target : request.Source);
			StatusText.Text = $"{request.Operation}: {request.Source.Name} → {request.DestinationParent.Name} (sample data)";
		}
		catch (Exception exception)
		{
			StatusText.Text = $"Drop failed: {exception.Message}";
		}
	}

	private static void ApplyDemoDrop(TreeGridDropRequest request)
	{
		TreeGridNode sourceParent = request.Source.Parent!;
		if (request.Operation == TreeGridDropOperation.Merge)
		{
			foreach (TreeGridNode child in request.Source.Children.ToArray())
			{
				while (request.Target.Children.Any(existing => string.Equals(existing.Name, child.Name, StringComparison.OrdinalIgnoreCase)))
				{
					child.Name += "_1";
				}
				request.Source.Children.Remove(child);
				InsertDemoNode(request.Target, child, request.Target.Children.Count(existing => existing.IsGroup == child.IsGroup));
			}
			sourceParent.Children.Remove(request.Source);
			return;
		}
		sourceParent.Children.Remove(request.Source);
		InsertDemoNode(request.DestinationParent, request.Source, request.InsertIndex);
	}

	private static void InsertDemoNode(TreeGridNode parent, TreeGridNode node, int index)
	{
		TreeGridNode[] siblings = parent.Children.Where(child => child.IsGroup == node.IsGroup).ToArray();
		int collectionIndex = index < siblings.Length ? parent.Children.IndexOf(siblings[index])
			: siblings.Length > 0 ? parent.Children.IndexOf(siblings[^1]) + 1
			: node.IsGroup ? 0 : parent.Children.Count;
		parent.Children.Insert(collectionIndex, node);
	}

	private static ObservableCollection<TreeGridNode> CreateAccounts()
	{
		TreeGridNode root = new() { Name = "Accounts", IsGroup = true, IsExpanded = true };
		TreeGridNode daily = new() { Name = "Daily", IsGroup = true, IsExpanded = true };
		daily.Children.Add(new TreeGridNode { Name = "Cash", Data = "UAH", IsStarred = true });
		daily.Children.Add(new TreeGridNode { Name = "Cash", Data = "USD" });
		daily.Children.Add(new TreeGridNode { Name = "Groceries", Data = "UAH", IsStarred = true });
		TreeGridNode savings = new() { Name = "Savings", IsGroup = true };
		savings.Children.Add(new TreeGridNode { Name = "Emergency fund", Data = "EUR" });
		root.Children.Add(daily);
		root.Children.Add(savings);
		root.Children.Add(new TreeGridNode { Name = "Rebalancing", Data = "UAH" });
		TreeGridNode many = new() { Name = "More accounts", IsGroup = true };
		for (int i = 1; i <= 200; i++)
		{
			many.Children.Add(new TreeGridNode { Name = $"Sample account {i:000}", Data = "UAH" });
		}
		root.Children.Insert(2, many);
		return new ObservableCollection<TreeGridNode> { root };
	}

	private static ObservableCollection<TreeGridNode> CreateCorrespondents()
	{
		TreeGridNode root = new() { Name = "Correspondents", IsGroup = true, IsExpanded = true };
		TreeGridNode shops = new() { Name = "Shops", IsGroup = true, IsExpanded = true, IsChecked = null };
		shops.Children.Add(new TreeGridNode { Name = "Supermarket", IsStarred = true, IsChecked = true });
		shops.Children.Add(new TreeGridNode { Name = "Local bakery" });
		TreeGridNode services = new() { Name = "Services", IsGroup = true };
		services.Children.Add(new TreeGridNode { Name = "Internet provider" });
		services.Children.Add(new TreeGridNode { Name = "A correspondent with a long name to check trimming and the full-name tooltip" });
		root.Children.Add(shops);
		root.Children.Add(services);
		return new ObservableCollection<TreeGridNode> { root };
	}
}
