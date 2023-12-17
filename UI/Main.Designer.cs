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
            components = new System.ComponentModel.Container();
            tableNames = new ListView();
            TableMenu = new ContextMenuStrip(components);
            ShowAllRecords = new ToolStripMenuItem();
            DropTable = new ToolStripMenuItem();
            DataGridView = new DataGridView();
            TableMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)DataGridView).BeginInit();
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
            // 
            // TableMenu
            // 
            TableMenu.ImageScalingSize = new Size(20, 20);
            TableMenu.Items.AddRange(new ToolStripItem[] { ShowAllRecords, DropTable });
            TableMenu.Name = "TableMenu";
            TableMenu.Size = new Size(188, 52);
            // 
            // ShowAllRecords
            // 
            ShowAllRecords.Name = "ShowAllRecords";
            ShowAllRecords.Size = new Size(187, 24);
            ShowAllRecords.Text = "Show all records";
            ShowAllRecords.Click += ShowAllRecords_Click;
            // 
            // DropTable
            // 
            DropTable.Name = "DropTable";
            DropTable.Size = new Size(187, 24);
            DropTable.Text = "Drop table";
            DropTable.Click += DropTable_Click;
            // 
            // DataGridView
            // 
            DataGridView.AllowUserToAddRows = false;
            DataGridView.AllowUserToDeleteRows = false;
            DataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            DataGridView.Location = new Point(225, 0);
            DataGridView.Name = "DataGridView";
            DataGridView.ReadOnly = true;
            DataGridView.RowHeadersWidth = 51;
            DataGridView.Size = new Size(1036, 688);
            DataGridView.TabIndex = 1;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1263, 689);
            Controls.Add(DataGridView);
            Controls.Add(tableNames);
            Margin = new Padding(3, 4, 3, 4);
            Name = "Main";
            Text = "MainForm";
            Load += MainForm_Load;
            TableMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)DataGridView).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private ListView tableNames;
        private ContextMenuStrip TableMenu;
        private ToolStripMenuItem ShowAllRecords;
        private ToolStripMenuItem DropTable;
        private DataGridView DataGridView;
    }
}