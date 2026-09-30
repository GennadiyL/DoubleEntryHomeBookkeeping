using Business.Models.Enums;
using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class CurrencyRate : IDalEntity
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

	public DateOnly Date
	{
		get;
		set;
	}

	public decimal Rate
	{
		get;
		set;
	}

	public string? Description
	{
		get;
		set;
	}
}
