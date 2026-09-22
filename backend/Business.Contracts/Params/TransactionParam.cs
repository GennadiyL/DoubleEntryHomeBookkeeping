using Business.Models.Enums;

namespace Business.Contracts.Params;

public class TransactionParam
{
    public DateTime DateTime { get; set; }
    public TransactionState State { get; set; }
    public string? Comment { get; set; }
    public List<TransactionEntryParam> Entries { get; } = new();
}
