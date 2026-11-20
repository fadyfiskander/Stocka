using BuildingBlocks.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Behaviors;

public sealed class UnitOfWorkTransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseTransactionCommand
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<
        UnitOfWorkTransactionBehavior<TRequest, TResponse>> _logger;

    public UnitOfWorkTransactionBehavior(
        IIdentityUnitOfWork unitOfWork,
        ILogger<UnitOfWorkTransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // If beginning the transaction fails, no handler is executed.
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Execute the use case.
            var response = await next(cancellationToken);

            // Persist all tracked changes inside the transaction.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Commit only after the handler and save both succeed.
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return response;
        }
        catch (Exception originalException)
        {
            try
            {
                // Cleanup should still be attempted if the request
                // cancellation token has already been cancelled.
                await _unitOfWork.RollbackTransactionAsync(
                    CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                // Preserve the original failure. A rollback failure
                // must not replace the exception that triggered it.
                _logger.LogError(
                    rollbackException,
                    "Rollback failed for transaction command {CommandType}. " +
                    "Original exception: {OriginalException}",
                    typeof(TRequest).FullName,
                    originalException);
            }

            // Rethrows the original exception with its stack trace.
            throw;
        }
    }
}
