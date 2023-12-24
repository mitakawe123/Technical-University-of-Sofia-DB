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
            TableInfoGrid = new DataGridView();
            TableNameTextBox = new TextBox();
            NumberOfDataPagesTextBox = new TextBox();
            ColumnCountTextBox = new TextBox();
            insertIntoTableToolStripMenuItem = new ToolStripMenuItem();
            TableMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)DataGridView).BeginInit();
            ((System.ComponentModel.ISupportInitialize)TableInfoGrid).BeginInit();
            SuspendLayout();
            // 
            // tableNames
            // 
            tableNames.HoverSelection = true;
            tableNames.Location = new Point(0, 0);
            tableNames.Margin = new Padding(3, 2, 3, 2);
            tableNames.Name = "tableNames";
            tableNames.Size = new Size(192, 517);
            tableNames.TabIndex = 0;
            tableNames.UseCompatibleStateImageBehavior = false;
            tableNames.View = View.List;
            // 
            // TableMenu
            // 
            TableMenu.ImageScalingSize = new Size(20, 20);
            TableMenu.Items.AddRange(new ToolStripItem[] { ShowAllRecords, DropTable, insertIntoTableToolStripMenuItem });
            TableMenu.Name = "TableMenu";
            TableMenu.Size = new Size(181, 92);
            // 
            // ShowAllRecords
            // 
            ShowAllRecords.Name = "ShowAllRecords";
            ShowAllRecords.Size = new Size(180, 22);
            ShowAllRecords.Text = "Show all records";
            ShowAllRecords.Click += ShowAllRecords_Click;
            // 
            // DropTable
            // 
            DropTable.Name = "DropTable";
            DropTable.Size = new Size(180, 22);
            DropTable.Text = "Drop table";
            DropTable.Click += DropTable_Click;
            // 
            // DataGridView
            // 
            DataGridView.AllowUserToAddRows = false;
            DataGridView.AllowUserToDeleteRows = false;
            DataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            DataGridView.Location = new Point(197, 139);
            DataGridView.Margin = new Padding(3, 2, 3, 2);
            DataGridView.Name = "DataGridView";
            DataGridView.ReadOnly = true;
            DataGridView.RowHeadersWidth = 51;
            DataGridView.Size = new Size(906, 377);
            DataGridView.TabIndex = 1;
            // 
            // TableInfoGrid
            // 
            TableInfoGrid.AllowUserToAddRows = false;
            TableInfoGrid.AllowUserToDeleteRows = false;
            TableInfoGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            TableInfoGrid.Location = new Point(372, 0);
            TableInfoGrid.Margin = new Padding(3, 2, 3, 2);
            TableInfoGrid.Name = "TableInfoGrid";
            TableInfoGrid.ReadOnly = true;
            TableInfoGrid.RowHeadersWidth = 51;
            TableInfoGrid.Size = new Size(732, 134);
            TableInfoGrid.TabIndex = 2;
            // 
            // TableNameTextBox
            // 
            TableNameTextBox.Location = new Point(197, 52);
            TableNameTextBox.Margin = new Padding(3, 2, 3, 2);
            TableNameTextBox.Multiline = true;
            TableNameTextBox.Name = "TableNameTextBox";
            TableNameTextBox.Size = new Size(170, 42);
            TableNameTextBox.TabIndex = 3;
            // 
            // NumberOfDataPagesTextBox
            // 
            NumberOfDataPagesTextBox.Location = new Point(197, 9);
            NumberOfDataPagesTextBox.Margin = new Padding(3, 2, 3, 2);
            NumberOfDataPagesTextBox.Multiline = true;
            NumberOfDataPagesTextBox.Name = "NumberOfDataPagesTextBox";
            NumberOfDataPagesTextBox.Size = new Size(170, 39);
            NumberOfDataPagesTextBox.TabIndex = 4;
            // 
            // ColumnCountTextBox
            // 
            ColumnCountTextBox.Location = new Point(197, 98);
            ColumnCountTextBox.Margin = new Padding(3, 2, 3, 2);
            ColumnCountTextBox.Multiline = true;
            ColumnCountTextBox.Name = "ColumnCountTextBox";
            ColumnCountTextBox.Size = new Size(170, 38);
            ColumnCountTextBox.TabIndex = 5;
            // 
            // insertIntoTableToolStripMenuItem
            // 
            insertIntoTableToolStripMenuItem.Name = "insertIntoTableToolStripMenuItem";
            insertIntoTableToolStripMenuItem.Size = new Size(180, 22);
            insertIntoTableToolStripMenuItem.Text = "Insert Into Table";
            insertIntoTableToolStripMenuItem.Click += InsertIntoTableToolStripMenuItem_Click;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1105, 517);
            Controls.Add(ColumnCountTextBox);
            Controls.Add(NumberOfDataPagesTextBox);
            Controls.Add(TableNameTextBox);
            Controls.Add(TableInfoGrid);
            Controls.Add(DataGridView);
            Controls.Add(tableNames);
            Name = "Main";
            Text = "MainForm";
            Load += MainForm_Load;
            TableMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)DataGridView).EndInit();
            ((System.ComponentModel.ISupportInitialize)TableInfoGrid).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListView tableNames;
        private ContextMenuStrip TableMenu;
        private ToolStripMenuItem ShowAllRecords;
        private ToolStripMenuItem DropTable;
        private DataGridView DataGridView;
        private DataGridView TableInfoGrid;
        private TextBox TableNameTextBox;
        private TextBox NumberOfDataPagesTextBox;
        private TextBox ColumnCountTextBox;
        private ToolStripMenuItem insertIntoTableToolStripMenuItem;
    }
}