using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using VkNet.Model;
using VkNet.Utils;
using System;
using VkNet.Abstractions;
using VkNet.Model.RequestParams;
using System.Collections;
using VkNet.Enums.SafetyEnums;
using VkNet.Model.Attachments;
using System.Collections.Generic;
using WashmachineServer.MessageHandling;
using System.Text.RegularExpressions;

namespace WashmachineServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallbackController : ControllerBase
    {
        /// <summary>
        /// Конфигурация приложения
        /// </summary>
        private readonly IConfiguration _configuration;
        private readonly IVkApi _vkApi;
       
        //Конструктор
        public CallbackController(IVkApi vkApi, IConfiguration configuration)
        {
            _vkApi = vkApi;
            _configuration = configuration;
        }


        [HttpPost]
        public IActionResult Callback([FromBody] Updates updates)
        {
            // Тип события
            switch (updates.Type)
            {
                // Ключ-подтверждение
                case "confirmation":
                    {
                        return Ok(_configuration["Config:Confirmation"]);
                    }
                // Новое сообщение
                case "message_new":
                    {
                        // Десериализация
                        var msg = Message.FromJson(new VkResponse(updates.Object));
                        ConnectToDB connectToDB = new ConnectToDB();
                        /// <summary>
                        /// Начинаем работу с пользователем с очередной проверки, есть ли он в списке
                        /// Временно, я буду просто рассказывать ему, что он есть в списке либо отсутствует в нём
                        /// Возможно, приведённая ниже проверка - избыточна, к изменению?
                        /// </summary>
                        if (connectToDB.IsUserExist(msg.PeerId.Value))
                        {
                            try
                            {
                                if (connectToDB.IsUserRegistred(msg.PeerId.Value))
                                {
                                    
                                    long UserID = msg.PeerId.Value;
                                    Int16 UserDS = connectToDB.GetUserDialogStage(UserID);
                                    switch (UserDS)
                                    {
                                        //Главное меню
                                        case 0:
                                            connectToDB.SetUserDialogStage(UserID, DS_0(UserID));
                                            break;
                                        //Выбор вариантов в главном меню
                                        case 1:
                                            connectToDB.SetUserDialogStage(UserID, DS_1(UserID, msg.Text));
                                            
                                            break;
                                        //Просмотреть свои записи: выбор вариантов
                                        case 11:
                                            connectToDB.SetUserDialogStage(UserID, DS_1_1(UserID, msg.Text,connectToDB));
                                            break;
                                        //Записаться на стирку: выбор вариантов
                                        case 12:
                                            connectToDB.SetUserDialogStage(UserID, DS_1_2(UserID, msg.Text));
                                            break;
                                        //Если вылезло за пределы
                                        default:
                                            //Здесь нужно добавить отправку лога в отдельную таблицу
                                            SendMessage(msg.PeerId.Value, "Ошибка DialogStage! Сообщите администратору");
                                            connectToDB.SetUserDialogStage(UserID, DS_0(UserID));
                                            break;
                                    }



                                }
                                else
                                {
                                    //Здесь будет антиспам-функционал - при получении более 5 запросов от незарегистрированного пользователя (число запросов будет инкреминтироваться и храниться для каждого пользователя),
                                    //пользователь будет добавлен в чёрный список
                                    
                                    if (msg.ConversationMessageId > Int64.Parse(_configuration["Config:AntiSpam"]))
                                    {

                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                SendMessage(msg.PeerId.Value, "Произошла ошибка! Обратитесь к администратору с данным текстом ошибки: " + ex.Message);
                            }
                        }
                        else
                        {
                            /// <summary>
                            /// Если пользователь отсутствует в списках БД
                            /// Пока нет функци добавления пользователя и функций рут-пользователя (а так же, пока я не разобрался с контейнерами),
                            /// Будет просто отправка сообщения о запрете доступа, создание записи в БД и прекращение цепочки работы
                            /// </summary>
                            connectToDB.AddNewUser(msg.PeerId.Value);
                            SendMessage(msg.PeerId.Value, "Вы незарегистрированы!");
                        }



                        //var photos = _vkApi.Photo.Get(new PhotoGetParams
                        //{
                        //    AlbumId = PhotoAlbumType.Id(albumid),
                        //    OwnerId = 58910369
                        //});
                        break;
                    }
            }

            return Ok("ok");
        }

        /// <summary>
        /// Метод отправки сообщений с различными перегрузками
        /// На будущее: добавить возвращаемое значение для ловли ошибок и записи в лог 
        /// </summary>
        public void SendMessage(long peerID, string msg)
        {
            _vkApi.Messages.SendAsync(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerID,
                Message = msg
            });
        }
        //Отправка сообщения и фото
        public void SendMessage(long peerID, string msg, string way)
        {
            var uploadServer = _vkApi.Photo.GetMessagesUploadServer(peerID);
            var wc = new System.Net.WebClient();
            string url = "https://cwatch.comodo.com/images/web-malware-detection-remediation.png";
            byte[] imageByte = wc.DownloadData(url);
            var result = System.Text.Encoding.ASCII.GetString(wc.UploadData(uploadServer.UploadUrl,imageByte));
            //var result = System.Text.Encoding.ASCII.GetString(wc.UploadFile(uploadServer.UploadUrl, @"unnamed.jpg"));
            var photo = _vkApi.Photo.SaveMessagesPhoto(result);
            
            _vkApi.Messages.SendAsync(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerID,
                Message = msg,
                Attachments = new List<MediaAttachment>
                {
                    
                    photo[0]
                }
            });
        }

        private void AntiMat()
        {

        }

        public Int16 DS_0(long UserID)
        {
            string msg_reply = "Главное меню. Выберите варианты:\n 1. Просмотреть свои записи на стирку \n 2. Записаться на стирку\n В любой момент можно написать \"Отмена\" для возвращения в главное меню";
            SendMessage(UserID, msg_reply);
            return 1;
        }
        public Int16 DS_1(long UserID, string msg)
        {
            //В дальнейшем эту конструкцию надо бы оптимизировать, путём добавления алгоритма сравнения строк по близости к результату, с добавлением некоторых исключений для случаев,
            //в которых строка может относиться к разным выборам
            //(Не дай хаос кто-то увидит этот кошмар)
            string MsgToCase = msg.ToLower();
            string msg_reply = "";
            MsgToCase.TrimStart();
            MsgToCase.TrimEnd();
            switch (msg.ToLower())
            {
                case "1":
                    msg_reply = "Меню просмотра записей на стирку. Выберите варианты или введите конкретную дату в формате <ММ.ДД.ГГГГ>:\n1. За эту неделю\n2. За следующую неделю\n3. Сегодня\n4. За этот месяц\n5. За прошлую неделю\n6. За прошлый месяц\nВ любой момент можно написать \"Отмена\" для возвращения в главное меню";
                    SendMessage(UserID, msg_reply);
                    return 11;
                case "1.":
                    goto case "1";
                case "просмотреть свои записи на стирку":
                    goto case "1";
                case "просмотреть записи":
                    goto case "1";
                case "посмотреть записи":
                    goto case "1";
                case "записи":
                    goto case "1";
                case "мои записи":
                    goto case "1";
                case "записи на стирку":
                    goto case "1";
                case "2":
                    msg_reply = "Меню записи на стирку.\n Выберите варианты или введите конкретную дату в формате <ММ.ДД.ГГГГ>, чтобы посмотреть свободные места и записаться:\n1. На эту неделю\n2. На следующую неделю\n3. Сегодня\n4. Через неделю\n5. Через две недели\nВ любой момент можно написать \"Отмена\" для возвращения в главное меню";
                    SendMessage(UserID, msg_reply);
                    return 12;
                case "записаться на стирку":
                    goto case "2";
                case "записаться":
                    goto case "2";
                case "запись на стирку":
                    goto case "2";
                case "2.":
                    goto case "2";
                case "отмена":
                    msg_reply = "Возврат в главное меню...";
                    SendMessage(UserID, msg_reply);
                    return DS_0(UserID);
                default:
                    msg_reply = "Нет такого пункта!";
                    SendMessage(UserID, msg_reply);
                    return 1;
                
            }
        }
        public Int16 DS_1_1(long UserID, string msg, ConnectToDB connect)
        {
            string MsgToCase = msg.ToLower();
            string msg_reply = "";
            MsgToCase.TrimStart();
            MsgToCase.TrimEnd();
            switch (msg.ToLower())
            {
                //За эту неделю
                case "1":
                    if (connect.IsUserRecordsExist(UserID,1))
                    {
                        msg_reply = "У вас есть записи в этом интервале \nВозврат в главное меню...";
                        SendMessage(UserID, msg_reply);
                        return DS_0(UserID);
                    }
                    else 
                    {
                        msg_reply = "У вас нет записей на эту неделю. Повторно выберите интервал или напишите \"Отмена\" для возврата в главное меню:";
                        SendMessage(UserID, msg_reply);
                        return 11;
                    }
                case "1.":
                    goto case "1";
                case "эта неделя":
                    goto case "1";
                case "на эту неделю":
                    goto case "1";
                case "текущая неделя":
                    goto case "1";
                case "неделя":
                    goto case "1";
                //За следующую неделю
                case "2":
                    if (connect.IsUserRecordsExist(UserID, 2))
                    {
                        msg_reply = "У вас есть записи в этом интервале \nВозврат в главное меню...";
                        SendMessage(UserID, msg_reply);
                        return DS_0(UserID);
                    }
                    else
                    {
                        msg_reply = "У вас нет записей на следующую неделю. Повторно выберите интервал или напишите \"Отмена\" для возврата в главное меню:";
                        SendMessage(UserID, msg_reply);
                        return 11;
                    }
                case "следующая неделя":
                    goto case "2";
                case "на следующую неделю":
                    goto case "2";
                case "на следующей неделе":
                    goto case "2";
                case "2.":
                    goto case "2";
                //За сегодня
                case "3":
                    if (connect.IsUserRecordsExist(UserID, 3))
                    {
                        msg_reply = "У вас есть записи в этом интервале \nВозврат в главное меню...";
                        SendMessage(UserID, msg_reply);
                        return DS_0(UserID);
                    }
                    else
                    {
                        msg_reply = "У вас нет записей на сегодня. Повторно выберите интервал или напишите \"Отмена\" для возврата в главное меню:";
                        SendMessage(UserID, msg_reply);
                        return 11;
                    }
                case "3.":
                    goto case "2";
                case "сегодня":
                    goto case "2";
                case "на сегодня":
                    goto case "2";
                case "сейчас":
                    goto case "2";
                //За этот месяц
                case "4":
                    if (connect.IsUserRecordsExist(UserID, 4))
                    {
                        msg_reply = "У вас есть записи в этом интервале \nВозврат в главное меню...";
                        SendMessage(UserID, msg_reply);
                        return DS_0(UserID);
                    }
                    else
                    {
                        msg_reply = "У вас нет записей на этот месяц. Повторно выберите интервал или напишите \"Отмена\" для возврата в главное меню:";
                        SendMessage(UserID, msg_reply);
                        return 11;
                    }
                case "4.":
                    goto case "1";
                case "этот месяц":
                    goto case "1";
                case "текущий месяц":
                    goto case "1";
                case "месяц":
                    goto case "1";
                case "на этот месяц":
                    goto case "1";
                //За прошлую неделю
                case "5":
                    if (connect.IsUserRecordsExist(UserID, 5))
                    {
                        msg_reply = "У вас есть записи в этом интервале \nВозврат в главное меню...";
                        SendMessage(UserID, msg_reply);
                        return DS_0(UserID);
                    }
                    else
                    {
                        msg_reply = "У вас нет записей за прошлую неделю. Повторно выберите интервал или напишите \"Отмена\" для возврата в главное меню:";
                        SendMessage(UserID, msg_reply);
                        return 11;
                    }
                case "5.":
                    goto case "1";
                case "прошлая неделя":
                    goto case "1";
                case "прошедшая неделя":
                    goto case "1";
                //За прошлый месяц
                case "6":
                    if (connect.IsUserRecordsExist(UserID, 6))
                    {
                        msg_reply = "У вас есть записи в этом интервале \nВозврат в главное меню...";
                        SendMessage(UserID, msg_reply);
                        return DS_0(UserID);
                    }
                    else
                    {
                        msg_reply = "У вас нет записей за прошлый месяц. Повторно выберите интервал или напишите \"Отмена\" для возврата в главное меню:";
                        SendMessage(UserID, msg_reply);
                        return 11;
                    }
                case "6.":
                    goto case "1";
                case "прошлый месяц":
                    goto case "1";
                case "прошедший месяц":
                    goto case "1";

                case "отмена":
                    msg_reply = "Возврат в главное меню...";
                    SendMessage(UserID, msg_reply);
                    return DS_0(UserID);
                default:
                    Regex MMDDYYYY = new Regex("^[0-9]{2}[.][0-9]{2}[.][0-9]{4}");
                    if (MMDDYYYY.IsMatch(msg))
                    {
                        if (connect.IsUserRecordsExist(UserID, msg))
                        {
                            msg_reply = "У вас есть записи в этом интервале \nВозврат в главное меню...";
                            SendMessage(UserID, msg_reply);
                            return DS_0(UserID);
                        }
                        else
                        {
                            msg_reply = "У вас нет записей за "+msg+". Повторно выберите интервал или напишите \"Отмена\" для возврата в главное меню:";
                            SendMessage(UserID, msg_reply);
                            return 11;
                        }
                    }
                    else
                    {
                        msg_reply = "Нет такого временного промежутка либо он введён неверно. Повторно выберите интервал или напишите \"Отмена\" для возврата в главное меню: ";
                        SendMessage(UserID, msg_reply);
                        return 11;
                    }
            }
        }
        public Int16 DS_1_2(long UserID, string msg)
        {
            string msg_reply = "";
            msg_reply = "Возврат в главное меню...";
            SendMessage(UserID, msg_reply);
            return DS_0(UserID);
        }
    }
}