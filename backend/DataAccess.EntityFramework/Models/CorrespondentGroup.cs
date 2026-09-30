using Business.Models.Enums;
using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class CorrespondentGroup : IDalEntity
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

	public CorrespondentGroup? Parent
	{
		get;
		set;
	}

	public Guid ParentId
	{
		get;
		set;
	}

	public ICollection<CorrespondentGroup> Children
	{
		get;
		set;
	} = new List<CorrespondentGroup>();

	public ICollection<Correspondent> Elements
	{
		get;
		set;
	} = new List<Correspondent>();
}
