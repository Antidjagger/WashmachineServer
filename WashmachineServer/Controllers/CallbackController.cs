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

        public CallbackController(IVkApi vkApi, IConfiguration configuration)
        {
            _vkApi = vkApi;
            _configuration = configuration;
        }

        //public CallbackController(IConfiguration configuration)//////! to delete
        //{
        //    _configuration = configuration;
        //}

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
                        IEnumerable attach = "photos58910369_1243252";
                        var albumid = 457268977;
                        var photos = _vkApi.Photo.Get(new PhotoGetParams
                        {
                            AlbumId = PhotoAlbumType.Id(albumid),
                            OwnerId = 58910369
                        });


                        // Отправим в ответ полученный от пользователя текст
                        _vkApi.Messages.SendAsync(new MessagesSendParams
                        {
                            RandomId = new DateTime().Millisecond,
                            PeerId = msg.PeerId.Value,
                            Message = "SendNudes",
                            Attachments = photos

                        }) ;
                        //_vkApi.Messages.Send(new MessagesSendParams
                        //{
                        //    RandomId = new DateTime().Millisecond,
                        //    PeerId = msg.PeerId.Value,
                        //    Message = "SendNudes",
                        //    //UserId = msg.UserId.Value,
                            
                        //});
                        break;
                    }
            }

            return Ok("ok");
        }
    }
}