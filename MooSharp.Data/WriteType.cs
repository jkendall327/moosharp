namespace MooSharp.Data;

public enum WriteType
{
    /// <summary>
    /// The database write will be actioned immediately.
    /// </summary>
    Immediate,
    
    /// <summary>
    /// The database write will be pushed to a queue that will be processed at a later point.
    /// </summary>
    Deferred
}