using DataStructures;
using DMS.Commands;
using DMS.DataPages;
using System.Data;
using DMS.Shared;

namespace UI
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            DataPageManager.InitDataPageManager();

            LoadTableNamesIntoListView();

            tableNames.ContextMenuStrip = TableMenu;
        }

        private void LoadTableNamesIntoListView()
        {
            foreach (var item in DataPageManager.TableOffsets)
            {
                ListViewItem listViewItem = new(new string(item.Key));
                listViewItem.SubItems.Add(item.Value.ToString());
                tableNames.Items.Add(listViewItem);
            }
        }

        private void ShowAllRecords_Click(object sender, EventArgs e)
        {
            if (tableNames.SelectedItems.Count <= 0)
                return;

            DKList<string> valuesToSelect = new() { "*" };
            ReadOnlySpan<char> tableName = tableNames.SelectedItems[0].Text;
            ReadOnlySpan<char> logicalOperator = "";

            SelectQueryParams tableInformation = SqlCommands.SelectFromTable(valuesToSelect, tableName, logicalOperator, true);

            DataTable dataTable = new();

            foreach (var column in tableInformation.ColumnTypeAndName)
                dataTable.Columns.Add(column.Name, typeof(string));

            for (int i = 0; i < tableInformation.AllData.Count; i += tableInformation.ColumnCount)
            {
                DataRow newRow = dataTable.NewRow();

                for (int col = 0; col < tableInformation.ColumnCount; col++)
                    newRow[col] = new string(tableInformation.AllData[i + col]);

                dataTable.Rows.Add(newRow);
            }

            DataGridView.DataSource = dataTable;

            TableInfo tableInfo = DataPageManager.TableInfo(tableName, true);

            NumberOfDataPagesTextBox.Text = "Number of data pages the table snaps across: " + tableInfo.NumberOfDataPages;
            TableNameTextBox.Text = "Table Name: " + tableInfo.TableName;
            ColumnCountTextBox.Text = "Number of columns: " + tableInfo.ColumnCount;

            TableInfoGrid.Columns.Add("ColumnName", "Column Name");
            TableInfoGrid.Columns.Add("ColumnType", "Column Type");

            for (int i = 0; i < tableInfo.ColumnName.Count; i++)
                TableInfoGrid.Rows.Add(tableInfo.ColumnName[i], tableInfo.ColumnType[i]);

        }

        private void DropTable_Click(object sender, EventArgs e)
        {
            if (tableNames.SelectedItems.Count <= 0)
            {
                MessageBox.Show("Please select a table to drop.");
                return;
            }

            ReadOnlySpan<char> tableName = tableNames.SelectedItems[0].Text;
            DataPageManager.DropTable(tableName);

            tableNames.Items.Remove(tableNames.SelectedItems[0]);
            tableNames.Refresh();
            DataGridView.Refresh();
        }
    }
}