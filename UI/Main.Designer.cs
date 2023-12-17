namespace UI
{
    partial class Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tableNames = new ListView();
            SuspendLayout();
            // 
            // tableNames
            // 
            tableNames.HoverSelection = true;
            tableNames.Location = new Point(0, 0);
            tableNames.Name = "tableNames";
            tableNames.Size = new Size(219, 688);
            tableNames.TabIndex = 0;
            tableNames.UseCompatibleStateImageBehavior = false;
            tableNames.View = View.List;
            tableNames.ItemActivate += ShowTableRecords;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1263, 689);
            Controls.Add(tableNames);
            Margin = new Padding(3, 4, 3, 4);
            Name = "Main";
            Text = "MainForm";
            Load += MainForm_Load;
            ResumeLayout(false);
        }

        #endregion

        private ListView tableNames;
    }
}