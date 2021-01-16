using System;
using System.IO;
using System.Text;
using Http;
using static GameSpyEmulator.TcpPortHandler;

namespace GameSpyEmulator
{
    public partial class GameSpyServer
    {
        /// <summary>
        /// Обработка HTTP запроса от игры на порт 80. 
        /// Обрабатывает новостное сообщение, наличие патча, запрос страницы статистики, настройки автоматча и список имен комнат чата.
        /// </summary>
        void OnHttpReceived(TcpPortHandler handler, TcpClientNode node, byte[] buffer, int count)
        {
            try
            {
                var str = buffer.ToUtf8(count);

                LogTrace("HTTP CLIENT HASH " + node.GetHashCode());
                LogTrace("HTTP " + str);

                HttpRequest request;

                using (var ms = new MemoryStream(buffer, 0, count, false, true))
                    request = HttpHelper.GetRequest(ms);

                using (var ms = new MemoryStream())
                {

                    // Запрос страницы статистики по кнопке из игры
                    if (request.Url.StartsWith("/SS_StatsPage", StringComparison.OrdinalIgnoreCase))
                    {
                        HttpHelper.WriteResponse(ms, HttpResponceBuilder.DowstatsRedirect());
                        goto END;
                    }

                    // Запрос текста новостей
                    if (request.Url.EndsWith("news.txt", StringComparison.OrdinalIgnoreCase))
                    {
                        LogForUser($"News requested");

                        // Фикс для рускоязычных
                        if (request.Url.EndsWith("Russiandow_news.txt", StringComparison.OrdinalIgnoreCase))
                            HttpHelper.WriteResponse(ms, HttpResponceBuilder.Text(GameSpyHttpDataConstants.RusNews, Encoding.Unicode));
                        else
                            HttpHelper.WriteResponse(ms, HttpResponceBuilder.Text(GameSpyHttpDataConstants.EnNews, Encoding.Unicode));
                        goto END;
                    }

                    // Отправка сообщения дня. Вроде нигде не отображается
                    if (request.Url.StartsWith("/motd/motd", StringComparison.OrdinalIgnoreCase))
                    {
                        HttpHelper.WriteResponse(ms, HttpResponceBuilder.Text(GameSpyHttpDataConstants.RusNews, Encoding.Unicode));
                        goto END;
                    }

                    // Проверка на существование патча. Можно прокидывать свои патчи для игры
                    if (request.Url.StartsWith("/motd/vercheck", StringComparison.OrdinalIgnoreCase))
                    {
                        LogForUser($"Vercheck requested");

                        // Пример отправки ссылки на скачивания патча
                        //HttpHelper.WriteResponse(ms, HttpResponceBuilder.Text(@"\newver\1\newvername\1.4\dlurl\http://127.0.0.1/NewPatchHere.exe"));

                        // Отправка инфы о том, что патча сейчас нет
                        HttpHelper.WriteResponse(ms, HttpResponceBuilder.Text(@"\newver\0", Encoding.UTF8));
                        goto END;
                    }

                    // Запрос списка комнат с именами для отображения в интерфейсе
                    if (request.Url.EndsWith("LobbyRooms.lua", StringComparison.OrdinalIgnoreCase))
                    {
                        LogForUser($"LobbyRooms requested");
                        HttpHelper.WriteResponse(ms, HttpResponceBuilder.Text(GameSpyHttpDataConstants.RoomPairs, Encoding.ASCII));
                        goto END;
                    }

                    // Запрос дефолных настроек автоматча
                    if (request.Url.EndsWith("AutomatchDefaultsSS.lua", StringComparison.OrdinalIgnoreCase) || request.Url.EndsWith("AutomatchDefaultsDXP2Fixed.lua", StringComparison.OrdinalIgnoreCase))
                    {
                        LogForUser($"AutomatchDefaults requested");
                        //HttpHelper.WriteResponse(ms, HttpResponceBuilder.TextFileBytes(CoreContext.MasterServer.AutomatchDefaultsBytes));
                        HttpHelper.WriteResponse(ms, HttpResponceBuilder.Text(GameSpyHttpDataConstants.AutomatchDefaults, Encoding.ASCII));
                        goto END;
                    }

                    /*if (request.Url.EndsWith("homepage.php.htm", StringComparison.OrdinalIgnoreCase))
                    {
                        if (StatsResponce == null || (DateTime.Now - _lastStatsUpdate).TotalMinutes > 5)
                            StatsResponce = BuildTop10StatsResponce();

                        HttpHelper.WriteResponse(ms, StatsResponce);
                        goto END;
                    }*/

                    // Если дошли сюда - отправляет NotFound
                    HttpHelper.WriteResponse(ms, HttpResponceBuilder.NotFound());

END:
                    LogTrace("HTTP WANT TO SEND " + node.GetHashCode() + " " + ms.Length);
                    handler.Send(node, ms.ToArray());
                    handler.KillClient(node);
                }
            }
            catch (InvalidDataException ex)
            {
                //Log(ex);
            }
        }
    }
}
