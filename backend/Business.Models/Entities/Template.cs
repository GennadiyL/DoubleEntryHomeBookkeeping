using Business.Models.Entities.Base;

namespace Business.Models.Entities;

public class Template : ElementEntity<TemplateGroup, Template>
{
	public List<TemplateEntry> Entries { get; set; } = new();
}
