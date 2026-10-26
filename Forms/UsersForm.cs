using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Lavrinovich.Forms
{
    public class UsersForm : Form
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["DB_Users"].ConnectionString;
        private readonly BindingSource usersSource = new BindingSource();
        private readonly BindingNavigator navigator;
        private readonly DataGridView grid;
        private readonly Button saveButton;
        private SqlDataAdapter adapter;
        private DataTable usersTable;
        private DataTable rolesTable;

        public UsersForm()
        {
            Text = "Lavrinovich — работа с BindingNavigator";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(800, 480);
            Size = new Size(1000, 600);

            navigator = new BindingNavigator(true) { Dock = DockStyle.Top, Name = "usersBindingNavigator" };
            navigator.AddNewItem = null;
            navigator.DeleteItem = null;
            var addButton = new ToolStripButton("Добавить") { Name = "bindingNavigatorAddNewItem" };
            var deleteButton = new ToolStripButton("Удалить") { Name = "bindingNavigatorDeleteItem" };
            navigator.Items.Add(addButton);
            navigator.Items.Add(deleteButton);
            navigator.Items.Add(new ToolStripSeparator());
            navigator.Items.Add(new ToolStripLabel("Записи:"));
            navigator.Items.Add(new ToolStripTextBox { Name = "positionItem", ReadOnly = true, Width = 45 });
            navigator.Items.Add(new ToolStripLabel("из"));
            navigator.Items.Add(new ToolStripLabel { Name = "countItem", Width = 35 });
            navigator.Items.Add(new ToolStripSeparator());
            saveButton = new Button { Text = "Сохранить изменения", Dock = DockStyle.Bottom, Height = 42 };
            grid = new DataGridView
            {
                Dock = DockStyle.Fill, Name = "usersDataGridView", AutoGenerateColumns = false,
                AllowUserToAddRows = true, AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, RowHeadersVisible = false
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ID", HeaderText = "ID", ReadOnly = true, Name = "ID", FillWeight = 35 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Login", HeaderText = "Логин", Name = "Login" });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Password", HeaderText = "Пароль", Name = "Password" });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "First_Name", HeaderText = "Имя", Name = "First_Name" });
            grid.Columns.Add(new DataGridViewComboBoxColumn { DataPropertyName = "Role_Name", HeaderText = "Роль", Name = "Role_Name", DisplayMember = "Name", ValueMember = "Name", FlatStyle = FlatStyle.Flat });

            Controls.Add(grid);
            Controls.Add(saveButton);
            Controls.Add(navigator);

            navigator.BindingSource = usersSource;
            saveButton.Click += SaveButton_Click;
            ((ToolStripButton)navigator.Items["bindingNavigatorAddNewItem"]).Click += AddNewItem_Click;
            ((ToolStripButton)navigator.Items["bindingNavigatorDeleteItem"]).Click += DeleteItem_Click;
            Load += UsersForm_Load;
            FormClosing += UsersForm_FormClosing;
        }

        private void UsersForm_Load(object sender, EventArgs e)
        {
            try { StartLoadData(); }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить данные из DB_Users. Проверьте SQL Server и строку подключения.\\n\\n" + ex.Message,
                    "Ошибка подключения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartLoadData()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                rolesTable = new DataTable();
                using (var rolesAdapter = new SqlDataAdapter("SELECT [Name] FROM dbo.Roles ORDER BY [Name]", connection))
                    rolesAdapter.Fill(rolesTable);

                usersTable = new DataTable();
                adapter = new SqlDataAdapter("SELECT [ID], [Login], [Password], [First_Name], [Role_Name] FROM dbo.Users ORDER BY [ID]", connection);
                var builder = new SqlCommandBuilder(adapter);
                adapter.InsertCommand = builder.GetInsertCommand();
                adapter.UpdateCommand = builder.GetUpdateCommand();
                adapter.DeleteCommand = builder.GetDeleteCommand();
                adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                adapter.Fill(usersTable);
            }

            var roleColumn = (DataGridViewComboBoxColumn)grid.Columns["Role_Name"];
            roleColumn.DataSource = rolesTable;
            usersSource.DataSource = usersTable;
            grid.DataSource = usersSource;
            usersSource.PositionChanged += (s, e) => UpdatePosition();
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            var position = navigator.Items["positionItem"] as ToolStripTextBox;
            var count = navigator.Items["countItem"] as ToolStripLabel;
            if (position != null) position.Text = usersSource.Count == 0 ? "0" : (usersSource.Position + 1).ToString();
            if (count != null) count.Text = usersSource.Count.ToString();
        }

        private void AddNewItem_Click(object sender, EventArgs e)
        {
            if (rolesTable != null && rolesTable.Rows.Count > 0)
            {
                usersSource.AddNew();
                if (usersSource.Current is DataRowView current)
                    current["Role_Name"] = rolesTable.Rows[0]["Name"];
            }
        }

        private void DeleteItem_Click(object sender, EventArgs e)
        {
            if (usersSource.Current == null) return;
            if (MessageBox.Show("Удалить выбранную запись?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                usersSource.RemoveCurrent();
        }

        private void SaveButton_Click(object sender, EventArgs e) { SaveData(); }

        private void SaveData()
        {
            try
            {
                Validate();
                grid.EndEdit();
                usersSource.EndEdit();
                if (usersTable != null && adapter != null)
                {
                    adapter.Update(usersTable);
                    MessageBox.Show("Изменения сохранены в базе данных.", "Готово",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    StartLoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить данные. Проверьте обязательные поля и значения ролей.\\n\\n" + ex.Message,
                    "Ошибка сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UsersForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (usersTable != null && usersTable.GetChanges() != null)
            {
                var answer = MessageBox.Show("Есть несохранённые изменения. Всё равно закрыть окно?",
                    "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (answer == DialogResult.No) e.Cancel = true;
            }
        }
    }
}
