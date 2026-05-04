namespace fromApp
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label labelSqlFile;
        private System.Windows.Forms.TextBox textBoxSqlPath;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.Label labelHost;
        private System.Windows.Forms.TextBox textBoxHost;
        private System.Windows.Forms.Label labelPort;
        private System.Windows.Forms.TextBox textBoxPort;
        private System.Windows.Forms.Label labelDatabase;
        private System.Windows.Forms.TextBox textBoxDatabase;
        private System.Windows.Forms.Label labelUser;
        private System.Windows.Forms.TextBox textBoxUser;
        private System.Windows.Forms.Label labelPassword;
        private System.Windows.Forms.TextBox textBoxPassword;
        private System.Windows.Forms.Button buttonAddConnection;
        private System.Windows.Forms.Button buttonRemoveSelected;
        private System.Windows.Forms.Label labelTargetList;
        private System.Windows.Forms.DataGridView dataGridViewTargets;
        private System.Windows.Forms.Button buttonTestConnections;
        private System.Windows.Forms.Button buttonExecute;
        private System.Windows.Forms.TextBox textBoxResults;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            labelSqlFile = new Label();
            textBoxSqlPath = new TextBox();
            buttonBrowse = new Button();
            labelHost = new Label();
            textBoxHost = new TextBox();
            labelPort = new Label();
            textBoxPort = new TextBox();
            labelDatabase = new Label();
            textBoxDatabase = new TextBox();
            labelUser = new Label();
            textBoxUser = new TextBox();
            labelPassword = new Label();
            textBoxPassword = new TextBox();
            buttonAddConnection = new Button();
            buttonRemoveSelected = new Button();
            labelTargetList = new Label();
            dataGridViewTargets = new DataGridView();
            buttonTestConnections = new Button();
            buttonExecute = new Button();
            textBoxResults = new TextBox();
            ((System.ComponentModel.ISupportInitialize)dataGridViewTargets).BeginInit();
            SuspendLayout();
            // 
            // labelSqlFile
            // 
            labelSqlFile.AutoSize = true;
            labelSqlFile.Location = new Point(12, 9);
            labelSqlFile.Name = "labelSqlFile";
            labelSqlFile.Size = new Size(52, 15);
            labelSqlFile.TabIndex = 0;
            labelSqlFile.Text = "SQL File:";
            // 
            // textBoxSqlPath
            // 
            textBoxSqlPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxSqlPath.Location = new Point(12, 27);
            textBoxSqlPath.Name = "textBoxSqlPath";
            textBoxSqlPath.Size = new Size(600, 23);
            textBoxSqlPath.TabIndex = 1;
            // 
            // buttonBrowse
            // 
            buttonBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonBrowse.Location = new Point(620, 26);
            buttonBrowse.Name = "buttonBrowse";
            buttonBrowse.Size = new Size(150, 25);
            buttonBrowse.TabIndex = 2;
            buttonBrowse.Text = "Browse...";
            buttonBrowse.UseVisualStyleBackColor = true;
            buttonBrowse.Click += ButtonBrowse_Click;
            // 
            // labelHost
            // 
            labelHost.AutoSize = true;
            labelHost.Location = new Point(12, 70);
            labelHost.Name = "labelHost";
            labelHost.Size = new Size(35, 15);
            labelHost.TabIndex = 3;
            labelHost.Text = "Host:";
            // 
            // textBoxHost
            // 
            textBoxHost.Location = new Point(120, 67);
            textBoxHost.Name = "textBoxHost";
            textBoxHost.Size = new Size(220, 23);
            textBoxHost.TabIndex = 4;
            textBoxHost.Text = "127.0.0.1";
            // 
            // labelPort
            // 
            labelPort.AutoSize = true;
            labelPort.Location = new Point(360, 70);
            labelPort.Name = "labelPort";
            labelPort.Size = new Size(32, 15);
            labelPort.TabIndex = 5;
            labelPort.Text = "Port:";
            // 
            // textBoxPort
            // 
            textBoxPort.Location = new Point(420, 67);
            textBoxPort.Name = "textBoxPort";
            textBoxPort.Size = new Size(100, 23);
            textBoxPort.TabIndex = 6;
            textBoxPort.Text = "3306";
            // 
            // labelDatabase
            // 
            labelDatabase.AutoSize = true;
            labelDatabase.Location = new Point(12, 110);
            labelDatabase.Name = "labelDatabase";
            labelDatabase.Size = new Size(63, 15);
            labelDatabase.TabIndex = 7;
            labelDatabase.Text = "Database:";
            // 
            // textBoxDatabase
            // 
            textBoxDatabase.Location = new Point(120, 107);
            textBoxDatabase.Name = "textBoxDatabase";
            textBoxDatabase.Size = new Size(220, 23);
            textBoxDatabase.TabIndex = 8;
            // 
            // labelUser
            // 
            labelUser.AutoSize = true;
            labelUser.Location = new Point(360, 110);
            labelUser.Name = "labelUser";
            labelUser.Size = new Size(33, 15);
            labelUser.TabIndex = 9;
            labelUser.Text = "User:";
            // 
            // textBoxUser
            // 
            textBoxUser.Location = new Point(420, 107);
            textBoxUser.Name = "textBoxUser";
            textBoxUser.Size = new Size(150, 23);
            textBoxUser.TabIndex = 10;
            textBoxUser.Text = "root";
            // 
            // labelPassword
            // 
            labelPassword.AutoSize = true;
            labelPassword.Location = new Point(12, 150);
            labelPassword.Name = "labelPassword";
            labelPassword.Size = new Size(60, 15);
            labelPassword.TabIndex = 11;
            labelPassword.Text = "Password:";
            // 
            // textBoxPassword
            // 
            textBoxPassword.Location = new Point(120, 147);
            textBoxPassword.Name = "textBoxPassword";
            textBoxPassword.PasswordChar = '*';
            textBoxPassword.Size = new Size(220, 23);
            textBoxPassword.TabIndex = 12;
            // 
            // buttonAddConnection
            // 
            buttonAddConnection.Location = new Point(360, 145);
            buttonAddConnection.Name = "buttonAddConnection";
            buttonAddConnection.Size = new Size(120, 25);
            buttonAddConnection.TabIndex = 13;
            buttonAddConnection.Text = "Add Connection";
            buttonAddConnection.UseVisualStyleBackColor = true;
            buttonAddConnection.Click += ButtonAddConnection_Click;
            // 
            // buttonRemoveSelected
            // 
            buttonRemoveSelected.Location = new Point(490, 145);
            buttonRemoveSelected.Name = "buttonRemoveSelected";
            buttonRemoveSelected.Size = new Size(120, 25);
            buttonRemoveSelected.TabIndex = 14;
            buttonRemoveSelected.Text = "Remove Selected";
            buttonRemoveSelected.UseVisualStyleBackColor = true;
            buttonRemoveSelected.Click += ButtonRemoveSelected_Click;
            // 
            // labelTargetList
            // 
            labelTargetList.AutoSize = true;
            labelTargetList.Location = new Point(12, 190);
            labelTargetList.Name = "labelTargetList";
            labelTargetList.Size = new Size(87, 15);
            labelTargetList.TabIndex = 15;
            labelTargetList.Text = "Target servers:";
            // 
            // dataGridViewTargets
            // 
            dataGridViewTargets.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewTargets.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewTargets.Location = new Point(12, 210);
            dataGridViewTargets.Name = "dataGridViewTargets";
            dataGridViewTargets.RowHeadersVisible = false;
            dataGridViewTargets.Size = new Size(760, 180);
            dataGridViewTargets.TabIndex = 16;
            // 
            // buttonTestConnections
            // 
            buttonTestConnections.Location = new Point(12, 400);
            buttonTestConnections.Name = "buttonTestConnections";
            buttonTestConnections.Size = new Size(150, 25);
            buttonTestConnections.TabIndex = 17;
            buttonTestConnections.Text = "Test Connections";
            buttonTestConnections.UseVisualStyleBackColor = true;
            buttonTestConnections.Click += ButtonTestConnections_Click;
            // 
            // buttonExecute
            // 
            buttonExecute.Location = new Point(180, 400);
            buttonExecute.Name = "buttonExecute";
            buttonExecute.Size = new Size(150, 25);
            buttonExecute.TabIndex = 18;
            buttonExecute.Text = "Execute SQL";
            buttonExecute.UseVisualStyleBackColor = true;
            buttonExecute.Click += ButtonExecute_Click;
            // 
            // textBoxResults
            // 
            textBoxResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxResults.Location = new Point(12, 440);
            textBoxResults.Multiline = true;
            textBoxResults.Name = "textBoxResults";
            textBoxResults.ReadOnly = true;
            textBoxResults.ScrollBars = ScrollBars.Vertical;
            textBoxResults.Size = new Size(760, 180);
            textBoxResults.TabIndex = 19;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 640);
            Controls.Add(textBoxResults);
            Controls.Add(buttonExecute);
            Controls.Add(buttonTestConnections);
            Controls.Add(dataGridViewTargets);
            Controls.Add(labelTargetList);
            Controls.Add(buttonRemoveSelected);
            Controls.Add(buttonAddConnection);
            Controls.Add(textBoxPassword);
            Controls.Add(labelPassword);
            Controls.Add(textBoxUser);
            Controls.Add(labelUser);
            Controls.Add(textBoxDatabase);
            Controls.Add(labelDatabase);
            Controls.Add(textBoxPort);
            Controls.Add(labelPort);
            Controls.Add(textBoxHost);
            Controls.Add(labelHost);
            Controls.Add(buttonBrowse);
            Controls.Add(textBoxSqlPath);
            Controls.Add(labelSqlFile);
            Name = "Form1";
            Text = "MySQL SQL Executor";
            ((System.ComponentModel.ISupportInitialize)dataGridViewTargets).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
