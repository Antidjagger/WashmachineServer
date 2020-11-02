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
        //Список пользователей
        private List<long> UserList;
        //Конструктор
        public CallbackController(IVkApi vkApi, IConfiguration configuration)
        {
            _vkApi = vkApi;
            _configuration = configuration;
            UserList = new List<long>();
            /// <summary>
            /// Загрузка списка UserId пользователей из гугл-таблицы
            /// Пока нет подключения к таблице, идёт искусственная "ручная подгрузка". К изменению!
            /// Нужно не забыть сделать отдельный класс для хранения и загрузки списка пользователей (Нужно будет в т.ч для адекватного обновления списка).
            /// </summary>
            UserList.Add(550105754);
            
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
                        if (UserList.Contains(msg.PeerId.Value))
                        {
                            /// <summary>
                            /// Начинаем работу с пользователем
                            /// Пока нет подключения к гугл-таблице - фиктивная отправка сообщения, что он есть в списке
                            /// Нужно добавить также в класс User возможность сохранить текущее состояние диалога, 
                            /// если только сторона вконтакте не присылает это состояние при каждом запросе
                            /// </summary>
                            SendMessage(msg.PeerId.Value, "Вы зарегистрированы!");
                            //_vkApi.Messages.SendAsync(new MessagesSendParams
                            //{
                            //    RandomId = new DateTime().Millisecond,
                            //    PeerId = msg.PeerId.Value,
                            //    Message = "зарегистрированы"
                            //});
                        }
                        else
                        {
                            /// <summary>
                            /// Если пользователь отсутствует в списках таблицы
                            /// Пока нет функци добавления пользователя и функций рут-пользователя (а так же, пока я не разобрался с контейнерами),
                            /// Будет просто отправка сообщения о запрете доступа и прекращение цепочки работы
                            /// </summary>

                            //_vkApi.Messages.SendAsync(new MessagesSendParams
                            //{
                            //    RandomId = new DateTime().Millisecond,
                            //    PeerId = msg.PeerId.Value,
                            //    Message = "незарегистрированы"
                            //});

                            SendMessage(msg.PeerId.Value, "Вы незарегистрированы!");
                        }






                        //IEnumerable attach = "photos58910369_1243252";
                        //var albumid = 00;




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

    }
}