using Business.Models.Entities.Base;

namespace Business.Models.Entities;

/// <summary>
/// Represents a persistent category classification referenced by accounts.
/// Belongs to one CategoryGroup through the inherited reference and foreign key.
/// Inherited state supplies naming, optional description, favorites, ordering, and tracking.
/// Services prevent deletion while accounts reference the classification.
/// Writable inherited identity supports creation and materialization of persistent state.
/// The model carries data; business services implement validation and lifecycle operations.
/// </summary>
public class Category : ElementEntity<CategoryGroup, Category>
{
}