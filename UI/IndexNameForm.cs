using DataStructures;
using DMS.Extensions;
using DMS.Indexes;

namespace UI
{
    public partial class IndexNameForm : Form
    {
        private readonly IReadOnlyList<string> _columnNames;
        private readonly string _tableName;

        private IndexNameForm()
        {
            InitializeComponent();
        }

        public IndexNameForm(string tableName, IReadOnlyList<string> columnNames) : this()
        {
            _tableName = tableName;
            _columnNames = columnNames;
        }

        private void IndexNameDialog_Load(object sender, EventArgs e)
        {
            foreach (string name in _columnNames)
                ColumnNamesListBox.Items.Add(name);
        }

        private void SubmitButton_Click(object sender, EventArgs e)
        {
            if (ColumnNamesListBox.SelectedItems.Count <= 0)
            {
                MessageBox.Show(@"Please select at least one column to index");
                return;
            }

            if (IndexNameTextBox.Text == string.Empty)
            {
                MessageBox.Show(@"Please enter a index name");
                return;
            }

            DKList<string> columnsToIndex = new();

            foreach (string name in ColumnNamesListBox.SelectedItems)
                columnsToIndex.Add(name);

            ReadOnlySpan<char> tableName = _tableName.CustomAsSpan();
            ReadOnlySpan<char> indexName = IndexNameTextBox.Text.CustomAsSpan().CustomTrim();

            IndexManager.CreateIndex(columnsToIndex, tableName, indexName);
            Close();
        }
    }
}
