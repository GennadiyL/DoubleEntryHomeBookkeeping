using Business.Models.Entities.Base;

namespace Business.Models.Entities;

/// <summary>
/// Represents a persistent hierarchical group of transaction categories.
/// It participates in the category catalog and owns child groups and categories.
/// Its writable identifier can be assigned during creation or materialization.
/// Parameterless construction supports persistence and object initialization.
/// The model stores category-group state and does not implement business workflows.
/// </summary>
public class CategoryGroup : GroupEntity<CategoryGroup, Category>
{
}
