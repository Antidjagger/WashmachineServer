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
                        /// </summary>
                        if (connectToDB.IsUserExist(msg.PeerId.Value))
                        {
                            
                            SendMessage(msg.PeerId.Value, "Вы зарегистрированы!");
                            SendMessage(msg.PeerId.Value, "", "https://i.stack.imgur.com/wyrTc.png");
                            
                        }
                        else
                        {
                            /// <summary>
                            /// Если пользователь отсутствует в списках БД
                            /// Пока нет функци добавления пользователя и функций рут-пользователя (а так же, пока я не разобрался с контейнерами),
                            /// Будет просто отправка сообщения о запрете доступа и прекращение цепочки работы
                            /// </summary>

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
            var result = System.Text.Encoding.ASCII.GetString(wc.UploadFile(uploadServer.UploadUrl, way));
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
    }
}