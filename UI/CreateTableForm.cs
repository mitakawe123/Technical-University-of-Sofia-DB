using DataStructures;
using DMS.DataPages;
using DMS.Extensions;
using DMS.Shared;
using TextBox = System.Windows.Forms.TextBox;

namespace UI;

public partial class CreateTableForm : Form
{
    private const int MAX_NUMBER_COLUMNS = 128;
    private readonly DKList<Control> _dynamicControls = new();

    public CreateTableForm()
    {
        InitializeComponent();
        InitializeDropdown();
    }

    private void InitializeDropdown()
    {
        ColumnNumberDropdown.Items.Clear();
        for (int i = 1; i <= MAX_NUMBER_COLUMNS; i++)
            ColumnNumberDropdown.Items.Add(i);

        ColumnNumberDropdown.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
    }

    private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        ClearDynamicControls();
        int numberOfRows = (int)ColumnNumberDropdown.SelectedItem;
        CreateInputFields(numberOfRows);
    }

    private void ClearDynamicControls()
    {
        foreach (Control control in _dynamicControls)
        {
            Controls.Remove(control);
            control.Dispose();
        }
        _dynamicControls.Clear();
    }

    private void CreateInputFields(int numberOfRows)
    {
        const int startY = 50;
        const int spacingY = 30;
        int currentY = startY;

        for (int i = 0; i < numberOfRows; i++)
        {
            Label lblColumnName = new()
            {
                Text = "Column Name",
                Location = new Point(10, currentY)
            };
            this.Controls.Add(lblColumnName);
            _dynamicControls.Add(lblColumnName);

            TextBox txtColumnName = new()
            {
                Name = $"ColumnName_{i}",
                Location = new Point(110, currentY)
            };
            this.Controls.Add(txtColumnName);
            _dynamicControls.Add(txtColumnName);

            Label lblColumnType = new()
            {
                Text = "Column Type",
                Location = new Point(220, currentY)
            };
            this.Controls.Add(lblColumnType);
            _dynamicControls.Add(lblColumnType);

            TextBox txtColumnType = new()
            {
                Name = $"ColumnType_{i}",
                Location = new Point(320, currentY)
            };
            this.Controls.Add(txtColumnType);
            _dynamicControls.Add(txtColumnType);

            currentY += spacingY;
        }
    }

    private void CreateTableButton_Click(object sender, EventArgs e)
    {
        ReadOnlySpan<char> tableName = TableNameInput.Text.CustomAsSpan();
        DKList<Column> columns = new();

        for (int i = 0; i < _dynamicControls.Count; i += 2)
        {
            if (Controls[$"ColumnName_{i / 2}"] is not TextBox columnNameTextBox
                || Controls[$"ColumnType_{i / 2}"] is not TextBox columnTypeTextBox)
                continue;

            string columnName = columnNameTextBox.Text;
            string columnType = columnTypeTextBox.Text;
            columns.Add(new Column(columnName, columnType));
        }

        DataPageManager.CreateTable(columns, tableName);

        Close();
    }
}