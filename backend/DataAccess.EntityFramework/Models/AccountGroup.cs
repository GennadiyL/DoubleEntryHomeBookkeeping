using Business.Models.Enums;
using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class AccountGroup : IDalEntity
{
	public Guid Id
	{
		get;
		set;
	}

	public long? EditRevision
	{
		get;
		set;
	}

	public long? DeleteRevision
	{
		get;
		set;
	}

	public ModificationType ModificationType
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

	public AccountGroup? Parent
	{
		get;
		set;
	}

	public Guid ParentId
	{
		get;
		set;
	}

	public ICollection<AccountGroup> Children
	{
		get;
		set;
	} = new List<AccountGroup>();

	public ICollection<Account> Elements
	{
		get;
		set;
	} = new List<Account>();
}

