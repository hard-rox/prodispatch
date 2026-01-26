namespace ProDispatch.Examples.MinimalApi.Behaviors;

/// <summary>
/// Pipeline behavior that validates requests implementing IValidatable.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    /// <summary>Validates the request and invokes the next delegate.</summary>
    /// <param name="request">Request being handled.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <param name="next">Next delegate in the pipeline.</param>
    /// <returns>Response produced by the pipeline.</returns>
    public async Task<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken,
        Func<TRequest, CancellationToken, Task<TResponse>> next)
    {
        if (request is IValidatable validatable)
        {
            List<string> errors = validatable.Validate().ToList();
            if (errors.Count > 0)
            {
                Console.WriteLine($"[VALIDATION] Validation failed for {request.GetType().Name}");
                throw new ValidationException(errors);
            }

            Console.WriteLine($"[VALIDATION] Validation passed for {request.GetType().Name}");
        }

        return await next(request!, cancellationToken);
    }
}
