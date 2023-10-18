namespace UserInterface
{
    partial class MainPanel
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
            this.Notepad = new System.Windows.Forms.TextBox();
            this.ExecuteCommands = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Notepad
            // 
            this.Notepad.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.Notepad.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Notepad.Location = new System.Drawing.Point(24, 71);
            this.Notepad.Multiline = true;
            this.Notepad.Name = "Notepad";
            this.Notepad.Size = new System.Drawing.Size(810, 367);
            this.Notepad.TabIndex = 0;
            // 
            // ExecuteCommands
            // 
            this.ExecuteCommands.Location = new System.Drawing.Point(366, 7);
            this.ExecuteCommands.Name = "ExecuteCommands";
            this.ExecuteCommands.Size = new System.Drawing.Size(131, 58);
            this.ExecuteCommands.TabIndex = 1;
            this.ExecuteCommands.Text = "Execute";
            this.ExecuteCommands.UseVisualStyleBackColor = true;
            this.ExecuteCommands.Click += new System.EventHandler(this.ExecuteCommand_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(846, 465);
            this.Controls.Add(this.ExecuteCommands);
            this.Controls.Add(this.Notepad);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox Notepad;
        private System.Windows.Forms.Button ExecuteCommands;
    }
}

