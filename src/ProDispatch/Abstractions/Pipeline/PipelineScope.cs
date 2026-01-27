namespace ProDispatch.Abstractions.Pipeline;

/// <summary>
/// Defines the scope where a pipeline behavior should be applied.
/// </summary>
public enum PipelineScope
{
    /// <summary>
    /// Behavior applies to all requests (commands and queries).
    /// </summary>
    All,

    /// <summary>
    /// Behavior applies only to commands.
    /// </summary>
    CommandsOnly,

    /// <summary>
    /// Behavior applies only to queries.
    /// </summary>
    QueriesOnly
}
