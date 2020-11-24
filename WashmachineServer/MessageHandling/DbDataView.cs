using System;
using System.Collections.Generic;
using System.Linq;
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

        //Здесь будет метод переработки данных из БД в изображение
        public void TextToTableIMG()
        {

        }


    }
}
