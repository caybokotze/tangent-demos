namespace Scheduling.Application.Abstractions;

/// <summary>Who is asking. Handlers stamp the audit trail with it.</summary>
public interface IUserContext
{
    /// <summary>The name of the caller.</summary>
    string UserName { get; }
}
