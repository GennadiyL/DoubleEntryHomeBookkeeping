using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class Currency : IDalEntity
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

	public string IsoCode
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
