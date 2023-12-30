namespace UI
{
    partial class DropIndexDialog
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
            DropIndexLabel = new Label();
            DropIndexTextBox = new TextBox();
            DropIndexButton = new Button();
            SuspendLayout();
            // 
            // DropIndexLabel
            // 
            DropIndexLabel.AutoSize = true;
            DropIndexLabel.Location = new Point(167, 37);
            DropIndexLabel.Name = "DropIndexLabel";
            DropIndexLabel.Size = new Size(89, 20);
            DropIndexLabel.TabIndex = 0;
            DropIndexLabel.Text = "Index Name";
            // 
            // DropIndexTextBox
            // 
            DropIndexTextBox.Location = new Point(22, 89);
            DropIndexTextBox.Multiline = true;
            DropIndexTextBox.Name = "DropIndexTextBox";
            DropIndexTextBox.Size = new Size(394, 101);
            DropIndexTextBox.TabIndex = 1;
            // 
            // DropIndexButton
            // 
            DropIndexButton.Location = new Point(123, 216);
            DropIndexButton.Name = "DropIndexButton";
            DropIndexButton.Size = new Size(171, 70);
            DropIndexButton.TabIndex = 2;
            DropIndexButton.Text = "Drop Index";
            DropIndexButton.UseVisualStyleBackColor = true;
            DropIndexButton.Click += DropIndexButton_Click;
            // 
            // DropIndexDialog
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(441, 298);
            Controls.Add(DropIndexButton);
            Controls.Add(DropIndexTextBox);
            Controls.Add(DropIndexLabel);
            Name = "DropIndexDialog";
            Text = "DropIndexDialog";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label DropIndexLabel;
        private TextBox DropIndexTextBox;
        private Button DropIndexButton;
    }
}