using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WashmachineServer.MessageHandling
{
    public class DbDataView
    {
        public DbDataView()
        {
            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ti">Принимает номер интервала из базы данных</param>
        /// <returns>Возвращает понятный пользователю текст со временем его записи</returns>
        public string dbTimeIntervalView(Int16 ti)
        {
            switch (ti)
            {
                case 0:
                    return "00:00-03:00";
                case 1:
                    return "03:00-06:00";
                case 2:
                    return "06:00-09:00";
                case 3:
                    return "09:00-12:00";
                case 4:
                    return "12:00-15:00";
                case 5:
                    return "15:00-18:00";
                case 6:
                    return "18:00-21:00";
                case 7:
                    return "21:00-00:00";
                default:
                    break;
            }
            return "";
        }

        public string ConvertFromPostgreDate(string Date)
        {
            string[] NumMas = Regex.Split(Date, "[-]");
            string dd = NumMas[2];
            string mm = NumMas[1];
            string yy = NumMas[0];
            return dd + "." + mm + "." + yy;
        }
        //Переводит представление "ДДММГГ" в "ДД-месяц-ГГ"
        public string NumDateToText(string Date, bool trimYear)
        {
            string[] NumMas = Regex.Split(Date, "[.]");
            string dd = NumMas[0];
            string mm = NumMas[1];
            string yy = NumMas[2];
            string temp = dd + " ";
            switch (mm)
            {
                case "1":
                    temp += "января";
                    break;
                case "2":
                    temp += "февраля";
                    break;
                case "3":
                    temp += "марта";
                    break;
                case "4":
                    temp += "апреля";
                    break;
                case "5":
                    temp += "мая";
                    break;
                case "6":
                    temp += "июня";
                    break;
                case "7":
                    temp += "июля";
                    break;
                case "8":
                    temp += "августа";
                    break;
                case "9":
                    temp += "сентября";
                    break;
                case "10":
                    temp += "октября";
                    break;
                case "11":
                    temp += "ноября";
                    break;
                case "12":
                    temp += "декабря";
                    break;
                default:
                    break;
            }
            if (trimYear)
                return temp;
            else
                return temp += " " + yy;
        }
        public string WeekDayTranslation(string ENG_weekday)
        {
            string weekday = ENG_weekday.Trim(' ');
            switch (weekday.ToLower())
            {
                case "monday":
                    return "Понедельник";
                case "tuesday":
                    return "Вторник";
                case "wednesday":
                    return "Среда";
                case "thursday":
                    return "Четверг";
                case "friday":
                    return "Пятница";
                case "saturday":
                    return "Суббота";
                case "sunday":
                    return "Воскресенье";
                default:
                    return "Wednesday, ma dudes!";
            }

        }

        //Здесь будет метод переработки данных из БД в изображение
        public void TextToTableIMG()
        {

        }


    }
}
