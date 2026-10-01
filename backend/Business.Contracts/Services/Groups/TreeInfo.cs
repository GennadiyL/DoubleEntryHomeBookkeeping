namespace Business.Contracts.Services.Groups;

/// <summary>
/// Contains all category groups and elements for one catalog.
/// Groups includes the root exactly once.
/// Elements connect to groups through GroupId.
/// Collections are separate flat sequences with stable instances.
/// Order is available for constructing the catalog display.
/// No persistent entity or synchronization state is exposed.
/// The service populates this read-only operation result.
/// Changing this record does not persist catalog edits.
/// </summary>
public record TreeInfo
{
	public List<GroupInfo> Groups { get; } = new();
	public List<ElementInfo> Elements { get; } = new();
}
