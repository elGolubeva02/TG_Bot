using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TRSHELP
{
    class Program
    {
        private static ITelegramBotClient _botClient;
        private static ReceiverOptions _receiverOptions;
        private static Dictionary<long, string> userStates = new Dictionary<long, string>();

        static async Task Main(string[] args)
        {
            _botClient = new TelegramBotClient("6760722759:AAG44M97Z9ifv_u82UmaJoipbxO3IFrYbfk");
            _receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[]
                {
                    UpdateType.Message,
                    UpdateType.CallbackQuery
                },
            };

            // Удаление webhook
            await _botClient.DeleteWebhookAsync();

            using (var cts = new CancellationTokenSource())
            {
                _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);
                var me = await _botClient.GetMeAsync();
                Console.WriteLine($"{me.FirstName} запущен!");
                await Task.Delay(-1);
            }
        }

        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                long chatId = 0;

                if (update.Type == UpdateType.Message && update.Message.Type == MessageType.Text)
                {
                    var message = update.Message;
                    chatId = message.Chat.Id;
                    if (!userStates.ContainsKey(chatId))
                    {
                        userStates[chatId] = "start";
                    }

                    await ShowModules(botClient, chatId);
                }
                else if (update.Type == UpdateType.CallbackQuery)
                {
                    var callbackQuery = update.CallbackQuery;
                    chatId = callbackQuery.Message?.Chat?.Id ?? callbackQuery.From?.Id ?? 0; // Получаем ChatId

                    if (chatId != 0)
                    {
                        if (!userStates.ContainsKey(chatId))
                        {
                            userStates[chatId] = "start";
                        }

                        var data = callbackQuery.Data;

                        if (!string.IsNullOrEmpty(data))
                        {
                            var dataArray = data.Split('_');
                            if (dataArray.Length >= 2)
                            {
                                if (dataArray[0] == "module")
                                {
                                    var moduleId = dataArray[1];
                                    userStates[chatId] = $"module_{moduleId}";
                                    await ShowTypes(botClient, chatId, moduleId);
                                }
                                else if (dataArray[0] == "type" && dataArray.Length >= 2) 
                                {
                                    var typeId = dataArray[1];
                                    userStates[chatId] = $"type_{typeId}";
                                    await ShowTasks(botClient, chatId, typeId);
                                }
                                else if (dataArray[0] == "task") 
                                {
                                    var taskId = dataArray[1];
                                    userStates[chatId] = $"task_{taskId}";
                                    await ShowTaskDetails(botClient, chatId, taskId);
                                }
                            }
                        }

                        if (data == "back_to_modules")
                        {
                            await ShowModules(botClient, chatId);
                        }
                        else if (data == "back_to_types")
                        {
                            var moduleId = userStates[chatId].Split('_')[1];
                            await ShowTypes(botClient, chatId, moduleId);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"ChatId {chatId} is not valid.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task ShowModules(ITelegramBotClient botClient, long chatId)
        {
            string connectionString = "Data Source=ngknn.ru;Initial Catalog=tables;User ID=22V;Password=123;Database=22v_Ivanov";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string sqlQuery = "SELECT ID_Model, Title_Model FROM Model";
                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        var inlineKeyboard = new List<InlineKeyboardButton[]>();
                        while (reader.Read())
                        {
                            string moduleId = reader["ID_Model"].ToString();
                            string moduleTitle = reader["Title_Model"].ToString();
                            inlineKeyboard.Add(new[] { InlineKeyboardButton.WithCallbackData(moduleTitle, $"module_{moduleId}") });
                        }
                        await botClient.SendTextMessageAsync(chatId, "Выберите модуль:", replyMarkup: new InlineKeyboardMarkup(inlineKeyboard));
                    }
                }
            }
        }

        private static async Task ShowTypes(ITelegramBotClient botClient, long chatId, string moduleId)
        {
            string connectionString = "Data Source=ngknn.ru;Initial Catalog=tables;User ID=22V;Password=123;Database=22v_Ivanov";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string sqlQuery = "SELECT ID_Type, Title_Type FROM Type WHERE ID_Model = @ID_Model";
                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.AddWithValue("@ID_Model", moduleId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        var inlineKeyboard = new List<InlineKeyboardButton[]>();
                        while (reader.Read())
                        {
                            string typeId = reader["ID_Type"].ToString();
                            string typeTitle = reader["Title_Type"].ToString();
                            inlineKeyboard.Add(new[] { InlineKeyboardButton.WithCallbackData(typeTitle, $"type_{typeId}") });
                        }
                        inlineKeyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("Назад", "back_to_modules") });
                        await botClient.SendTextMessageAsync(chatId, "Выберите тип:", replyMarkup: new InlineKeyboardMarkup(inlineKeyboard));
                    }
                }
            }
        }

        private static async Task ShowTasks(ITelegramBotClient botClient, long chatId, string typeId)
        {
            string connectionString = "Data Source=ngknn.ru;Initial Catalog=tables;User ID=22V;Password=123;Database=22v_Ivanov";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string sqlQuery = "SELECT ID_Task, Title_Task FROM Task WHERE ID_Type = @ID_Type";
                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.AddWithValue("@ID_Type", typeId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        var inlineKeyboard = new List<InlineKeyboardButton[]>();
                        while (reader.Read())
                        {
                            string taskId = reader["ID_Task"].ToString();
                            string taskTitle = reader["Title_Task"].ToString();
                            Console.WriteLine($"Task ID: {taskId}, Title: {taskTitle}"); // Добавим отладочный вывод
                            inlineKeyboard.Add(new[] { InlineKeyboardButton.WithCallbackData(taskTitle, $"task_{taskId}") });
                        }
                        inlineKeyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("Назад", "back_to_types") });
                        await botClient.SendTextMessageAsync(chatId, "Выберите задачу:", replyMarkup: new InlineKeyboardMarkup(inlineKeyboard));
                    }
                }
            }
        }

        private static async Task ShowTaskDetails(ITelegramBotClient botClient, long chatId, string taskId)
        {
            string connectionString = "Data Source=ngknn.ru;Initial Catalog=tables;User ID=22V;Password=123;Database=22v_Ivanov";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string sqlQuery = $"SELECT Title_Task, Info FROM Task WHERE ID_Task = {taskId}";
                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    //command.Parameters.AddWithValue("@ID_Task", taskId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string taskInfo = reader["Info"].ToString();
                            await botClient.SendTextMessageAsync(chatId, taskInfo);
                        }
                    }
                }
            }
        }



        private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            var ErrorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}
