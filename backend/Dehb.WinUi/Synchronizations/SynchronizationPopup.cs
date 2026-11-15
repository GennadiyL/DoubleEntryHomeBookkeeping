using System.ComponentModel;
using Dehb.WinUi.Common;
using Dehb.WinUi.Dialogs;
using Dehb.WinUi.UiSettingsTypes;
using Dehb.WinUi.WindowManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace Dehb.WinUi.Synchronizations;

/// <summary>
/// Presents a cancellable synchronization attempt in the shared owned-window stack.
/// The model owns progress text and the cooperative cancellation command.
/// Native Close and Escape request cancellation without dismissing active work.
/// The caller closes this popup only after task cleanup and Local reload.
/// Fixed dimensions and shared control styles come from portable UI settings.
/// Property notifications are consumed on the UI thread.
/// Disposal detaches notifications and releases the owned window.
/// Service execution and result presentation remain the host's responsibility.
/// </summary>
internal sealed partial class SynchronizationPopup : IDisposable
{
	private readonly EditorWindow _window;
	private readonly SynchronizationViewModel _model;
	private readonly TextBlock _status;

	public SynchronizationPopup(Window owner, PopupCoordinator coordinator, SynchronizationViewModel model, UiEditorPopupSettings settings)
	{
		_model = model;
		_window = new EditorWindow(owner, coordinator, "Synchronization.Progress", "Synchronization", new SizeInt32(settings.Width, settings.Height));
		_status = Ui.Text(model.Status);
		_status.TextWrapping = TextWrapping.Wrap;
		_window.Fields.Children.Add(new ProgressRing { IsActive = true, Width = 32, Height = 32, HorizontalAlignment = HorizontalAlignment.Center });
		_window.Fields.Children.Add(_status);
		Button cancel = new() { Content = "Cancel", Style = Ui.DialogButtonStyle, Command = model.CancelCommand };
		EditorToolTips.SetHint(cancel, "Cancel (Esc)");
		_window.Commands.Children.Add(cancel);
		_window.CloseRequested = RequestCancellation;
		_model.PropertyChanged += OnPropertyChanged;
	}

	public Task<bool> Show() => _window.Show();
	public void Finish() => _window.Finish(false);
	private void RequestCancellation() { if (_model.CancelCommand.CanExecute(null)) { _model.CancelCommand.Execute(null); } }
	private void OnPropertyChanged(object? sender, PropertyChangedEventArgs args) => _status.Text = _model.Status;
	public void Dispose() { _model.PropertyChanged -= OnPropertyChanged; _window.Dispose(); }
}
