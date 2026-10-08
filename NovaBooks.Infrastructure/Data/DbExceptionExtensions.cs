using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace NovaBooks.Infrastructure.Data;

public static class DbExceptionExtensions
{
    private const int SqlServerUniqueIndexViolation = 2601;
    private const int SqlServerUniqueConstraintViolation = 2627;

    /// <summary>
    /// Indica si el error se debe a un índice o restricción única,
    /// por ejemplo cuando dos solicitudes guardan el mismo código a la vez.
    /// </summary>
    public static bool IsUniqueViolation(this DbUpdateException exception)
    {
        return exception.InnerException switch
        {
            SqlException sqlException =>
                sqlException.Number is SqlServerUniqueIndexViolation
                    or SqlServerUniqueConstraintViolation,

            { } inner =>
                inner.Message.Contains(
                    "UNIQUE constraint failed",
                    StringComparison.OrdinalIgnoreCase),

            _ => false
        };
    }
}
