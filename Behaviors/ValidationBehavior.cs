using Prodispatch.Abstractions.Exceptions;
using Prodispatch.Abstractions.Pipeline;
using Prodispatch.Abstractions.Validation;

namespace Prodispatch.Behaviors;

/// <summary>
/// Pipeline behavior that validates requests implementing IValidatable.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken,
        Func<TRequest, CancellationToken, Task<TResponse>> next)
    {
        if (request is IValidatable validatable)
        {
            var errors = validatable.Validate().ToList();
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
