using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Data;

public partial class ClinicManagementDbContext
{
    // Built-in SQL Server functions; no database function/schema is created.
    [DbFunction("ISJSON", IsBuiltIn = true)]
    public static int? AuditIsJson(string? json) => throw new NotSupportedException("SQL query only.");

    [DbFunction("JSON_VALUE", IsBuiltIn = true)]
    public static string? AuditJsonValue(string? json, string path) => throw new NotSupportedException("SQL query only.");
}
