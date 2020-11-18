using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace WashmachineServer.MessageHandling
{
    /// <summary>
    /// Основной класс для работы с PostgreSQL, в дальнейшем проработаю настройку connect string через appsettings.json, а так же выбор типа базы данных
    /// </summary>
    public class ConnectToDB
    {
        //Строка подключения
        private string ConnectionString;
        //private const string ConnectionString = "";
        public ConnectToDB()
        {
            //Получаю строку из переменных окружения
            ConnectionString = Environment.GetEnvironmentVariable("EnvironmentCS");
        }
        /// <summary>
        /// Проверка, есть ли пользователь в списках БД
        /// </summary>
        /// <param name="UserID">ID пользователя Вконтакте, присланное в запросе от VkApi</param>
        /// <returns>Возвращает true/false </returns>
        public bool IsUserExist(long UserID)
        {
            string DB_Query = "SELECT EXISTS (SELECT 1 FROM \"Users\" WHERE \"UserID\" = " + UserID.ToString() + ")";

            NpgsqlConnection DB_Connection = new NpgsqlConnection(ConnectionString);
            NpgsqlCommand DB_Command = new NpgsqlCommand(DB_Query, DB_Connection);
            DB_Connection.Open(); //Открываем соединение.
            //NpgsqlDataReader reader;
            //reader = DB_Command.ExecuteReader();
            bool res = (bool)DB_Command.ExecuteScalar();
            DB_Connection.Close();
            return res;

        }
        /// <summary>
        /// Заполнение списка List<> информацией о ID существующих в записях БД пользователей
        /// В дальнейшем будет расширено до получения полного расклада по пользователям (необходимо для админ-панели)
        /// </summary>
        /// <returns>Возвращает System.Collections.Generic.List<long> со списком ID пользователей из БД</returns>
        public List<long> GetUserList()
        {
            List<long> UserList = new List<long>();
            string DB_Query = "SELECT \"UserID\" FROM \"Users\"";
           
            NpgsqlConnection DB_Connection = new NpgsqlConnection(ConnectionString);
            NpgsqlCommand DB_Command = new NpgsqlCommand(DB_Query, DB_Connection);
            DB_Connection.Open(); //Открываем соединение.
            NpgsqlDataReader reader;
            reader = DB_Command.ExecuteReader();
            long temp = 0;
            while (reader.Read())
            {
                try
                {
                    temp = reader.GetInt64(0);
                    UserList.Add(temp);
                }
                ///<remarks>
                ///TODO: Нужно будет добавить набор исключений для случаев, когда нет связи с БД. В принципе, пользователя это не должно волновать, но из админ-панели
                ///должна быть возможность просмотреть все возможные проблемы без перезапуска сервера
                ///</remarks>
                catch { }
            }
            DB_Connection.Close();
            return UserList;
                
        }

    }
}
