using Business.Models.Enums;
using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class LocalConfig : IDalEntity
{
	public Guid Id { get; set; }
	public string LocalDatasetKey { get; set; } = string.Empty;
	public DefaultAccountNameOrder DefaultAccountNameOrder { get; set; } = DefaultAccountNameOrder.CorrespondentCategoryProject;
	public string DefaultAccountNameSeparator { get; set; } = "/";
}
