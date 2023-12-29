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
            ExitButton = new Button();
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
            tableNames.Name = "tableNames";
            tableNames.Size = new Size(219, 621);
            tableNames.TabIndex = 0;
            tableNames.UseCompatibleStateImageBehavior = false;
            tableNames.View = View.List;
            // 
            // TableMenu
            // 
            TableMenu.ImageScalingSize = new Size(20, 20);
            TableMenu.Items.AddRange(new ToolStripItem[] { ShowAllRecords, DropTable, insertIntoTableToolStripMenuItem, createTableToolStripMenuItem });
            TableMenu.Name = "TableMenu";
            TableMenu.Size = new Size(188, 100);
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
            // insertIntoTableToolStripMenuItem
            // 
            insertIntoTableToolStripMenuItem.Name = "insertIntoTableToolStripMenuItem";
            insertIntoTableToolStripMenuItem.Size = new Size(187, 24);
            insertIntoTableToolStripMenuItem.Text = "Insert Into Table";
            insertIntoTableToolStripMenuItem.Click += InsertIntoTableToolStripMenuItem_Click;
            // 
            // createTableToolStripMenuItem
            // 
            createTableToolStripMenuItem.Name = "createTableToolStripMenuItem";
            createTableToolStripMenuItem.Size = new Size(187, 24);
            createTableToolStripMenuItem.Text = "Create Table";
            createTableToolStripMenuItem.Click += CreateTableToolStripMenuItem_Click;
            // 
            // DataGridView
            // 
            DataGridView.AllowUserToAddRows = false;
            DataGridView.AllowUserToDeleteRows = false;
            DataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            DataGridView.Location = new Point(225, 185);
            DataGridView.Name = "DataGridView";
            DataGridView.ReadOnly = true;
            DataGridView.RowHeadersWidth = 51;
            DataGridView.Size = new Size(1035, 503);
            DataGridView.TabIndex = 1;
            DataGridView.MouseClick += DataGridView_MouseClick;
            // 
            // TableInfoGrid
            // 
            TableInfoGrid.AllowUserToAddRows = false;
            TableInfoGrid.AllowUserToDeleteRows = false;
            TableInfoGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            TableInfoGrid.Location = new Point(425, 0);
            TableInfoGrid.Name = "TableInfoGrid";
            TableInfoGrid.ReadOnly = true;
            TableInfoGrid.RowHeadersWidth = 51;
            TableInfoGrid.Size = new Size(837, 179);
            TableInfoGrid.TabIndex = 2;
            // 
            // TableNameTextBox
            // 
            TableNameTextBox.Location = new Point(226, 0);
            TableNameTextBox.Multiline = true;
            TableNameTextBox.Name = "TableNameTextBox";
            TableNameTextBox.Size = new Size(194, 83);
            TableNameTextBox.TabIndex = 3;
            // 
            // ColumnCountTextBox
            // 
            ColumnCountTextBox.Location = new Point(225, 89);
            ColumnCountTextBox.Multiline = true;
            ColumnCountTextBox.Name = "ColumnCountTextBox";
            ColumnCountTextBox.Size = new Size(194, 91);
            ColumnCountTextBox.TabIndex = 5;
            // 
            // DataGridViewMenu
            // 
            DataGridViewMenu.ImageScalingSize = new Size(20, 20);
            DataGridViewMenu.Items.AddRange(new ToolStripItem[] { deleteRowToolStripMenuItem });
            DataGridViewMenu.Name = "DataGridViewMenu";
            DataGridViewMenu.Size = new Size(156, 28);
            // 
            // deleteRowToolStripMenuItem
            // 
            deleteRowToolStripMenuItem.Name = "deleteRowToolStripMenuItem";
            deleteRowToolStripMenuItem.Size = new Size(155, 24);
            deleteRowToolStripMenuItem.Text = "Delete Row";
            deleteRowToolStripMenuItem.Click += DeleteRowToolStripMenuItem_Click;
            // 
            // ExitButton
            // 
            ExitButton.Location = new Point(0, 627);
            ExitButton.Name = "ExitButton";
            ExitButton.Size = new Size(219, 61);
            ExitButton.TabIndex = 6;
            ExitButton.Text = "Exit";
            ExitButton.UseVisualStyleBackColor = true;
            ExitButton.Click += ExitButton_Click;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1263, 689);
            Controls.Add(ExitButton);
            Controls.Add(ColumnCountTextBox);
            Controls.Add(TableNameTextBox);
            Controls.Add(TableInfoGrid);
            Controls.Add(DataGridView);
            Controls.Add(tableNames);
            Margin = new Padding(3, 4, 3, 4);
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
        private Button ExitButton;
    }
}