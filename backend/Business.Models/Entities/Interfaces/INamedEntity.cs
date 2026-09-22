namespace Business.Models.Entities.Interfaces;

public interface INamedEntity
{
    public string Name { get; set; }
    public string? Description { get; set; }
}
