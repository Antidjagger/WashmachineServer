using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace WashmachineServer.MessageHandling
{
    public struct WashTime
    {
        public ushort Day;
        public ushort Month;
        public ushort Year;
        public ushort TimeInterval;
    }



    public class User
    {
        private long _UserID;
        private Dictionary<WashTime, bool> WashList;


        public long UserID { get => _UserID; set => _UserID = value; }

        public User (long userID)
        {
            //Load user info from googleSheets
            WashList = new Dictionary<WashTime, bool>();
        }

        public bool IsWash(WashTime date)
        {
            return WashList[date];
        }

    }
}
