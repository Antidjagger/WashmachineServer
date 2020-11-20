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
                                    SendMessage(msg.PeerId.Value, "Вы зарегистрированы!");
                                    long UserID = msg.PeerId.Value;
                                    Int16 UserDS = connectToDB.GetUserDialogStage(UserID);
                                    switch (UserDS)
                                    {
                                        //Главное меню
                                        case 0:
                                            DS_0(UserID);
                                            break;
                                        //Выбор вариантов в главном меню
                                        case 1:
                                            
                                            break;
                                        //Просмотреть свои записи: выбор вариантов
                                        case 11:

                                            break;
                                        //Записаться на стирку: выбор вариантов
                                        case 12:

                                            break;
                                        //Если вылезло за пределы
                                        default:

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

        public void DS_0(long UserID)
        {
            string msg_reply = "Главное меню. Выберите варианты: %0A 1. Просмотреть свои записи на стирку %0A 2. Записаться на стирку";
            SendMessage(UserID, msg_reply);
        }
        public void DS_1(long UserID, string msg)
        {

        }
    }
}