using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class SystemConfig : IDalEntity
{
	public Guid Id
	{
		get;
		set;
	}

	public string MainCurrencyIsoCode
	{
		get;
		set;
	} = string.Empty;

	public DateOnly MinDate
	{
		get;
		set;
	}

	public DateOnly MaxDate
	{
		get;
		set;
	}
}
