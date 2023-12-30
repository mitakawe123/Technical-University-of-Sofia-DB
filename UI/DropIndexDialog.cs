using DMS.Extensions;
using DMS.Indexes;

namespace UI
{
    public partial class DropIndexDialog : Form
    {
        private readonly string _tableName;

        public DropIndexDialog()
        {
            InitializeComponent();
        }

        public DropIndexDialog(string tableName)
        {
            _tableName = tableName;
        }

        private void DropIndexButton_Click(object sender, EventArgs e)
        {
            ReadOnlySpan<char> indexName = DropIndexTextBox.Text.CustomAsSpan().CustomTrim();
            ReadOnlySpan<char> tableName = _tableName.CustomAsSpan();

            IndexManager.DropIndex(tableName, indexName);
        }
    }
}
