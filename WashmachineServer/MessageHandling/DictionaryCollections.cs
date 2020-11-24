using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WashmachineServer.MessageHandling
{
    /// <summary>
    /// Этот класс создан для методов и переменных работы с пользовательским диалогом
    /// В т.ч для проверки пользовательских ответов на соответствие, и.т.д
    /// </summary>
    public class DictionaryCollections
    {
        //Словари позднее надо бы переписать с учётом применения регулярных выражений в методах
        public readonly List<string> DS_1_GetRecords = new List<string>() {"1", "1.", "просмотреть свои записи на стирку", "просмотреть записи", "посмотреть записи", "записи", "мои записи", "записи на стирку", "список записей" };
        public readonly List<string> DS_1_SetRecords = new List<string>() { "2", "2.", "записаться на стирку", "записаться", "запись на стирку", "хочу записаться на стирку" };
        public readonly List<string> DS_1_1_GR_Today = new List<string>() { "1", "1.", "на сегодня", "сегодня", "записи на сегодня" };
        public readonly List<string> DS_1_1_GR_ThisWeek = new List<string>() { "2", "2.", "эта неделя", "на этой неделе", "текущая неделя", "неделя", "на неделю", "записи на эту неделю", "записи на текущую неделю", "записи на этой неделе" };
        public readonly List<string> DS_1_1_GR_NextWeek = new List<string>() { "3", "3.", "следующая неделя", "на следующей неделе", "на следующую неделю", "записи на следующую неделю", "записи на следующей неделе" };
        public readonly List<string> DS_1_1_GR_ThisMonth = new List<string>() { "4", "4.", "этот месяц", "в этом месяце", "записи в этом месяце", "на этот месяц", "текущий месяц", "записи на текущий месяц", "записи на этот месяц" };
        public readonly List<string> DS_1_1_GR_LastWeek = new List<string>() { "5", "5.", "прошедшая неделя", "на прошедшую неделю", "записи на прошедшей неделе", "на прошедшей неделе", "прошлая неделя", "записи на прошлой неделе", "предыдущая неделя", "записи на предыдущей неделе", "на предыдущей неделе", "записи на прошлую неделю" };
        public readonly List<string> DS_1_1_GR_LastMonth = new List<string>() { "6", "6.", "прошлый месяц", "записи на прошлый месяц", "прошедший месяц", "записи на прошедший месяц", "записи в прошлом месяце", "в прошлом месяце", "на прошлый месяц", "на прошедший месяц", "на предыдущий месяц", "предыдущий месяц", "записи на предыдущий месяц" };
        private int IsKey;
        public DictionaryCollections(int isKey)
        { IsKey = isKey; }
        //Проверяет, соответствует ли строка условному формату даты 
        //Возможно, к слиянию с ConvertToPostgreDate
        public bool IsInDateFormat(string Date)
        {
            Regex MMDDYYYY = new Regex(@"^[0-9]{2}[.][0-9]{2}[.][0-9]{4}");
            Regex MMDDYY = new Regex(@"^[0-9]{2}[.][0-9]{2}[.][0-9]{2}");
            if ((MMDDYYYY.IsMatch(Date)) || (MMDDYY.IsMatch(Date)))
            {
                return true;
            }
            else
            {
                return false;
            } 
        }
        //Конвертирует строку с датой в формат PostgreSQL (MM.DD.YY) либо возвращает пустую строку в случае, если дата некорректна (хотя и соответствует формату...)
        public string ConverToPostgreDate(string Date)
        {
            string[] NumMas = Regex.Split(Date, "[.]");
            Int16 dd = Int16.Parse(NumMas[0]);
            Int16 mm = Int16.Parse(NumMas[1]);
            Int16 yy = Int16.Parse(NumMas[2]);
            if ((mm < 12)&&(mm > 0))
            {
                if (mm == 2)
                {
                    //Здесь намеренно не приведена проверка на соответствие условию "каждый четвёртый, оканчивающийся на 00", т.к срок службы приложения не рассчитан на такой временной промежуток
                    if (yy%4 == 0)
                    {
                        if ((dd > 0)&&(dd <=29))
                        {
                            return mm.ToString() + "." + dd.ToString() + "." + yy.ToString();
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        if ((dd > 0) && (dd <= 28))
                        {
                            return mm.ToString() + "." + dd.ToString() + "." + yy.ToString();
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                if ((mm == 4)||(mm == 6)||(mm == 9)||(mm == 11))
                {
                    if ((dd > 0) && (dd <= 30))
                    {
                        return mm.ToString() + "." + dd.ToString() + "." + yy.ToString();
                    }
                    else
                    {
                        return null;
                    }
                }
                if ((mm == 1) || (mm == 3) || (mm == 5) || (mm == 7)||(mm==8)||(mm == 10)||(mm == 12))
                {
                    if ((dd > 0) && (dd <= 31))
                    {
                        return mm.ToString() + "." + dd.ToString() + "." + yy.ToString();
                    }
                    else
                    {
                        return null;
                    }
                }
                return null;
            }
            else
            {
                return null;
            }
            
        }
        //Работа с DialogStage 1 (ответы на главное меню)
        public Int16 DS_1(string msg)
        {
            if (DS_1_GetRecords.Contains(msg))
            {
                return 1;
            }
            else
            {
                if (DS_1_SetRecords.Contains(msg))
                {
                    return 2;
                }
                else
                {
                    if (msg == "отмена")
                    {
                        return 0;
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
        }
        //Работа с DialogStage 1_1 (ответы на меню вывода записей)
        public Int16 DS_1_1(string msg)
        {
            if (DS_1_1_GR_Today.Contains(msg))
            {
                return 1;
            }
            else
            {
                if (DS_1_1_GR_ThisWeek.Contains(msg))
                {
                    return 2;
                }
                else
                {
                    if (DS_1_1_GR_NextWeek.Contains(msg))
                    {
                        return 3;
                    }
                    else
                    {
                        if (DS_1_1_GR_ThisMonth.Contains(msg))
                        {
                            return 4;
                        }
                        else
                        {
                            if (DS_1_1_GR_LastWeek.Contains(msg))
                            {
                                return 5;
                            }
                            else
                            {
                                if (DS_1_1_GR_LastMonth.Contains(msg))
                                {
                                    return 6;
                                }
                                else
                                {
                                    if (msg == "отмена")
                                    {
                                        return 0;
                                    }
                                    else
                                    {
                                        return -1;
                                    }
                                }
                            }
                        }
                    }

                    
                }
            }
        }
        //Работа с DialogStage 1_2 (ответы на меню создания записей)
        public Int16 DS_1_2(string msg)
        {
            return 0;
        }
    }
}
