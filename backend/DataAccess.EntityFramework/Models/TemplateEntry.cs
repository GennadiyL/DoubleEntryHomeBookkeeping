using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class TemplateEntry : IDalEntity
{
	public Guid Id
	{
		get;
		set;
	}

	public Template? Template
	{
		get;
		set;
	}

	public Guid TemplateId
	{
		get;
		set;
	}

	public Account? Account
	{
		get;
		set;
	}

	public Guid AccountId
	{
		get;
		set;
	}

	public decimal Amount
	{
		get;
		set;
	}
}
