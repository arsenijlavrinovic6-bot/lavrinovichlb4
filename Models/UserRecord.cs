namespace Lavrinovich.Models
{
    // Модель строки таблицы Users. Основная привязка выполняется через DataTable/BindingSource.
    public class UserRecord
    {
        public int ID { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string First_Name { get; set; }
        public string Role_Name { get; set; }
    }
}
