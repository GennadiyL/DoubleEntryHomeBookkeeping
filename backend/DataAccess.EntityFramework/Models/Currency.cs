using Business.Models.Enums;
using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class Currency : IDalEntity
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

	public string Code
	{
		get;
		set;
	} = string.Empty;

	public string Symbol
	{
		get;
		set;
	} = string.Empty;

	public string Name
	{
		get;
		set;
	} = string.Empty;

	public bool IsFavorite
	{
		get;
		set;
	}

	public int Order
	{
		get;
		set;
	}

	public ICollection<CurrencyRate> Rates
	{
		get;
		set;
	} = new List<CurrencyRate>();
}
