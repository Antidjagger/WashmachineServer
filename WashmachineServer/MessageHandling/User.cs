using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace WashmachineServer.MessageHandling
{
    /// <summary>
    /// Возможно, лишний для работы программы класс. Пока что его функционал - на будущее, т.к все данные можно опрашивать из БД,
    /// а стейты диалогов VKapi присылает прямо в запросе
    /// </summary>
    public class User
    {
        private long _UserID;
        public bool IsAdmin;
        private string _AdminPassowrd;
        public long UserID { get => _UserID; set => _UserID = value; }
        public string AdminPassowrd { get => _AdminPassowrd; set => _AdminPassowrd = value; }

        public User (long userID, bool isAdmin)
        {
            UserID = userID;
            IsAdmin = isAdmin;
            AdminPassowrd = "";
        }
    }
}
