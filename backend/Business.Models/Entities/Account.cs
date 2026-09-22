using Business.Models.Entities.Base;

namespace Business.Models.Entities;

public class Account : ElementEntity<AccountGroup, Account>
{
	public Currency Currency { get; set; } = null!;
	public Guid CurrencyId { get; set; }
	public Category? Category { get; set; }
	public Guid? CategoryId { get; set; }
	public Correspondent? Correspondent { get; set; }
	public Guid? CorrespondentId { get; set; }
	public Project? Project { get; set; }
	public Guid? ProjectId { get; set; }
}
