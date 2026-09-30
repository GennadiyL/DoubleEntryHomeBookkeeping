using Business.Core.Entities;
using Business.Models.Enums;

namespace Business.Models.Entities.Config;

public class LocalConfig : BaseEntity
{
	public string LocalDatasetKey { get; set; } = string.Empty;
	public DefaultAccountNameOrder DefaultAccountNameOrder { get; set; } = DefaultAccountNameOrder.CorrespondentCategoryProject;
	public string DefaultAccountNameSeparator { get; set; } = "/";
}
