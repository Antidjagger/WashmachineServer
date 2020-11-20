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
            string DB_Query = "INSERT INTO \"ErrorLog\"(\"ErrorText\", \"ErrorType\") VALUES (@errorText, @errorType)";
            NpgsqlConnection DB_Connection = new NpgsqlConnection(ConnectionString);
            DB_Connection.Open(); //Открываем соединение.
            
            NpgsqlCommand DB_Command = new NpgsqlCommand(DB_Query, DB_Connection);
     
            DB_Command.Parameters.AddWithValue("errorText", errorText);
            DB_Command.Parameters.AddWithValue("errorType", errorType);
            DB_Command.Prepare();
            DB_Command.ExecuteScalar();
            DB_Connection.Close();
        }
        //Отправка основного лога в БД
        public void MainLogWriting(string text)
        {
            string DB_Query = "INSERT INTO \"MainLog\"(\"LogText\") VALUES (@LogText)";

            NpgsqlConnection DB_Connection = new NpgsqlConnection(ConnectionString);
            DB_Connection.Open(); 
            NpgsqlCommand DB_Command = new NpgsqlCommand(DB_Query, DB_Connection);
            DB_Command.Parameters.AddWithValue("LogText", text);
            DB_Command.Prepare();
           
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
                string DB_Query = "INSERT INTO \"Users\"(\"UserID\") VALUES (@UserID)";

                NpgsqlConnection DB_Connection = new NpgsqlConnection(ConnectionString);
                NpgsqlCommand DB_Command = new NpgsqlCommand(DB_Query, DB_Connection);
                DB_Connection.Open();
                DB_Command.Parameters.AddWithValue("UserID", UserID);
                DB_Command.Prepare();
                bool res = (bool)DB_Command.ExecuteScalar();
                DB_Connection.Close();
                MainLogWriting("An record about user with ID " + UserID.ToString() + " was added");
                return res;
            }
            catch 
            {
                ErrorLogWriting("Failed to add record about user " + UserID.ToString() + " to database", 2);
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
                    catch { ErrorLogWriting("The value could not be retrieved from the database or the value was read incorrectly", 2); }
                }
                DB_Connection.Close();
                
            }
            else
            {
                ErrorLogWriting("The user was polled, but was not added to the database/was removed from the database", 2);
                throw new Exception("The record about user with id " + UserID.ToString() + " does not exist in the database");

            }
            return false;
        }
        public Int16 GetUserDialogStage(long UserID)
        {
            string DB_Query = "SELECT \"DialogStage\" FROM \"Users\" WHERE \"UserID\" = " + UserID.ToString() + ")";

            NpgsqlConnection DB_Connection = new NpgsqlConnection(ConnectionString);
            NpgsqlCommand DB_Command = new NpgsqlCommand(DB_Query, DB_Connection);
            DB_Connection.Open();
            NpgsqlDataReader reader;
            reader = DB_Command.ExecuteReader();

            Int16 res;
            while (reader.Read())
            {
                try
                {
                    res = reader.GetInt16(0);
                    return res;
                }
                catch { ErrorLogWriting("The value could not be retrieved from the database or the value was read incorrectly", 2); }
            }
            DB_Connection.Close();
            return -1;
        }
        public void SetUserDialogStage(long UserID)
        {

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
                catch { ErrorLogWriting("The value could not be retrieved from the database or the value was read incorrectly", 2); }
            }
            DB_Connection.Close();
            return UserList;
                
        }

    }
}
