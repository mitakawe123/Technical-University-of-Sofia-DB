namespace UI
{
    partial class CreateTableForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            CreateTableButton = new Button();
            TableNameLabel = new Label();
            TableNameInput = new TextBox();
            ColumnNumberDropdown = new ComboBox();
            NumberOfColumnsLabel = new Label();
            SuspendLayout();
            // 
            // CreateTableButton
            // 
            CreateTableButton.Location = new Point(373, 476);
            CreateTableButton.Margin = new Padding(3, 4, 3, 4);
            CreateTableButton.Name = "CreateTableButton";
            CreateTableButton.Size = new Size(139, 108);
            CreateTableButton.TabIndex = 0;
            CreateTableButton.Text = "Create Table";
            CreateTableButton.UseVisualStyleBackColor = true;
            CreateTableButton.Click += CreateTableButton_Click;
            // 
            // TableNameLabel
            // 
            TableNameLabel.AutoSize = true;
            TableNameLabel.Location = new Point(59, 12);
            TableNameLabel.Name = "TableNameLabel";
            TableNameLabel.Size = new Size(88, 20);
            TableNameLabel.TabIndex = 1;
            TableNameLabel.Text = "Table Name";
            // 
            // TableNameInput
            // 
            TableNameInput.Location = new Point(166, 8);
            TableNameInput.Margin = new Padding(3, 4, 3, 4);
            TableNameInput.Name = "TableNameInput";
            TableNameInput.Size = new Size(298, 27);
            TableNameInput.TabIndex = 2;
            // 
            // ColumnNumberDropdown
            // 
            ColumnNumberDropdown.FormattingEnabled = true;
            ColumnNumberDropdown.Location = new Point(657, 8);
            ColumnNumberDropdown.Margin = new Padding(3, 4, 3, 4);
            ColumnNumberDropdown.Name = "ColumnNumberDropdown";
            ColumnNumberDropdown.Size = new Size(138, 28);
            ColumnNumberDropdown.TabIndex = 3;
            // 
            // NumberOfColumnsLabel
            // 
            NumberOfColumnsLabel.AutoSize = true;
            NumberOfColumnsLabel.Location = new Point(505, 12);
            NumberOfColumnsLabel.Name = "NumberOfColumnsLabel";
            NumberOfColumnsLabel.Size = new Size(144, 20);
            NumberOfColumnsLabel.TabIndex = 4;
            NumberOfColumnsLabel.Text = "Number Of Columns";
            // 
            // CreateTableForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(914, 600);
            Controls.Add(NumberOfColumnsLabel);
            Controls.Add(ColumnNumberDropdown);
            Controls.Add(TableNameInput);
            Controls.Add(TableNameLabel);
            Controls.Add(CreateTableButton);
            Margin = new Padding(3, 4, 3, 4);
            Name = "CreateTableForm";
            Text = "CreateTableForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button CreateTableButton;
        private Label TableNameLabel;
        private TextBox TableNameInput;
        private ComboBox ColumnNumberDropdown;
        private Label NumberOfColumnsLabel;
    }
}