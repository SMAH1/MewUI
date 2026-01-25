namespace Aprillz.MewUI;

public sealed class DispatcherUnhandledExceptionEventArgs : EventArgs
{
    public DispatcherUnhandledExceptionEventArgs(Exception exception) => Exception = exception;

    public Exception Exception { get; }

    /// <summary>
    /// Set to true to continue the UI loop instead of terminating.
    /// </summary>
    public bool Handled { get; set; }
}

