using Business.Contracts.Params;
using Business.Contracts.Services.Base;

namespace Business.Contracts.Services;

public interface ITransactionService : IEntityService<TransactionParam>
{
	public Task DeleteTransactionList(List<Guid> transactionIds);
}
