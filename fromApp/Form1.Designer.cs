namespace fromApp
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label labelSqlFile;
        private System.Windows.Forms.TextBox textBoxSqlPath;
        private System.Windows.Forms.Button buttonBrowse;
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
            buttonExecute = new Button();
            textBoxResults = new TextBox();
            SuspendLayout();
            // 
            // labelSqlFile
            // 
            labelSqlFile.AutoSize = true;
            labelSqlFile.Location = new Point(12, 14);
            labelSqlFile.Name = "labelSqlFile";
            labelSqlFile.Size = new Size(52, 15);
            labelSqlFile.TabIndex = 0;
            labelSqlFile.Text = "SQL File:";
            // 
            // textBoxSqlPath
            // 
            textBoxSqlPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxSqlPath.Location = new Point(12, 36);
            textBoxSqlPath.Name = "textBoxSqlPath";
            textBoxSqlPath.ReadOnly = true;
            textBoxSqlPath.Size = new Size(516, 23);
            textBoxSqlPath.TabIndex = 1;
            // 
            // buttonBrowse
            // 
            buttonBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonBrowse.Location = new Point(540, 35);
            buttonBrowse.Name = "buttonBrowse";
            buttonBrowse.Size = new Size(120, 25);
            buttonBrowse.TabIndex = 2;
            buttonBrowse.Text = "Upload File";
            buttonBrowse.UseVisualStyleBackColor = true;
            buttonBrowse.Click += ButtonBrowse_Click;
            // 
            // buttonExecute
            // 
            buttonExecute.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonExecute.Location = new Point(668, 35);
            buttonExecute.Name = "buttonExecute";
            buttonExecute.Size = new Size(120, 25);
            buttonExecute.TabIndex = 3;
            buttonExecute.Text = "Upload and Run";
            buttonExecute.UseVisualStyleBackColor = true;
            buttonExecute.Click += ButtonExecute_Click;
            // 
            // textBoxResults
            // 
            textBoxResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxResults.Font = new Font("Consolas", 10F);
            textBoxResults.Location = new Point(12, 76);
            textBoxResults.Multiline = true;
            textBoxResults.Name = "textBoxResults";
            textBoxResults.ReadOnly = true;
            textBoxResults.ScrollBars = ScrollBars.Both;
            textBoxResults.Size = new Size(776, 522);
            textBoxResults.TabIndex = 4;
            textBoxResults.WordWrap = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 610);
            Controls.Add(textBoxResults);
            Controls.Add(buttonExecute);
            Controls.Add(buttonBrowse);
            Controls.Add(textBoxSqlPath);
            Controls.Add(labelSqlFile);
            Name = "Form1";
            Text = "MySQL SQL Runner";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
