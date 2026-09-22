using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class UserConfig : IDalEntity
{
	public Guid Id { get; set; }
}
