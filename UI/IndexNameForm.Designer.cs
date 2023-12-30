namespace UI
{
    partial class IndexNameForm
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
            SubmitButton = new Button();
            IndexNameLabel = new Label();
            IndexNameTextBox = new TextBox();
            ColumnNamesListBox = new ListBox();
            SuspendLayout();
            // 
            // SubmitButton
            // 
            SubmitButton.Location = new Point(63, 124);
            SubmitButton.Name = "SubmitButton";
            SubmitButton.Size = new Size(177, 67);
            SubmitButton.TabIndex = 0;
            SubmitButton.Text = "Submit";
            SubmitButton.UseVisualStyleBackColor = true;
            SubmitButton.Click += SubmitButton_Click;
            // 
            // IndexNameLabel
            // 
            IndexNameLabel.AutoSize = true;
            IndexNameLabel.Location = new Point(106, 9);
            IndexNameLabel.Name = "IndexNameLabel";
            IndexNameLabel.Size = new Size(89, 20);
            IndexNameLabel.TabIndex = 1;
            IndexNameLabel.Text = "Index Name";
            // 
            // IndexNameTextBox
            // 
            IndexNameTextBox.Location = new Point(26, 44);
            IndexNameTextBox.Multiline = true;
            IndexNameTextBox.Name = "IndexNameTextBox";
            IndexNameTextBox.Size = new Size(252, 55);
            IndexNameTextBox.TabIndex = 2;
            // 
            // ColumnNamesListBox
            // 
            ColumnNamesListBox.FormattingEnabled = true;
            ColumnNamesListBox.ItemHeight = 20;
            ColumnNamesListBox.Location = new Point(420, 9);
            ColumnNamesListBox.Name = "ColumnNamesListBox";
            ColumnNamesListBox.SelectionMode = SelectionMode.MultiSimple;
            ColumnNamesListBox.Size = new Size(255, 564);
            ColumnNamesListBox.TabIndex = 3;
            // 
            // IndexNameDialog
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(677, 588);
            Controls.Add(ColumnNamesListBox);
            Controls.Add(IndexNameTextBox);
            Controls.Add(IndexNameLabel);
            Controls.Add(SubmitButton);
            Name = "IndexNameDialog";
            Text = "IndexNameDialog";
            Load += IndexNameDialog_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button SubmitButton;
        private Label IndexNameLabel;
        private TextBox IndexNameTextBox;
        private ListBox ColumnNamesListBox;
    }
}