using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Synchronization.Contracts.Services.Synchronizations;

namespace Dehb.WinUi.Synchronizations;

/// <summary>
/// Holds presentation state for one manual synchronization attempt.
/// The owning UI creates and disposes this model for each progress popup.
/// Stage notifications are delivered on the UI thread by the host window.
/// Cancellation changes presentation immediately but does not close the popup.
/// The workflow remains active until the host finishes cleanup and reload.
/// Late progress notifications cannot replace cancellation or completion state.
/// Service results remain separate from the current stage label.
/// No database, service scope or WinUI control is retained here.
/// </summary>
internal sealed partial class SynchronizationViewModel : ObservableObject, IDisposable
{
	private readonly CancellationTokenSource _cancellation = new();
	public CancellationToken Token => _cancellation.Token;

	[ObservableProperty]
	public partial string Status { get; set; } = "Starting synchronization...";

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CancelCommand))]
	public partial bool IsRunning { get; set; } = true;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CancelCommand))]
	public partial bool IsCancelling { get; set; }

	private bool CanCancel() => IsRunning && !IsCancelling;

	[RelayCommand(CanExecute = nameof(CanCancel))]
	private void Cancel()
	{
		if (!CanCancel()) { return; }
		IsCancelling = true;
		Status = "Cancelling...";
		_cancellation.Cancel();
	}

	public void Report(SynchronizationStage stage)
	{
		if (!IsRunning || IsCancelling) { return; }
		Status = stage switch
		{
			SynchronizationStage.CheckingAccess => "Checking synchronization access...",
			SynchronizationStage.CapturingChanges => "Capturing local changes...",
			SynchronizationStage.SynchronizingWithMaster => "Synchronizing with Master...",
			SynchronizationStage.DownloadingSnapshot => "Downloading latest data...",
			SynchronizationStage.PreparingLocalData => "Preparing local data...",
			SynchronizationStage.InstallingLocalData => "Installing latest data...",
			SynchronizationStage.Acknowledging => "Confirming synchronization...",
			_ => Status
		};
	}

	public void Complete() => IsRunning = false;
	public void Dispose() { IsRunning = false; _cancellation.Dispose(); }
}
