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
            insertIntoTableToolStripMenuItem = new ToolStripMenuItem();
            createTableToolStripMenuItem = new ToolStripMenuItem();
            DataGridView = new DataGridView();
            TableInfoGrid = new DataGridView();
            TableNameTextBox = new TextBox();
            ColumnCountTextBox = new TextBox();
            DataGridViewMenu = new ContextMenuStrip(components);
            deleteRowToolStripMenuItem = new ToolStripMenuItem();
            TableMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)DataGridView).BeginInit();
            ((System.ComponentModel.ISupportInitialize)TableInfoGrid).BeginInit();
            DataGridViewMenu.SuspendLayout();
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
            TableMenu.Items.AddRange(new ToolStripItem[] { ShowAllRecords, DropTable, insertIntoTableToolStripMenuItem, createTableToolStripMenuItem });
            TableMenu.Name = "TableMenu";
            TableMenu.Size = new Size(161, 92);
            // 
            // ShowAllRecords
            // 
            ShowAllRecords.Name = "ShowAllRecords";
            ShowAllRecords.Size = new Size(160, 22);
            ShowAllRecords.Text = "Show all records";
            ShowAllRecords.Click += ShowAllRecords_Click;
            // 
            // DropTable
            // 
            DropTable.Name = "DropTable";
            DropTable.Size = new Size(160, 22);
            DropTable.Text = "Drop table";
            DropTable.Click += DropTable_Click;
            // 
            // insertIntoTableToolStripMenuItem
            // 
            insertIntoTableToolStripMenuItem.Name = "insertIntoTableToolStripMenuItem";
            insertIntoTableToolStripMenuItem.Size = new Size(160, 22);
            insertIntoTableToolStripMenuItem.Text = "Insert Into Table";
            insertIntoTableToolStripMenuItem.Click += InsertIntoTableToolStripMenuItem_Click;
            // 
            // createTableToolStripMenuItem
            // 
            createTableToolStripMenuItem.Name = "createTableToolStripMenuItem";
            createTableToolStripMenuItem.Size = new Size(160, 22);
            createTableToolStripMenuItem.Text = "Create Table";
            createTableToolStripMenuItem.Click += CreateTableToolStripMenuItem_Click;
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
            DataGridView.MouseClick += DataGridView_MouseClick;
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
            TableNameTextBox.Location = new Point(198, 0);
            TableNameTextBox.Margin = new Padding(3, 2, 3, 2);
            TableNameTextBox.Multiline = true;
            TableNameTextBox.Name = "TableNameTextBox";
            TableNameTextBox.Size = new Size(170, 63);
            TableNameTextBox.TabIndex = 3;
            // 
            // ColumnCountTextBox
            // 
            ColumnCountTextBox.Location = new Point(197, 67);
            ColumnCountTextBox.Margin = new Padding(3, 2, 3, 2);
            ColumnCountTextBox.Multiline = true;
            ColumnCountTextBox.Name = "ColumnCountTextBox";
            ColumnCountTextBox.Size = new Size(170, 69);
            ColumnCountTextBox.TabIndex = 5;
            // 
            // DataGridViewMenu
            // 
            DataGridViewMenu.Items.AddRange(new ToolStripItem[] { deleteRowToolStripMenuItem });
            DataGridViewMenu.Name = "DataGridViewMenu";
            DataGridViewMenu.Size = new Size(134, 26);
            // 
            // deleteRowToolStripMenuItem
            // 
            deleteRowToolStripMenuItem.Name = "deleteRowToolStripMenuItem";
            deleteRowToolStripMenuItem.Size = new Size(133, 22);
            deleteRowToolStripMenuItem.Text = "Delete Row";
            deleteRowToolStripMenuItem.Click += DeleteRowToolStripMenuItem_Click;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1105, 517);
            Controls.Add(ColumnCountTextBox);
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
            DataGridViewMenu.ResumeLayout(false);
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
        private ContextMenuStrip DataGridViewMenu;
        private ToolStripMenuItem createTableToolStripMenuItem;
        private ToolStripMenuItem deleteRowToolStripMenuItem;
    }
}