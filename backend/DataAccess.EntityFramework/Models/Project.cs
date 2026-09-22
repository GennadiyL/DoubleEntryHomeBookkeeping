using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class Project : IDalEntity
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

	public ProjectGroup? Group
	{
		get;
		set;
	}

	public Guid GroupId
	{
		get;
		set;
	}
}
