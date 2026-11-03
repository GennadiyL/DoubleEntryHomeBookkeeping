using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.Contracts.Commands;

public interface ICreateSnapshotCommand
{
	public Task CreateSnapshot(string masterDatasetKey, CancellationToken cancellationToken = default);
}
