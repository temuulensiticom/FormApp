using fromApp.Services;

namespace fromApp
{
    public partial class Form1 : Form
    {
        private readonly DatabaseService _databaseService;
        private readonly FileUploadService _fileUploadService;

        public Form1()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _fileUploadService = new FileUploadService();
            InitializeTargetGrid();
        }

        private void InitializeTargetGrid()
        {
            dataGridViewTargets.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Selected",
                HeaderText = "Use",
                Width = 50
            });
            dataGridViewTargets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Host",
                HeaderText = "Host",
                Width = 150
            });
            dataGridViewTargets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Port",
                HeaderText = "Port",
                Width = 70
            });
            dataGridViewTargets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Database",
                HeaderText = "Database",
                Width = 140
            });
            dataGridViewTargets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "User",
                HeaderText = "User",
                Width = 120
            });
            dataGridViewTargets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Password",
                HeaderText = "Password",
                Width = 160
            });
        }

        private void AddTargetRow(string host, int port, string database, string user, string password)
        {
            dataGridViewTargets.Rows.Add(true, host, port.ToString(), database, user, password);
        }

        private void ButtonBrowse_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog();
            dialog.Filter = "SQL files (*.sql)|*.sql|Text files (*.txt)|*.txt|All files (*.*)|*.*";
            dialog.Title = "Select SQL File";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBoxSqlPath.Text = dialog.FileName;
            }
        }

        private async void ButtonTestConnections_Click(object sender, EventArgs e)
        {
            textBoxResults.Clear();
            var targets = GetSelectedTargets();
            if (targets.Count == 0)
            {
                AppendResult("No database connections selected.");
                return;
            }

            AppendResult("Testing selected database connections...");
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var connected = await _databaseService.TestConnectionAsync(target.ConnectionString);
                AppendResult($"{target.Name}: {(connected ? "Connected" : "Failed")}");
            }
        }

        private async void ButtonExecute_Click(object sender, EventArgs e)
        {
            textBoxResults.Clear();

            if (string.IsNullOrWhiteSpace(textBoxSqlPath.Text) || !File.Exists(textBoxSqlPath.Text))
            {
                AppendResult("Please select a valid SQL file.");
                return;
            }

            var targets = GetSelectedTargets();
            if (targets.Count == 0)
            {
                AppendResult("Please select at least one database connection.");
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

            AppendResult($"Loaded file: {uploadResult.FileName}");
            AppendResult("Executing SQL queries...");

            var executionResult = await _databaseService.ExecuteQueriesAsync(uploadResult.Content, targets);
            AppendResult($"Result: {(executionResult.HasError ? "Completed with errors" : "Completed successfully")}");
            AppendResult(executionResult.Message);
            AppendResult($"Total queries: {executionResult.TotalQueries}");

            foreach (var databaseResult in executionResult.DatabaseResults)
            {
                AppendResult($"--- Database: {databaseResult.DatabaseName}");
                AppendResult($"Connected: {databaseResult.Connected}");
                if (!string.IsNullOrEmpty(databaseResult.Error))
                {
                    AppendResult($"Error: {databaseResult.Error}");
                }
                AppendResult($"Successful queries: {databaseResult.SuccessfulQueries}");
                AppendResult($"Failed queries: {databaseResult.FailedQueries}");
                AppendResult($"Duplicate errors: {databaseResult.DuplicateErrors}");
                foreach (var detail in databaseResult.Details)
                {
                    AppendResult(detail.Trim());
                }
            }
        }

        private void ButtonAddConnection_Click(object sender, EventArgs e)
        {
            var port = 3306;
            if (!int.TryParse(textBoxPort.Text, out port) || port <= 0)
            {
                port = 3306;
            }

            AddTargetRow(textBoxHost.Text, port, textBoxDatabase.Text, textBoxUser.Text, textBoxPassword.Text);
        }

        private void ButtonRemoveSelected_Click(object sender, EventArgs e)
        {
            for (var i = dataGridViewTargets.Rows.Count - 1; i >= 0; i--)
            {
                var row = dataGridViewTargets.Rows[i];
                if (row.IsNewRow)
                {
                    continue;
                }

                if (row.Cells["Selected"].Value is bool selected && selected)
                {
                    dataGridViewTargets.Rows.RemoveAt(i);
                }
            }
        }
        private void buttonRun_Click(object sender, EventArgs e)
        {
            ButtonExecute_Click(sender, e); // reuse your existing logic
        }

        private List<DatabaseTarget> GetSelectedTargets()
        {
            var targets = new List<DatabaseTarget>();
            foreach (DataGridViewRow row in dataGridViewTargets.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                var cellValue = row.Cells["Selected"].Value;
                var selected = cellValue is bool isSelected && isSelected;
                if (!selected)
                {
                    continue;
                }

                var host = row.Cells["Host"].Value?.ToString() ?? string.Empty;
                var portText = row.Cells["Port"].Value?.ToString() ?? string.Empty;
                var database = row.Cells["Database"].Value?.ToString() ?? string.Empty;
                var user = row.Cells["User"].Value?.ToString() ?? string.Empty;
                var password = row.Cells["Password"].Value?.ToString() ?? string.Empty;

                var port = 3306;
                if (!int.TryParse(portText, out port) || port <= 0)
                {
                    port = 3306;
                }

                targets.Add(_databaseService.CreateCustomTarget($"{database} ({host}:{port})", host, port, database, user, password));
            }

            return targets;
        }

        private void AppendResult(string message)
        {
            textBoxResults.AppendText(message + Environment.NewLine);
        }
    }
}
