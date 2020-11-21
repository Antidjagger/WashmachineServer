using System;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using VkNet.Model;
using VkNet.Utils;
using VkNet.Abstractions;
using VkNet.Model.RequestParams;
using VkNet.Model.Attachments;
using VkNet.Enums.SafetyEnums;
using VkNet.Infrastructure;
using VkNet.Categories;
using VkNet.Model.Keyboard;
using VkNet.UWP;
using WashmachineServer.MessageHandling;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

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
        private DictionaryCollections dictionaryCollections;
        //Конструктор
        public CallbackController(IVkApi vkApi, IConfiguration configuration)
        {
            _vkApi = vkApi;
            _configuration = configuration;
            dictionaryCollections = new DictionaryCollections();
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
                        break;
                    }
            }
            return Ok("ok");
        }
        private async Task<string> UploadFileFromUrl(string serverUrl, string file, string fileExtension)
        {
            
         
            var data = GetBytesFromURL(file);
            if (data == null)
            {
                ConnectToDB cdb = new ConnectToDB();
                cdb.ErrorLogWriting("Error read img data from URL: " + file,1);
                throw new Exception("Error read img data from URL: " + file);
            }

            // Создание запроса на загрузку файла на сервер
            using (var client = new HttpClient())
            {
                var requestContent = new MultipartFormDataContent();
                var content = new ByteArrayContent(data);
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                requestContent.Add(content, "file", $"file.{fileExtension}");

                var response = client.PostAsync(serverUrl, requestContent).Result;
                return System.Text.Encoding.Default.GetString(await response.Content.ReadAsByteArrayAsync());
            }
        }

        private byte[] GetBytesFromURL(string fileUrl)
        {
            using (var webClient = new System.Net.WebClient())
            {
                return webClient.DownloadData(fileUrl);
            }
        }
        //private byte[] GetBytesFromFile(string filePath)
        //{
        //    return File.ReadAllBytes(filePath);
        //}

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
        public async void SendMessage(long UserID, string msg, string urlway, string filetype)
        {
            var uploadServer = _vkApi.Photo.GetMessagesUploadServer(UserID);
            var response = await UploadFileFromUrl(uploadServer.UploadUrl, urlway, filetype);
            // Сохранить загруженный файл
            var attachment = _vkApi.Photo.SaveMessagesPhoto(response);
            _vkApi.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = UserID,
                Message = msg,
                Attachments = attachment
            });
        }

        private void AntiMat()
        {

        }

        public Int16 DS_0(long UserID)
        {
            string msg_reply = "Главное меню. Выберите варианты:\n 1. Просмотреть свои записи на стирку \n 2. Записаться на стирку\n В любой момент можно написать \"Отмена\" для возвращения в главное меню";
            SendMessage(UserID, msg_reply, "https://www.gstatic.com/webp/gallery/1.jpg", "jpg");
            return 1;
        }
        public Int16 DS_1(long UserID, string msg)
        {
            //В дальнейшем эту конструкцию надо бы оптимизировать, путём добавления алгоритма сравнения строк по близости к результату, с добавлением некоторых исключений для случаев,
            //в которых строка может относиться к разным выборам
            //(Не дай хаос кто-то увидит этот кошмар) (но уже не так страшно)
            string MsgToCase = msg.ToLower();
            string msg_reply = "";
            MsgToCase.TrimStart();
            MsgToCase.TrimEnd();

            switch (dictionaryCollections.DS_1(MsgToCase))
            {
                case 0:
                    msg_reply = "Возврат в главное меню...";
                    SendMessage(UserID, msg_reply);
                    return DS_0(UserID);
                case 1:
                    msg_reply = "Меню просмотра записей на стирку. Выберите варианты или введите конкретную дату в формате <ММ.ДД.ГГГГ>:\n1. За эту неделю\n2. За следующую неделю\n3. Сегодня\n4. За этот месяц\n5. За прошлую неделю\n6. За прошлый месяц\nВ любой момент можно написать \"Отмена\" для возвращения в главное меню";
                    SendMessage(UserID, msg_reply);
                    return 11;
                case 2:
                    msg_reply = "Меню записи на стирку.\n Выберите варианты или введите конкретную дату в формате <ММ.ДД.ГГГГ>, чтобы посмотреть свободные места и записаться:\n1. На эту неделю\n2. На следующую неделю\n3. Сегодня\n4. Через неделю\n5. Через две недели\nВ любой момент можно написать \"Отмена\" для возвращения в главное меню";
                    SendMessage(UserID, msg_reply);
                    return 12;
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
            switch (dictionaryCollections.DS_1_1(MsgToCase))
            {
                case 0:
                    msg_reply = "Возврат в главное меню...";
                    SendMessage(UserID, msg_reply);
                    return DS_0(UserID);
                case 1:
                //За сегодня
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
                case 2:
                    if (connect.IsUserRecordsExist(UserID, 1))
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
                case 3:
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
                case 4:
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
                case 5:
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
                case 6:
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
                default:
                    if (dictionaryCollections.IsInDateFormat(msg))
                    {
                        string DateMsg = dictionaryCollections.ConverToPostgreDate(msg);
                        if (DateMsg != null)
                        {
                            if (connect.IsUserRecordsExist(UserID, msg))
                            {
                                msg_reply = "У вас есть записи в этом интервале \nВозврат в главное меню...";
                                SendMessage(UserID, msg_reply);
                                return DS_0(UserID);
                            }
                            else
                            {
                                msg_reply = "У вас нет записей за " + msg + ". Повторно выберите интервал или напишите \"Отмена\" для возврата в главное меню:";
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