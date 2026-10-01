using Business.Contracts.Services.Templates;
using Business.Models.Entities;
using Business.Contracts.Base.Services;

namespace Business.Contracts.Services;

/// <summary>
/// Provides template editing and unsaved transaction/template preparation.
/// Templates may contain no entries or an unbalanced set of valid entries.
/// Editor updates replace the complete entry set in submitted list order.
/// Applying a template selects current applicable rates and prepares an unsaved transaction.
/// Preparing a template from a transaction copies amounts and order without transaction rates.
/// CombineElements is unsupported, including equal identities; template group merges remain supported.
/// </summary>
public interface ITemplateService :
	IElementService<TemplateGroup, Template>,
	IUpdateEntityService<TemplateParam>,
	IReadEntityService<TemplateInfo>
{
	/// <summary>
	/// Prepares an unsaved transaction for the editor from template accounts, amounts, description and entry order.
	/// Sets DateTime to now and selects applicable rates; Save uses transaction Add and Cancel changes nothing.
	/// </summary>
	public Task<ApplyTemplateInfo> ApplyTemplate(Guid templateId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Prepares an unsaved template from a Confirmed or Draft transaction for the template editor.
	/// Copies accounts, amounts, description and entry order; the user chooses name and group before Add.
	/// Preparation and cancellation do not persist changes.
	/// </summary>
	public Task<FromTransactionInfo> FromTransaction(Guid transactionId, CancellationToken cancellationToken = default);
}
