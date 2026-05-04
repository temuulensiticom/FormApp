using MySqlConnector;
using System.Text;
using System.Text.RegularExpressions;

namespace fromApp.Services
{
    public class DatabaseService
    {
        public DatabaseTarget CreateDefaultServerTarget(string host, int port, string user, string password)
        {
            return CreateTarget("Default Server", host, port, string.Empty, user, password);
        }

        public DatabaseTarget CreateSchemaTarget(string schema, string host, int port, string user, string password)
        {
            return CreateTarget(schema, host, port, schema, user, password);
        }

        private DatabaseTarget CreateTarget(string name, string host, int port, string database, string user, string password)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host.Trim(),
                Port = (uint)(port > 0 ? port : 3008),
                Database = (database ?? string.Empty).Trim(),
                UserID = user.Trim(),
                Password = password ?? string.Empty,
                ConnectionProtocol = MySqlConnectionProtocol.Tcp,
                SslMode = MySqlSslMode.None,
                AllowPublicKeyRetrieval = true,
                ConnectionTimeout = 10
            };

            return new DatabaseTarget(name.Trim(), builder.ConnectionString);
        }

        public async Task<ExecutionResult> ExecuteQueriesAsync(string sqlContent, IEnumerable<DatabaseTarget> targets)
        {
            var result = new ExecutionResult();

            try
            {
                var queries = ParseSqlQueries(sqlContent);

                if (queries.Count == 0)
                {
                    result.HasError = true;
                    result.Message = "No valid SQL queries found in the file.";
                    return result;
                }

                result.TotalQueries = queries.Count;
                result.Queries = queries
                    .Select((query, index) => new UploadedQuery { Number = index + 1, Text = query })
                    .ToList();

                foreach (var target in targets)
                {
                    var databaseResult = await ExecuteQueriesOnDatabase(target.ConnectionString, queries, target.Name);
                    result.DatabaseResults.Add(databaseResult);
                    if (!string.IsNullOrEmpty(databaseResult.Error) || databaseResult.FailedQueries > 0)
                    {
                        result.HasError = true;
                    }
                }

                if (!result.HasError)
                {
                    result.Message = "All queries executed successfully!";
                }
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.Message = $"Fatal error: {ex.Message}";
            }

            return result;
        }

        private async Task<DatabaseExecutionResult> ExecuteQueriesOnDatabase(string connectionString, List<string> queries, string dbName)
        {
            var result = new DatabaseExecutionResult { DatabaseName = dbName };

            try
            {
                using (var connection = await OpenConnectionAsync(connectionString))
                {
                    result.Connected = true;

                    for (var i = 0; i < queries.Count; i++)
                    {
                        var query = queries[i];
                        var queryResult = new QueryExecutionDetail
                        {
                            QueryNumber = i + 1,
                            QueryText = query
                        };

                        try
                        {
                            if (string.IsNullOrWhiteSpace(query))
                                continue;

                            if (IsUseDatabaseStatement(query))
                            {
                                queryResult.Success = true;
                                queryResult.Status = "Skipped";
                                queryResult.Error = "USE statement skipped so this run stays on the current schema.";
                                result.QueryResults.Add(queryResult);
                                continue;
                            }

                            var executableQuery = MakeInsertDuplicateSafe(query);

                            using (var command = new MySqlCommand(executableQuery, connection))
                            {
                                command.CommandTimeout = 300;
                                var rowsAffected = await command.ExecuteNonQueryAsync();
                                result.SuccessfulQueries++;
                                queryResult.Success = true;
                                queryResult.Status = executableQuery == query
                                    ? "Executed"
                                    : "Executed; duplicate rows skipped";
                                queryResult.RowsAffected = rowsAffected;
                                result.Details.Add($"Query executed successfully (Rows affected: {rowsAffected})");
                            }
                        }
                        catch (MySqlException ex) when (ex.Number == 1062)
                        {
                            queryResult.Success = false;
                            queryResult.Status = "Duplicate entry";
                            queryResult.Error = ex.Message;
                            result.Details.Add($" Duplicate entry: {ex.Message}");
                            result.DuplicateErrors++;
                        }
                        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.ParseError)
                        {
                            queryResult.Success = false;
                            queryResult.Status = "Syntax error";
                            queryResult.Error = ex.Message;
                            result.Details.Add($" Syntax Error: {ex.Message}");
                            result.FailedQueries++;
                        }
                        catch (MySqlException ex) when (ex.Number == 1054)
                        {
                            queryResult.Success = false;
                            queryResult.Status = "Unknown column";
                            queryResult.Error = ex.Message;
                            result.Details.Add($" Unknown column: {ex.Message}");
                            result.FailedQueries++;
                        }
                        catch (MySqlException ex) when (
                            ex.ErrorCode == MySqlErrorCode.StoredProcedureDoesNotExist ||
                            ex.ErrorCode == MySqlErrorCode.UnknownProcedure ||
                            ex.ErrorCode == MySqlErrorCode.FunctionNotDefined)
                        {
                            queryResult.Success = false;
                            queryResult.Status = "Routine error";
                            queryResult.Error = ex.Message;
                            result.Details.Add($" {ex.Message}");
                            result.FailedQueries++;
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("DELIMITER", StringComparison.OrdinalIgnoreCase))
                            {
                                queryResult.Success = false;
                                queryResult.Status = "Skipped";
                                queryResult.Error = "DELIMITER statement skipped (MySqlConnector limitation)";
                                result.Details.Add(" DELIMITER statement skipped (MySqlConnector limitation)");
                                result.QueryResults.Add(queryResult);
                                continue;
                            }

                            queryResult.Success = false;
                            queryResult.Status = "Error";
                            queryResult.Error = ex.Message;
                            result.Details.Add($" Error: {ex.Message}");
                            result.FailedQueries++;
                        }

                        result.QueryResults.Add(queryResult);
                    }
                }
            }
            catch (MySqlException ex) when (ex.Number == 0)
            {
                result.Error = $"Cannot connect to {dbName}: {ex.Message}";
                result.Connected = false;
                result.QueryResults = CreateNotRunResults(queries, result.Error);
            }
            catch (Exception ex)
            {
                result.Error = $"Error with {dbName}: {ex.Message}";
                result.QueryResults = CreateNotRunResults(queries, result.Error);
            }

            return result;
        }

        private static bool IsUseDatabaseStatement(string query)
        {
            return Regex.IsMatch(query.TrimStart(), @"^USE\s+[`""]?[\w$]+[`""]?\s*$", RegexOptions.IgnoreCase);
        }

        private static string MakeInsertDuplicateSafe(string query)
        {
            var trimmed = query.TrimStart();
            if (!Regex.IsMatch(trimmed, @"^INSERT\s+", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(trimmed, @"^INSERT\s+IGNORE\s+", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(trimmed, @"\sON\s+DUPLICATE\s+KEY\s+UPDATE\s", RegexOptions.IgnoreCase))
            {
                return query;
            }

            var leadingWhitespaceLength = query.Length - trimmed.Length;
            return query[..leadingWhitespaceLength] + Regex.Replace(trimmed, @"^INSERT\s+", "INSERT IGNORE ", RegexOptions.IgnoreCase);
        }

        private static List<QueryExecutionDetail> CreateNotRunResults(List<string> queries, string reason)
        {
            return queries
                .Select((query, index) => new QueryExecutionDetail
                {
                    QueryNumber = index + 1,
                    QueryText = query,
                    Success = false,
                    Status = "Not run",
                    Error = reason
                })
                .ToList();
        }

        private async Task<MySqlConnection> OpenConnectionAsync(string connectionString)
        {
            var connection = new MySqlConnection(connectionString);

            try
            {
                await connection.OpenAsync();
                return connection;
            }
            catch (MySqlException ex) when (ex.Number == 1049)
            {
                await connection.DisposeAsync();

                var builder = new MySqlConnectionStringBuilder(connectionString);
                var databaseName = builder.Database;

                if (string.IsNullOrWhiteSpace(databaseName))
                {
                    throw;
                }

                builder.Database = string.Empty;
                await using (var serverConnection = new MySqlConnection(builder.ConnectionString))
                {
                    await serverConnection.OpenAsync();
                    var quotedDatabaseName = QuoteIdentifier(databaseName);
                    await using var createCommand = new MySqlCommand($"CREATE DATABASE IF NOT EXISTS {quotedDatabaseName};", serverConnection);
                    await createCommand.ExecuteNonQueryAsync();
                }

                connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                return connection;
            }
        }

        private static string QuoteIdentifier(string identifier)
        {
            return $"`{identifier.Replace("`", "``")}`";
        }

        private List<string> ParseSqlQueries(string sqlContent)
        {
            var queries = new List<string>();

            sqlContent = RemoveComments(sqlContent);
            sqlContent = Regex.Replace(sqlContent, @"\r\n|\r|\n", "\n");

            var delimiter = ";";
            var currentQuery = new StringBuilder();

            foreach (var line in sqlContent.Split('\n'))
            {
                var trimmedLine = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    continue;
                }

                if (trimmedLine.StartsWith("DELIMITER", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = trimmedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        delimiter = parts[1];
                    }
                    continue;
                }

                if (currentQuery.Length > 0)
                {
                    currentQuery.Append('\n');
                }
                currentQuery.Append(line);

                var queryText = currentQuery.ToString().Trim();
                if (!queryText.EndsWith(delimiter, StringComparison.Ordinal))
                {
                    continue;
                }

                queryText = queryText[..^delimiter.Length].Trim();
                if (!string.IsNullOrWhiteSpace(queryText))
                {
                    queries.Add(queryText);
                }
                currentQuery.Clear();
            }

            var finalQuery = currentQuery.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(finalQuery))
            {
                queries.Add(finalQuery);
            }

            return queries;
        }

        private string RemoveComments(string sql)
        {
            sql = Regex.Replace(sql, @"--.*?(?=\n|$)", string.Empty, RegexOptions.Multiline);
            sql = Regex.Replace(sql, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            sql = Regex.Replace(sql, @"#.*?(?=\n|$)", string.Empty, RegexOptions.Multiline);
            return sql;
        }

        public async Task<SchemaListResult> GetSchemasAsync(string connectionString)
        {
            var result = new SchemaListResult();

            try
            {
                var builder = new MySqlConnectionStringBuilder(connectionString);
                builder.Database = string.Empty;

                await using var connection = await OpenConnectionAsync(builder.ConnectionString);
                var schemas = new List<string>();

                const string schemaQuery = """
                    SELECT SCHEMA_NAME
                    FROM information_schema.SCHEMATA
                    ORDER BY SCHEMA_NAME;
                    """;

                await using (var command = new MySqlCommand(schemaQuery, connection))
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        schemas.Add(reader.GetString(0));
                    }
                }

                var excluded = new[] { "information_schema", "mysql", "performance_schema", "sys" };
                result.Schemas = schemas
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(s => !excluded.Contains(s, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (result.Schemas.Count == 0)
                {
                    result.Schemas = schemas;
                }

                result.HasError = false;
                result.Message = "Schemas loaded successfully.";
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.Message = ex.Message;
            }

            return result;
        }

        public async Task<SchemaExportResult> ExportSchemasAsync(string connectionString)
        {
            var result = new SchemaExportResult();

            try
            {
                var builder = new MySqlConnectionStringBuilder(connectionString);
                builder.Database = string.Empty;

                await using var connection = await OpenConnectionAsync(builder.ConnectionString);
                var schemas = new List<string>();

                await using (var command = new MySqlCommand("SHOW DATABASES;", connection))
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        schemas.Add(reader.GetString(0));
                    }
                }

                if (schemas.Count == 0)
                {
                    result.HasError = true;
                    result.Message = "No databases were returned from the server.";
                    return result;
                }

                var excluded = new[] { "information_schema", "mysql", "performance_schema", "sys" };
                var exportSchemas = schemas
                    .Where(s => !excluded.Contains(s, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (exportSchemas.Count == 0)
                {
                    exportSchemas = schemas;
                }

                var scriptBuilder = new StringBuilder();
                scriptBuilder.AppendLine("-- Generated schema export file");
                scriptBuilder.AppendLine($"-- Export date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                scriptBuilder.AppendLine();

                foreach (var schema in exportSchemas)
                {
                    var quotedName = QuoteIdentifier(schema);
                    scriptBuilder.AppendLine($"CREATE DATABASE IF NOT EXISTS {quotedName};");
                }

                scriptBuilder.AppendLine();
                scriptBuilder.AppendLine("-- End of schema export");

                result.HasError = false;
                result.Message = "Schema export script generated successfully.";
                result.Schemas = exportSchemas;
                result.SqlScript = scriptBuilder.ToString();
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.Message = ex.Message;
            }

            return result;
        }

        public async Task<bool> TestConnectionAsync(string connectionString)
        {
            try
            {
                await using var connection = await OpenConnectionAsync(connectionString);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public record DatabaseTarget(string Name, string ConnectionString, string Key = "");

    public class SchemaListResult
    {
        public bool HasError { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Schemas { get; set; } = new();
    }

    public class SchemaExportResult
    {
        public bool HasError { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Schemas { get; set; } = new();
        public string SqlScript { get; set; } = string.Empty;
    }

    public class DatabaseTargetStatus
    {
        public DatabaseTarget Target { get; set; } = new(string.Empty, string.Empty);
        public bool Connected { get; set; }
    }

    public class ExecutionResult
    {
        public bool HasError { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalQueries { get; set; }
        public List<UploadedQuery> Queries { get; set; } = new();
        public List<DatabaseExecutionResult> DatabaseResults { get; set; } = new();
    }

    public class UploadedQuery
    {
        public int Number { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class DatabaseExecutionResult
    {
        public string DatabaseName { get; set; } = string.Empty;
        public bool Connected { get; set; }
        public string Error { get; set; } = string.Empty;
        public int SuccessfulQueries { get; set; }
        public int FailedQueries { get; set; }
        public int DuplicateErrors { get; set; }
        public List<string> Details { get; set; } = new();
        public List<QueryExecutionDetail> QueryResults { get; set; } = new();
    }

    public class QueryExecutionDetail
    {
        public int QueryNumber { get; set; }
        public string QueryText { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Status { get; set; } = "Not run";
        public int RowsAffected { get; set; }
        public string Error { get; set; } = string.Empty;
    }
}
