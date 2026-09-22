using Business.Models.Entities.Base;

namespace Business.Models.Entities;

/// <summary>
/// Represents a persistent hierarchical group of financial accounts.
/// It participates in the account catalog and owns child groups and accounts.
/// Its writable identifier can be assigned during creation or materialization.
/// Parameterless construction supports persistence and object initialization.
/// The model stores account-group state and does not implement business workflows.
/// </summary>
public class AccountGroup : GroupEntity<AccountGroup, Account>
{
}
