using Business.Models.Enums;

namespace Business.Models.Entities.Interfaces;

public interface ITrackedEntity
{
	public long? EditRevision { get; set; }
	public long? DeleteRevision { get; set; }
	public ModificationType ModificationType { get; set; }
}
