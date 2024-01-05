using DataStructures;
using DMS.DataPages;
using DMS.Extensions;
using DMS.Shared;
using TextBox = System.Windows.Forms.TextBox;

namespace UI;

public partial class CreateTableForm : Form
{
    private const int MaxNumberColumns = 128;
    private readonly DKList<Control> _dynamicControls = new();

    public CreateTableForm()
    {
        InitializeComponent();
        InitializeDropdown();
    }

    private void InitializeDropdown()
    {
        ColumnNumberDropdown.Items.Clear();
        for (int i = 1; i <= MaxNumberColumns; i++)
            ColumnNumberDropdown.Items.Add(i);

        ColumnNumberDropdown.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
    }

    private void ComboBox_SelectedIndexChanged(object? sender, EventArgs e)
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
        const int spacingY = 40;
        int currentY = startY;

        for (int i = 0; i < numberOfRows; i++)
        {
            Label lblColumnName = new()
            {
                Text = "Column Name",
                Location = new Point(20, currentY),
                Width = 100,
                AutoSize = true
            };
            this.Controls.Add(lblColumnName);
            _dynamicControls.Add(lblColumnName);

            TextBox txtColumnName = new()
            {
                Name = $"ColumnName_{i}",
                Location = new Point(130, currentY),
                Width = 100
            };
            this.Controls.Add(txtColumnName);
            _dynamicControls.Add(txtColumnName);

            Label lblColumnType = new()
            {
                Text = "Column Type",
                Location = new Point(240, currentY),
                Width = 100,
                AutoSize = true
            };
            this.Controls.Add(lblColumnType);
            _dynamicControls.Add(lblColumnType);

            TextBox txtColumnType = new()
            {
                Name = $"ColumnType_{i}",
                Location = new Point(350, currentY),
                Width = 100,
            };
            this.Controls.Add(txtColumnType);
            _dynamicControls.Add(txtColumnType);

            Label lblDefaultValue = new()
            {
                Text = "Default Value",
                Location = new Point(460, currentY),
                Width = 100,
                AutoSize = true,
                Visible = false
            };
            this.Controls.Add(lblDefaultValue);
            _dynamicControls.Add(lblDefaultValue);

            TextBox txtDefaultValue = new()
            {
                Name = $"DefaultValue_{i}",
                Location = new Point(570, currentY),
                Width = 100,
                Visible = false
            };
            this.Controls.Add(txtDefaultValue);
            _dynamicControls.Add(txtDefaultValue);

            currentY += spacingY;
        }
    }

    private void CreateTableButton_Click(object sender, EventArgs e)
    {
        ReadOnlySpan<char> tableName = TableNameInput.Text.CustomAsSpan();
        DKList<Column> columns = new();

        for (int i = 0; i < _dynamicControls.Count; i += 3)
        {
            if (Controls[$"ColumnName_{i / 3}"] is not TextBox columnNameTextBox
                || Controls[$"ColumnType_{i / 3}"] is not TextBox columnTypeTextBox
                || Controls[$"DefaultValue_{i / 3}"] is not TextBox defaultValueTextBox)
                continue;

            string columnName = columnNameTextBox.Text;
            string columnType = columnTypeTextBox.Text;
            string defaultValue = defaultValueTextBox.Text;
            columns.Add(new Column(columnName, columnType, defaultValue));
        }

        DataPageManager.CreateTable(columns, tableName);

        Close();
    }
}