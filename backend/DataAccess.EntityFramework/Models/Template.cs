using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class Template : IDalEntity
{
	public Guid Id
	{
		get;
		set;
	}

	public DateTime Original
	{
		get;
		set;
	}

	public DateTime Current
	{
		get;
		set;
	}

	public bool IsDeleted
	{
		get;
		set;
	}

	public string Name
	{
		get;
		set;
	} = string.Empty;

	public string? Description
	{
		get;
		set;
	}

	public int Order
	{
		get;
		set;
	}

	public bool IsFavorite
	{
		get;
		set;
	}

	public TemplateGroup? Group
	{
		get;
		set;
	}

	public Guid GroupId
	{
		get;
		set;
	}

	public ICollection<TemplateEntry> Entries
	{
		get;
		set;
	} = new List<TemplateEntry>();
}
