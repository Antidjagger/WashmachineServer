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
                                            connectToDB.SetUserDialogStage(UserID, DS_1_1(UserID, msg.Text));
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
                    msg_reply = "Меню просмотра записей на стирку. Выберите варианты или введите конкретную дату в формате <ММ.ДД.ГГГГ>:\n 1. За эту неделю \n 2. За следующую неделю\n 3. За этот месяц \n 4. За прошлую неделю \n 5. За прошлый месяц \nВ любой момент можно написать \"Отмена\" для возвращения в главное меню";
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
                    msg_reply = "Меню записи на стирку.\n Выберите варианты или введите конкретную дату в формате <ММ.ДД.ГГГГ>, чтобы посмотреть свободные места и записаться:\n 1. На эту неделю \n 2. На следующую неделю\n 3. Через неделю \n 4. Через две недели \nВ любой момент можно написать \"Отмена\" для возвращения в главное меню";
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
        public Int16 DS_1_1(long UserID, string msg)
        {
            string msg_reply = "";
            msg_reply = "Возврат в главное меню...";
            SendMessage(UserID, msg_reply);
            return DS_0(UserID);
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