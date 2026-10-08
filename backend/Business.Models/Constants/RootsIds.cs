namespace Business.Models.Constants;

/// <summary>
/// Defines fixed identities for the six catalog root groups.
/// Account, category, correspondent, project, template, and report roots each have a distinct identity.
/// Master and local copies use the same values during initialization and lookup.
/// A root refers to its own identity as parent and retains that identity throughout synchronization.
/// These shared values are application constants rather than editable configuration.
/// Consumers apply the values; this class does not create entities or enforce validation.
/// </summary>
public static class RootsIds
{
	public static readonly Guid AccountGroupId = new("22D0BBCC-37EC-4AF4-B5C7-9CAF34FEC1E9");
	public static readonly Guid CategoryGroupId = new("CDB033F6-8686-4222-B33F-66B5A3BF2948");
	public static readonly Guid CorrespondentGroupId = new("9C19A1FE-9C57-4703-9105-79075987EE45");
	public static readonly Guid ProjectGroupId = new("7C9385F6-28F2-4947-8B44-E81DD8D01949");
	public static readonly Guid ReportGroupId = new("8FD28CC7-69B2-441C-A27B-C85F56DF0D44");
	public static readonly Guid TemplateGroupId = new("B232A84F-47D8-426E-AA54-BA8771B8B6DE");
}
