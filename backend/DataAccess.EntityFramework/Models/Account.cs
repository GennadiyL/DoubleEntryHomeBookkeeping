using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class Account : IDalEntity
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

	public AccountGroup? Group
	{
		get;
		set;
	}

	public Guid GroupId
	{
		get;
		set;
	}

	public Currency? Currency
	{
		get;
		set;
	}

	public Guid CurrencyId
	{
		get;
		set;
	}

	public Category? Category
	{
		get;
		set;
	}

	public Guid? CategoryId
	{
		get;
		set;
	}

	public Correspondent? Correspondent
	{
		get;
		set;
	}

	public Guid? CorrespondentId
	{
		get;
		set;
	}

	public Project? Project
	{
		get;
		set;
	}

	public Guid? ProjectId
	{
		get;
		set;
	}
}
