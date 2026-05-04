using fromApp.Services;

namespace fromApp
{
    public partial class Form1 : Form
    {
        private const int DefaultPort = 3008;
        private const string DefaultUser = "root";
        private const string DefaultPassword = "@passmysql";
        private static readonly string[] DefaultHosts = ["127.0.0.1", "localhost"];

        private readonly DatabaseService _databaseService;
        private readonly FileUploadService _fileUploadService;

        public Form1()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _fileUploadService = new FileUploadService();
        }

        private void ButtonBrowse_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog();
            dialog.Filter = "SQL files (*.sql)|*.sql|Text files (*.txt)|*.txt|All files (*.*)|*.*";
            dialog.Title = "Select SQL File";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBoxSqlPath.Text = dialog.FileName;
                PreviewSelectedFile(dialog.FileName);
            }
        }

        private async void ButtonExecute_Click(object sender, EventArgs e)
        {
            textBoxResults.Clear();

            if (string.IsNullOrWhiteSpace(textBoxSqlPath.Text) || !File.Exists(textBoxSqlPath.Text))
            {
                AppendResult("Please upload a valid SQL file.");
                return;
            }

            var uploadResult = await _fileUploadService.UploadAndReadFileAsync(textBoxSqlPath.Text);
            if (uploadResult.HasError)
            {
                AppendResult($"Upload error: {uploadResult.Message}");
                return;
            }

            if (uploadResult.IsDuplicate)
            {
                AppendResult(uploadResult.DuplicateMessage);
            }

            AppendResult($"Uploaded file: {uploadResult.FileName}");
            AppendResult("Connecting to default server and loading schemas...");

            var serverResult = await LoadSchemasFromDefaultServerAsync();
            var schemasResult = serverResult.SchemaResult;
            if (schemasResult.HasError)
            {
                AppendResult($"Schema load failed: {schemasResult.Message}");
                return;
            }

            var targets = schemasResult.Schemas
                .Select(schema => _databaseService.CreateSchemaTarget(
                    schema,
                    serverResult.Host,
                    DefaultPort,
                    DefaultUser,
                    DefaultPassword))
                .ToList();

            if (targets.Count == 0)
            {
                AppendResult("No schemas found on the default server.");
                return;
            }

            AppendResult($"Default server: {serverResult.Host}:{DefaultPort}");
            AppendResult($"Schemas loaded: {targets.Count}");
            AppendResult("Schemas:");
            foreach (var target in targets)
            {
                AppendResult($" - {target.Name}");
            }
            if (schemasResult.SkippedSchemas.Count > 0)
            {
                AppendResult("Skipped system schemas:");
                foreach (var schema in schemasResult.SkippedSchemas)
                {
                    AppendResult($" - {schema}");
                }
            }
            AppendResult("Running SQL file...");

            var executionResult = await _databaseService.ExecuteQueriesAsync(uploadResult.Content, targets);
            AppendResult("");
            AppendResult($"Result: {(executionResult.HasError ? "Completed with errors" : "Completed successfully")}");
            AppendResult($"Total queries: {executionResult.TotalQueries}");

            foreach (var databaseResult in executionResult.DatabaseResults)
            {
                AppendResult("");
                AppendResult($"Schema: {databaseResult.DatabaseName}");
                AppendResult($"Connected: {databaseResult.Connected}");

                if (!string.IsNullOrWhiteSpace(databaseResult.Error))
                {
                    AppendResult($"Error: {databaseResult.Error}");
                }

                AppendResult($"Executed: {databaseResult.SuccessfulQueries}");
                AppendResult($"Failed: {databaseResult.FailedQueries}");
                AppendResult($"Duplicates: {databaseResult.DuplicateErrors}");

                foreach (var queryResult in databaseResult.QueryResults)
                {
                    AppendQueryResult(queryResult);
                }
            }
        }

        private async Task<(string Host, SchemaListResult SchemaResult)> LoadSchemasFromDefaultServerAsync()
        {
            var failures = new List<string>();

            foreach (var host in DefaultHosts)
            {
                var serverTarget = _databaseService.CreateDefaultServerTarget(
                    host,
                    DefaultPort,
                    DefaultUser,
                    DefaultPassword);

                var schemasResult = await _databaseService.GetSchemasAsync(serverTarget.ConnectionString);
                if (!schemasResult.HasError)
                {
                    return (host, schemasResult);
                }

                failures.Add($"{host}:{DefaultPort} -> {schemasResult.Message}");
            }

            return (DefaultHosts[0], new SchemaListResult
            {
                HasError = true,
                Message = string.Join(Environment.NewLine, failures)
            });
        }

        private void AppendQueryResult(QueryExecutionDetail queryResult)
        {
            AppendResult($"Query {queryResult.QueryNumber}: {queryResult.Status}");
            AppendResult(ShortenQuery(queryResult.QueryText));

            if (queryResult.Success)
            {
                AppendResult($"Rows affected: {queryResult.RowsAffected}");
            }
            else if (!string.IsNullOrWhiteSpace(queryResult.Error))
            {
                AppendResult($"Failed reason: {queryResult.Error}");
            }

            AppendResult("");
        }

        private static string ShortenQuery(string query)
        {
            var compact = string.Join(" ", query.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            return compact.Length <= 300 ? compact : compact[..300] + "...";
        }

        private void PreviewSelectedFile(string filePath)
        {
            try
            {
                textBoxResults.Clear();
                AppendResult($"Selected file: {filePath}");
                AppendResult("");
                AppendResult(File.ReadAllText(filePath));
            }
            catch (Exception ex)
            {
                textBoxResults.Clear();
                AppendResult($"Error reading selected file: {ex.Message}");
            }
        }

        private void AppendResult(string message)
        {
            textBoxResults.AppendText(message + Environment.NewLine);
        }
    }
}
