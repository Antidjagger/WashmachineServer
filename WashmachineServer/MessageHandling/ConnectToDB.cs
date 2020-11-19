using System;
using System.Collections.Generic;
using System.Data;
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
        public string GetConnectionString()
        {
            return ConnectionString;
        }
        //Отправка лога ошибок в таблицу БД
        public void ErrorLogWriting(string errorText,Int16 errorType)
        {
            string DB_Query = "INSERT INTO \"ErrorLog\"(ErrorText, ErrorType) VALUES ( " + errorText + "," + errorType.ToString() + ")";

            NpgsqlConnection DB_Connection = new NpgsqlConnection(ConnectionString);
            NpgsqlCommand DB_Command = new NpgsqlCommand(DB_Query, DB_Connection);
            DB_Connection.Open(); //Открываем соединение.
            DB_Command.ExecuteScalar();
            DB_Connection.Close();
        }
        //Отправка основного лога в БД
        public void MainLogWriting(string text)
        {
            string DB_Query = "INSERT INTO \"MainLog\"(LogText) VALUES ( " + text + ")";

            NpgsqlConnection DB_Connection = new NpgsqlConnection(ConnectionString);
            NpgsqlCommand DB_Command = new NpgsqlCommand(DB_Query, DB_Connection);
            DB_Connection.Open(); //Открываем соединение.
            DB_Command.ExecuteScalar();
            DB_Connection.Close();
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
            bool res = (bool)DB_Command.ExecuteScalar();
            DB_Connection.Close();
            return res;

        }
        //Добавление записи о пользователе в БД
        public bool AddNewUser(long UserID)
        {
            try
            {
                string DB_Query = "INSERT INTO \"Users\"(UserID) VALUES ( " + UserID.ToString() + ")";

                NpgsqlConnection DB_Connection = new NpgsqlConnection(ConnectionString);
                NpgsqlCommand DB_Command = new NpgsqlCommand(DB_Query, DB_Connection);
                DB_Connection.Open();
                bool res = (bool)DB_Command.ExecuteScalar();
                DB_Connection.Close();
                MainLogWriting("Была добавлена запись о пользователе " + UserID.ToString());
                return res;
            }
            catch 
            {
                ErrorLogWriting("Не удалось добавить пользователя в базу данных", 2);
                return false;
            }
        }
        //Метод добавления нового пользователя, для начала нужно получить сообщение-подтверждение с ключом доступа администратора, 
        //Далее, прямо отсюда будет сделан запрос на соответствие ключа записанному, далее - изменение статуса пользователя
        public void UserRegistration(string UserId, string Key)
        {

        }
        //Возвращает true, если пользователь зарегистрирован
        public bool IsUserRegistred(long UserID)
        {
            if (IsUserExist(UserID))
            {
                string DB_Query = "SELECT \"IsRegistred\" FROM \"Users\" WHERE \"UserID\" = " + UserID.ToString() + ")";

                NpgsqlConnection DB_Connection = new NpgsqlConnection(ConnectionString);
                NpgsqlCommand DB_Command = new NpgsqlCommand(DB_Query, DB_Connection);
                DB_Connection.Open(); 
                NpgsqlDataReader reader;
                reader = DB_Command.ExecuteReader();

                bool res;
                while (reader.Read())
                {
                    try
                    {
                        res = reader.GetBoolean(0);
                        return res;
                    }
                    ///<remarks>
                    ///TODO: Нужно будет добавить набор исключений для случаев, когда нет связи с БД. В принципе, пользователя это не должно волновать, но из админ-панели
                    ///должна быть возможность просмотреть все возможные проблемы без перезапуска сервера
                    ///</remarks>
                    catch { ErrorLogWriting("Не удалось получить значение из базы данных либо значение было прочитано неверно", 2); }
                }
                DB_Connection.Close();
                
            }
            else
            {
                ErrorLogWriting("Пользователь опрошен, но не был добавлен в базу данных/был удалён из базы", 2);
                throw new Exception("Пользователь не существует в базе данных");

            }
            return false;
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
                catch { ErrorLogWriting("Не удалось получить значение из базы данных либо значение было прочитано неверно", 2); }
            }
            DB_Connection.Close();
            return UserList;
                
        }

    }
}
