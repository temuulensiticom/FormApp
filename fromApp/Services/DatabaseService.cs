using MySqlConnector;
using System.Text;
using System.Text.RegularExpressions;

namespace fromApp.Services
{
    public class DatabaseService
    {
        public DatabaseTarget CreateCustomTarget(string name, string host, int port, string database, string user, string password)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host.Trim(),
                Port = (uint)(port > 0 ? port : 3306),
                Database = database.Trim(),
                UserID = user.Trim(),
                Password = password ?? string.Empty,
                ConnectionProtocol = MySqlConnectionProtocol.Tcp,
                SslMode = MySqlSslMode.None,
                AllowPublicKeyRetrieval = true,
                ConnectionTimeout = 10
            };

            var displayName = string.IsNullOrWhiteSpace(name)
                ? $"{builder.Database} ({builder.Server}:{builder.Port})"
                : name.Trim();

            return new DatabaseTarget(displayName, builder.ConnectionString);
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

                            using (var command = new MySqlCommand(query, connection))
                            {
                                command.CommandTimeout = 300;
                                var rowsAffected = await command.ExecuteNonQueryAsync();
                                result.SuccessfulQueries++;
                                queryResult.Success = true;
                                queryResult.Status = "Executed";
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
            }
            catch (Exception ex)
            {
                result.Error = $"Error with {dbName}: {ex.Message}";
            }

            return result;
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
            var lines = sqlContent.Split('\n');
            var inStoredObject = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                if (trimmedLine.StartsWith("DELIMITER", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = trimmedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        delimiter = parts[1];
                    }
                    continue;
                }

                var upperLine = trimmedLine.ToUpperInvariant();
                if (!inStoredObject && (upperLine.Contains("CREATE PROCEDURE") ||
                                       upperLine.Contains("CREATE FUNCTION") ||
                                       upperLine.Contains("CREATE TRIGGER") ||
                                       upperLine.Contains("CREATE EVENT")))
                {
                    inStoredObject = true;
                }
                else if (!inStoredObject && (upperLine.StartsWith("DROP PROCEDURE") ||
                                            upperLine.StartsWith("DROP FUNCTION") ||
                                            upperLine.StartsWith("DROP TRIGGER") ||
                                            upperLine.StartsWith("DROP EVENT")))
                {
                    var dropQuery = trimmedLine;
                    if (dropQuery.EndsWith(delimiter, StringComparison.Ordinal))
                    {
                        dropQuery = dropQuery.Substring(0, dropQuery.Length - delimiter.Length);
                    }
                    if (!string.IsNullOrWhiteSpace(dropQuery))
                    {
                        queries.Add(dropQuery.Trim());
                    }
                    delimiter = ";";
                    continue;
                }

                if (currentQuery.Length > 0)
                    currentQuery.Append("\n");

                currentQuery.Append(line);

                var isEndOfQuery = false;
                if (inStoredObject)
                {
                    if (upperLine.StartsWith("END") && trimmedLine.EndsWith(delimiter, StringComparison.Ordinal))
                    {
                        isEndOfQuery = true;
                        inStoredObject = false;
                    }
                }
                else
                {
                    if (trimmedLine.EndsWith(delimiter, StringComparison.Ordinal))
                    {
                        isEndOfQuery = true;
                    }
                }

                if (isEndOfQuery)
                {
                    var query = currentQuery.ToString().Trim();
                    if (query.EndsWith(delimiter, StringComparison.Ordinal))
                    {
                        query = query.Substring(0, query.Length - delimiter.Length);
                    }
                    query = query.Trim();
                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        queries.Add(query);
                    }
                    currentQuery.Clear();
                    delimiter = ";";
                }
            }

            if (currentQuery.Length > 0)
            {
                var query = currentQuery.ToString().Trim();
                if (query.EndsWith(delimiter, StringComparison.Ordinal))
                {
                    query = query.Substring(0, query.Length - delimiter.Length);
                }
                query = query.Trim();
                if (!string.IsNullOrWhiteSpace(query))
                {
                    queries.Add(query);
                }
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
