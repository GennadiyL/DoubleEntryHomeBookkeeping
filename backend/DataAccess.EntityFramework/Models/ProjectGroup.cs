using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class ProjectGroup : IDalEntity
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

	public ProjectGroup? Parent
	{
		get;
		set;
	}

	public Guid ParentId
	{
		get;
		set;
	}

	public ICollection<ProjectGroup> Children
	{
		get;
		set;
	} = new List<ProjectGroup>();

	public ICollection<Project> Elements
	{
		get;
		set;
	} = new List<Project>();
}
