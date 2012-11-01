using System;

public enum FutureState
{
    Pending, Completed, Faulted
}

/// <summary>
/// Represents an expected outcome from some operation.
/// </summary>
public class Future<T>
{
    //Futures should be immutable, so throw this if somebody tries to change a completed future
    private static readonly InvalidOperationException s_futureAlreadyCompletedException = 
        new InvalidOperationException("Cannot modify an exception that has already been completed.");

    /// <summary>
    /// The exception that caused the operation to fail (if state is Faulted)
    /// </summary>
    public Exception Exception
    {
        get;
        private set;
    }

    /// <summary>
    /// The result of the operation (if state is Completed)
    /// </summary>
    public T Result
    {
        get;
        private set;
    }

    /// <summary>
    /// The current state of the operation
    /// </summary>
    public FutureState State
    {
        get;
        private set;
    }

    /// <summary>
    /// When this operation started
    /// </summary>
    public DateTime StartTime
    {
        get;
        private set;
    }

    public Future()
    {
        StartTime = DateTime.Now;
        State = FutureState.Pending;
    }

    /// <summary>
    /// Set the result of the operation, indicating that it completed successfully.
    /// </summary>
    public void SetResult(T result)
    {
        if (State == FutureState.Pending)
        {
            Result = result;
            State = FutureState.Completed;
        }
        else
        {
            throw s_futureAlreadyCompletedException;
        }
    }

    /// <summary>
    /// Set an error for this operation, indicating that something went wrong
    /// </summary>
    public void SetException(Exception ex)
    {
        if (State == FutureState.Pending)
        {
            Exception = ex;
            State = FutureState.Faulted;
        }
        else
        {
            throw s_futureAlreadyCompletedException;
        }
    }
}
