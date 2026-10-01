using Business.Contracts.Services.Templates;
using Business.Contracts.Params;
using Business.Models.Entities;
using Business.Contracts.Base.Services;

namespace Business.Contracts.Services;

public interface ITemplateService : IElementService<TemplateGroup, Template>, IUpdateEntityService<TemplateParam>
{
	/// <summary>
	/// Prepares an unsaved transaction for the editor from template accounts, amounts, description and entry order.
	/// Sets DateTime to now and selects applicable rates; Save uses transaction Add and Cancel changes nothing.
	/// </summary>
	public Task<ApplyTemplateInfo> ApplyTemplate(Guid templateId);

	/// <summary>
	/// Prepares an unsaved template from a Confirmed or Draft transaction for the template editor.
	/// Copies accounts, amounts, description and entry order; the user chooses name and group before Add.
	/// Preparation and cancellation do not persist changes.
	/// </summary>
	public Task<FromTransactionInfo> FromTransaction(Guid transactionId);
}
