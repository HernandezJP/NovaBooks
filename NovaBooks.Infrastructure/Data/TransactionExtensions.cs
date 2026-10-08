using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NovaBooks.Application.Common;

namespace NovaBooks.Infrastructure.Data;

public static class TransactionExtensions
{
    /// <summary>
    /// Ejecuta una operación compuesta en una transacción compatible
    /// con la estrategia de reintentos. Solo confirma si la
    /// operación termina con éxito.
    /// </summary>
    public static Task<OperationResult<T>> ExecuteInTransactionAsync<T>(
        this AppDbContext context,
        Func<CancellationToken, Task<OperationResult<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        IExecutionStrategy strategy =
            context.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(
            async token =>
            {
                await using IDbContextTransaction transaction =
                    await context.Database.BeginTransactionAsync(token);

                OperationResult<T> result = await operation(token);

                if (result.Succeeded)
                {
                    await transaction.CommitAsync(token);
                }
                else
                {
                    await transaction.RollbackAsync(token);
                }

                return result;
            },
            cancellationToken);
    }
}
