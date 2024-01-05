namespace UI
{
    partial class InsertForm
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
            InsertButton = new Button();
            TableNameLabel = new Label();
            SuspendLayout();
            // 
            // InsertButton
            // 
            InsertButton.Enabled = false;
            InsertButton.Location = new Point(297, 363);
            InsertButton.Name = "InsertButton";
            InsertButton.Size = new Size(174, 75);
            InsertButton.TabIndex = 0;
            InsertButton.Text = "Insert";
            InsertButton.UseVisualStyleBackColor = true;
            InsertButton.Click += InsertButton_Click;
            // 
            // TableNameLabel
            // 
            TableNameLabel.AutoSize = true;
            TableNameLabel.Location = new Point(359, 20);
            TableNameLabel.Name = "TableNameLabel";
            TableNameLabel.Size = new Size(0, 15);
            TableNameLabel.TabIndex = 1;
            // 
            // InsertForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(TableNameLabel);
            Controls.Add(InsertButton);
            Name = "InsertForm";
            Text = "InsertForm";
            Load += InsertForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button InsertButton;
        private Label TableNameLabel;
    }
}