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
        //Опционально для подключения через запись в файле конфигурации
        //private const string ConnectionString = _configuration["Config:ConnectionString"];

        public ConnectToDB(Microsoft.Extensions.Configuration.IConfiguration _configuration)
        {
            switch (_configuration["ConnectionToDatabase:TakeCSFromConfig"])
            {
                case "true":
                    ConnectionString = _configuration["ConnectionToDatabase:ConnectionString"];
                    break;
                case "false":
                    ConnectionString = Environment.GetEnvironmentVariable(_configuration["ConnectionToDatabase:EnvironmentCS"]);
                    break;
                default:
                    //Здесь нужно реализовать запись в локальный лог-файл
                    ConnectionString = null;
                    break;
            }
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
            DB_Connection.CloseAsync();
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
            DB_Connection.CloseAsync();
        }
        public void MsgAPILogWriting(string text)
        {
            string DB_Query = "INSERT INTO \"ApiMessageLog\"(\"LogText\") VALUES (@LogText)";

            NpgsqlConnection DB_Connection = new NpgsqlConnection(ConnectionString);
            DB_Connection.Open();
            NpgsqlCommand DB_Command = new NpgsqlCommand(DB_Query, DB_Connection);
            DB_Command.Parameters.AddWithValue("LogText", text);
            DB_Command.Prepare();

            DB_Command.ExecuteScalar();
            DB_Connection.CloseAsync();
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
            DB_Connection.CloseAsync();
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
                DB_Connection.CloseAsync();
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
                string DB_Query = "SELECT \"IsRegistred\" FROM \"Users\" WHERE \"UserID\" = " + UserID.ToString();

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
                        DB_Connection.CloseAsync();
                        return res;

                    }
                    ///<remarks>
                    ///TODO: Нужно будет добавить набор исключений для случаев, когда нет связи с БД. В принципе, пользователя это не должно волновать, но из админ-панели
                    ///должна быть возможность просмотреть все возможные проблемы без перезапуска сервера
                    ///</remarks>
                    catch
                    {
                        DB_Connection.CloseAsync(); 
                        ErrorLogWriting("The value could not be retrieved from the database or the value was read incorrectly", 2); 
                    }
                    
                }
                
            }
            else
            {
                ErrorLogWriting("The user was polled, but was not added to the database/was removed from the database", 2);
                throw new Exception("The record about user with id " + UserID.ToString() + " does not exist in the database");
            }
            return false;
        }
        //Для конкретной даты
        public bool IsUserRecordsExist(long UserID, string interval)
        {
            NpgsqlConnection DB_Connection = new NpgsqlConnection(ConnectionString);
            try
            {
                string DB_Query = "SELECT EXISTS (SELECT 1 FROM  \"Records\" WHERE (\"RecordDate\" = @UserDate) AND (\"_UserId\" = @UserID))";
                NpgsqlCommand DB_Command = new NpgsqlCommand(DB_Query, DB_Connection);
                DB_Connection.Open();
                DB_Command.Parameters.AddWithValue("UserID", UserID);
                DB_Command.Parameters.AddWithValue("UserDate", "'"+interval+"'");
                DB_Command.Prepare();
                bool res = (bool)DB_Command.ExecuteScalar();
                DB_Connection.CloseAsync();
                return res;
            }
            catch 
            { 
                ErrorLogWriting("The value could not be retrieved from the database or the value was read incorrectly (Can't check user's records on existing in user-writed diapazone)", 2);
                return false;
            }

        }
        //Для заготовленных промежутков
        public bool IsUserRecordsExist(long UserID, Int16 interval)
        {
            NpgsqlConnection DB_Connection = new NpgsqlConnection(ConnectionString); 

            string DB_Query = ""; 
            switch (interval)
            {
                /// <summary>
                /// Нижепреведённые запросы составлены с учётом того, что время сервера - GMT+0, а время клиентов - московское (GMT+3)
                /// В дальнейшем в конфигурации будут добавлены соответствующие настройки
                /// </summary>
                case 3:
                    DB_Query = "SELECT EXISTS (SELECT 1 FROM  \"Records\" WHERE (\"RecordDate\" BETWEEN (CURRENT_DATE + INTERVAL '3 hours') AND (CURRENT_DATE + INTERVAL '1 week' + INTERVAL '3 hours')) AND (\"_UserId\" = @UserID))";
                    break;
                case 2:
                    DB_Query = "SELECT EXISTS (SELECT 1 FROM  \"Records\" WHERE (\"RecordDate\" BETWEEN (CURRENT_DATE + INTERVAL '1 week' + INTERVAL '3 hours' ) AND (CURRENT_DATE + INTERVAL '2 week'+ INTERVAL '3 hours')) AND (\"_UserId\" =@UserID))";
                    break;
                case 1:
                    DB_Query = "SELECT EXISTS (SELECT 1 FROM  \"Records\" WHERE (\"RecordDate\" = CURRENT_DATE + INTERVAL '3 hours') AND (\"_UserId\" = @UserID))";
                    break;
                case 4:
                    DB_Query = "SELECT EXISTS (SELECT 1 FROM  \"Records\" WHERE (\"RecordDate\" BETWEEN (CURRENT_DATE + INTERVAL '3 hours') AND (CURRENT_DATE + INTERVAL '1 month' + INTERVAL '3 hours')) AND (\"_UserId\" = @UserID))";
                    break;
                case 5:
                    DB_Query = "SELECT EXISTS (SELECT 1 FROM  \"Records\" WHERE (\"RecordDate\" BETWEEN (CURRENT_DATE - INTERVAL '1 week' + INTERVAL '3 hours') AND (CURRENT_DATE + INTERVAL '3 hours')) AND (\"_UserId\" = @UserID))";
                    break;
                case 6:
                    DB_Query = "SELECT EXISTS (SELECT 1 FROM  \"Records\" WHERE (\"RecordDate\" BETWEEN (CURRENT_DATE - INTERVAL '1 month' + INTERVAL '3 hours') AND (CURRENT_DATE + INTERVAL '3 hours')) AND (\"_UserId\" = @UserID))";
                    break;
                default:
                    return false;
            }
            NpgsqlCommand DB_Command = new NpgsqlCommand(DB_Query, DB_Connection);
            DB_Connection.Open();
            DB_Command.Parameters.AddWithValue("UserID", UserID);
            DB_Command.Prepare();
            bool res = (bool)DB_Command.ExecuteScalar();
            DB_Connection.CloseAsync();
            return res;
        }

        public string [] GetUserRecords(long UserID, Int16 interval)
        {
            
            string DB_Query = "";
            string DB_Query2 = "";
            switch (interval)
            {
                /// <summary>
                /// Нижепреведённые запросы составлены с учётом того, что время сервера - GMT+0, а время клиентов - московское (GMT+3)
                /// В дальнейшем в конфигурации будут добавлены соответствующие настройки
                /// </summary>
                case 3:
                    DB_Query = "SELECT COUNT(*) FROM \"Records\" WHERE (\"RecordDate\" BETWEEN (CURRENT_DATE + INTERVAL '3 hours') AND (CURRENT_DATE + INTERVAL '1 week' + INTERVAL '3 hours')) AND (\"_UserId\" = @UserID)";
                    DB_Query2 = "SELECT \"RecordDate\", \"RecordTimezone\" FROM \"Records\" WHERE (\"RecordDate\" BETWEEN (CURRENT_DATE + INTERVAL '3 hours') AND (CURRENT_DATE + INTERVAL '1 week' + INTERVAL '3 hours')) AND (\"_UserId\" = @UserID)";
                    break;
                case 2:
                    DB_Query = "SELECT COUNT(*) FROM \"Records\" WHERE (\"RecordDate\" BETWEEN (CURRENT_DATE + INTERVAL '1 week' + INTERVAL '3 hours' ) AND (CURRENT_DATE + INTERVAL '2 week'+ INTERVAL '3 hours')) AND (\"_UserId\" = @UserID)";
                    DB_Query2 = "SELECT \"RecordDate\", \"RecordTimezone\" FROM \"Records\" WHERE (\"RecordDate\" BETWEEN (CURRENT_DATE + INTERVAL '1 week' + INTERVAL '3 hours' ) AND (CURRENT_DATE + INTERVAL '2 week'+ INTERVAL '3 hours')) AND (\"_UserId\" = @UserID)";
                    break;
                case 1:
                    DB_Query = "SELECT COUNT(*) FROM \"Records\" WHERE (\"RecordDate\" = CURRENT_DATE + INTERVAL '3 hours') AND (\"_UserId\" = @UserID)";//!!!
                    DB_Query2 = "SELECT \"RecordDate\", \"RecordTimezone\" FROM \"Records\" WHERE (\"RecordDate\" = CURRENT_DATE + INTERVAL '3 hours') AND (\"_UserId\" = @UserID)";
                    break;
                case 4:
                    DB_Query = "SELECT COUNT(*) FROM \"Records\" WHERE (\"RecordDate\" BETWEEN (CURRENT_DATE + INTERVAL '3 hours') AND (CURRENT_DATE + INTERVAL '1 month' + INTERVAL '3 hours')) AND (\"_UserId\" = @UserID)";
                    DB_Query2 = "SELECT \"RecordDate\", \"RecordTimezone\" FROM \"Records\" WHERE (\"RecordDate\" BETWEEN (CURRENT_DATE + INTERVAL '3 hours') AND (CURRENT_DATE + INTERVAL '1 month' + INTERVAL '3 hours')) AND (\"_UserId\" = @UserID)";
                    break;
                case 5:
                    DB_Query = "SELECT COUNT(*) FROM \"Records\" WHERE (\"RecordDate\" BETWEEN (CURRENT_DATE - INTERVAL '1 week' + INTERVAL '3 hours') AND (CURRENT_DATE + INTERVAL '3 hours')) AND (\"_UserId\" = @UserID)";
                    DB_Query2 = "SELECT \"RecordDate\", \"RecordTimezone\" FROM \"Records\" WHERE (\"RecordDate\" BETWEEN (CURRENT_DATE - INTERVAL '1 week' + INTERVAL '3 hours') AND (CURRENT_DATE + INTERVAL '3 hours')) AND (\"_UserId\" = @UserID)";
                    break;
                case 6:
                    DB_Query = "SELECT COUNT(*) FROM \"Records\" WHERE (\"RecordDate\" BETWEEN (CURRENT_DATE - INTERVAL '1 month' + INTERVAL '3 hours') AND (CURRENT_DATE + INTERVAL '3 hours')) AND (\"_UserId\" = @UserID)";
                    DB_Query2 = "SELECT \"RecordDate\", \"RecordTimezone\" FROM \"Records\" WHERE (\"RecordDate\" BETWEEN (CURRENT_DATE - INTERVAL '1 month' + INTERVAL '3 hours') AND (CURRENT_DATE + INTERVAL '3 hours')) AND (\"_UserId\" = @UserID)";
                    break;
                default:
                    return null;
            }
            NpgsqlConnection DB_Connection = new NpgsqlConnection(ConnectionString);
            NpgsqlCommand DB_CommandCount = new NpgsqlCommand(DB_Query, DB_Connection);
            NpgsqlCommand DB_CommandReader = new NpgsqlCommand(DB_Query2, DB_Connection);
            DB_Connection.Open();
            DB_CommandCount.Parameters.AddWithValue("UserID", UserID);
            DB_CommandCount.Prepare();
            Int16 count = (short)DB_CommandReader.ExecuteScalar();
            string[] temp = new string[count];
            DB_CommandReader.Parameters.AddWithValue("UserID", UserID);
            DB_CommandReader.Prepare();
            NpgsqlDataReader reader;
            DbDataView dataView = new DbDataView();
            reader = DB_CommandReader.ExecuteReader();
            //Нужно добавить ловлю исключений на случай, если вдруг кто-то умудрится в момент получения данных вклинить-таки свой запрос на отправку данных
            while (reader.Read())
            {
                int i = 0;
                temp[i] = reader.GetString(0) + " в " + dataView.dbTimeIntervalView(reader.GetInt16(1));
                DB_Connection.CloseAsync();
                i++;
            }
            return temp;
        }
        //public bool GetUserRecords(long UserID, Int16 interval)
        //{

        //}

        public Int16 GetUserDialogStage(long UserID)
        {
            string DB_Query = "SELECT \"DialogStage\" FROM \"Users\" WHERE \"UserID\" = " + UserID.ToString();

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
                    DB_Connection.CloseAsync();
                    return res;
                }
                catch
                {
                    DB_Connection.CloseAsync(); 
                    ErrorLogWriting("The value could not be retrieved from the database or the value was read incorrectly", 2); 
                }
            }
            DB_Connection.CloseAsync();
            return -1;
        }
        public void SetUserDialogStage(long UserID, Int16 DS)
        {
            try
            {
                string DB_Query = "UPDATE \"Users\" SET \"DialogStage\" = "+ DS.ToString() +" WHERE \"UserID\" = @UserID";

                NpgsqlConnection DB_Connection = new NpgsqlConnection(ConnectionString);
                NpgsqlCommand DB_Command = new NpgsqlCommand(DB_Query, DB_Connection);
                DB_Connection.Open();
                DB_Command.Parameters.AddWithValue("UserID", UserID);
                DB_Command.Prepare();
                DB_Command.ExecuteScalar();
                DB_Connection.CloseAsync();
                //здесь надо бы добавить отправку лога в отдельный лог-файл для пользователей
            }
            catch
            {
                ErrorLogWriting("Failed to change DialogStage for user " + UserID.ToString(), 2);
            }
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
                catch 
                {
                    DB_Connection.CloseAsync();
                    ErrorLogWriting("The value could not be retrieved from the database or the value was read incorrectly", 2); 
                }
            }
            DB_Connection.CloseAsync();
            return UserList;
                
        }

    }
}
