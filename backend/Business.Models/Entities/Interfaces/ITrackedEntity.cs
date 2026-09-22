namespace Business.Models.Entities.Interfaces;

public interface ITrackedEntity
{
    public DateTime Original { get; set; }
    public DateTime Current { get; set; }
    public bool IsDeleted { get; set; }
}
