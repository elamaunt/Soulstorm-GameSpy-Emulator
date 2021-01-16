using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Text;
using Reality.Net.GameSpy.Servers;
using static GameSpyEmulator.TcpPortHandler;

namespace GameSpyEmulator
{
    public partial class GameSpyServer
    {
        bool _challengeResponded;
        readonly ConcurrentDictionary<string, GameServerDetails> _lastLoadedLobbies = new ConcurrentDictionary<string, GameServerDetails>();

        void OnServerRetrieveReceived(TcpPortHandler handler, TcpClientNode node, byte[] buffer, int count)
        {
            var str = buffer.ToASCII(count);
            LogTrace("RETRIEVE " + str);

            var endPoint = node.RemoteEndPoint;

            if (endPoint == null)
            {
                handler.KillClient(node);
                return;
            }

            string[] data = str.Split(new char[] { '\x00' }, StringSplitOptions.RemoveEmptyEntries);

            string validate = data[4];
            string filter = null;

            bool isAutomatch = false;

            if (validate.Length > 8)
            {
                filter = validate.Substring(8);
                validate = validate.Substring(0, 8);
            }
            else
            {
                //Log(Category, "ROOMS REQUEST - "+ data[2]);

                isAutomatch = data[2].EndsWith("am");

                if (!isAutomatch)
                {
                    SendChatRooms(handler, node, validate);
                    return;
                }
            }

            var lobbies = _emulationAdapter.GetOpenedLobbies();

            try
            {
                // var currentRating = ServerContext.ChatServer.CurrentRating;

                /*for (int i = 0; i < lobbies.Length; i++)
                {
                    var server = lobbies[i];

                    //server["score_"] = GetCurrentRating(server.MaxPlayers);
                }*/

                var fields = data[5].Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

                var unencryptedBytes = ParseHelper.PackServerList(endPoint, lobbies, fields, isAutomatch);

                _lastLoadedLobbies.Clear();

                for (int i = 0; i < lobbies.Length; i++)
                {
                    var server = lobbies[i];
                    var address = server.HostIP ?? server.LocalIP;
                    var port = ushort.Parse(server.HostPort ?? server.LocalPort);
                    var channelHash = ChatCrypt.PiStagingRoomHash(address, address, port);

                    Log($"HASHFOR {address}:{port}  {channelHash}");

                    server.RoomHash = channelHash;
                    _lastLoadedLobbies[channelHash] = server;
                }

                Log("SERVERS VALIDATE VALUE ~" + validate + "~");

                var encryptedBytes = GSEncoding.Encode(_gameGSkeyBytes, validate.ToAsciiBytes(), unencryptedBytes, unencryptedBytes.LongLength);

                Log("SERVERS bytes " + encryptedBytes.Length);

                int autoGames = 0;
                int customGames = 0;
                // TODO вынести в отдельно
                for (int i = 0; i < lobbies.Length; i++)
                {
                    var server = lobbies[i];
                    if (server.Ranked) autoGames++;
                    else customGames++;
                }

                //CoreContext.ClientServer.SendAsServerMessage(
                //    "Received game list: " + customGames + " - custom; " + autoGames +
                //    " - auto; Mod: "+ CoreContext.ThunderHawkModManager.CurrentModName);

                handler.Send(node, encryptedBytes);
            }
            finally
            {
                handler.KillClient(node);
            }
        }

        string GetLocalUserRating(string maxPlayers)
        {
            var stats = _emulationAdapter.GetLocalPlayerStats();

            switch (maxPlayers)
            {
                case "2": return stats.Score1v1.ToString();
                case "4": return stats.Score2v2.ToString();
                case "6":
                case "8": return stats.Score3v3_4v4.ToString();
                default: return "0";
            }
        }

        /// <summary>
        /// Обрабатывает состояния создания хоста (отдельно от логики автомачт-комнат чата)
        /// Актуально и для авто и для кастомок. Для камтомок нет только входа в приватный-чат лобби.
        /// </summary>
        void OnServerReportReceived(UdpPortHandler handler, byte[] receivedBytes, IPEndPoint remote)
        {
            var str = receivedBytes.ToUtf8(receivedBytes.Length);
            Log("REPORT " + str);

            // Проверка доступности сервиса
            if (receivedBytes[0] == (byte)MessageType.AVAILABLE)
            {
                LogTrace("REPORT: Send AVAILABLE");
                // Фейкаем доступность
                handler.Send(new byte[] { 0xfe, 0xfd, 0x09, 0x00, 0x00, 0x00, 0x00 }, remote);
            }
            else if (receivedBytes.Length > 5 && receivedBytes[0] == (byte)MessageType.HEARTBEAT)
            {
                // Сердебиение соединения. Если не ответим, хост закроется и игра выкенет в главный чат.
                // this is where server details come in, it starts with 0x03, it happens every 60 seconds or so

                var receivedData = Encoding.UTF8.GetString(receivedBytes.Skip(5).ToArray());
                var sections = receivedData.Split(new string[] { "\x00\x00\x00", "\x00\x00\x02" }, StringSplitOptions.None);

                if (sections.Length != 3 && !receivedData.EndsWith("\x00\x00"))
                    return; // true means we don't send back a response

                // Первый раз просто отвечаем. Игра еще не готова открыть хост для других, но прокидывает уникальный ID
                if (!_challengeResponded)
                {
                    byte[] uniqueId = new byte[4];
                    Array.Copy(receivedBytes, 1, uniqueId, 0, 4);

                    // Фейкаем успех
                    byte[] response = new byte[] { 0xfe, 0xfd, (byte)MessageType.CHALLENGE_RESPONSE, uniqueId[0], uniqueId[1], uniqueId[2], uniqueId[3], 0x41, 0x43, 0x4E, 0x2B, 0x78, 0x38, 0x44, 0x6D, 0x57, 0x49, 0x76, 0x6D, 0x64, 0x5A, 0x41, 0x51, 0x45, 0x37, 0x68, 0x41, 0x00 };

                    LogTrace("REPORT: Send challenge responce");
                    handler.Send(response, remote);
                    _challengeResponded = true;
                }
                else
                {
                    // Игра прислала нам полные данные и хост почти готов
                    string serverVars = sections[0];
                    //string playerVars = sections[1];
                    //string teamVars = sections[2];

                    var details = ParseHelper.ParseDetails(serverVars);

                    if (details.StateChanged == "2")
                    {
                        // Данные изменились и хост пора закрывать
                        LogForUser($"Clear local server");

                        LogTrace("REPORT: ClearServerDetails");
                        _emulationAdapter.OnLocalLobbyClearedByGame();
                        _challengeResponded = false;
                    }
                    else
                    {
                        // Хост активен
                        LogTrace("REPORT: UpdateCurrentLobby");

                        details["IPAddress"] = remote.Address.ToString();
                        details["QueryPort"] = remote.Port.ToString();
                        details["LastRefreshed"] = DateTime.UtcNow.ToString();
                        details["LastPing"] = DateTime.UtcNow.ToString();
                        details["country"] = "??";
                        details["hostport"] = remote.Port.ToString();
                        details["localport"] = remote.Port.ToString();

                        // Фикс бага с рейтингом хоста (актуально только для SS). При этом при прямом подключении P2P это значение тоже надо пофиксить иначе багнется
                        var serverScore = GetLocalUserRating(details.MaxPlayers);
                        details["score_"] = serverScore;

                        // Наш флаг, игре не нужен
                        details.LobbyLimited = _emulationAdapter.ShouldLimitLocalLobbyByRating();

                        details.HostId = _emulationAdapter.GetLocalCreatedLobbyId();

                        _emulationAdapter.UpdateLocalLobbyDetails(details);

                        LogForUser($"Update local server [{details.IsValid}]");

                        // Когда готовы принимать гостей в автоматче
                        if (details.IsValid && details.Ranked)
                        {
                            _emulationAdapter.OnAutomatchLobbyValidated();
                        }
                    }
                }
            }
            else if (receivedBytes.Length > 5 && receivedBytes[0] == (byte)MessageType.CHALLENGE_RESPONSE)
            {
                // Игре нужен уникальный набор байт CHALLENGE. Несколько раз прокидывается игрой
                LogTrace("REPORT: Validate challenge responce");

                byte[] uniqueId = new byte[4];
                Array.Copy(receivedBytes, 1, uniqueId, 0, 4);

                byte[] validate = Encoding.UTF8.GetBytes("Iare43/78WkOVaU1Aanv8vrXbSwA\0");
                byte[] validateDC = Encoding.UTF8.GetBytes("Egn4q1jDYyOIVczkXvlGbBxavC4A\0");

                byte[] clientResponse = new byte[validate.Length];
                Array.Copy(receivedBytes, 5, clientResponse, 0, clientResponse.Length);

                var resStr = Encoding.UTF8.GetString(clientResponse);


                // if we validate, reply back a good response
                if (clientResponse.SequenceEqual(validate) || clientResponse.SequenceEqual(validateDC))
                {
                    // Фейкаем успех
                    byte[] response = new byte[] { 0xfe, 0xfd, 0x0a, uniqueId[0], uniqueId[1], uniqueId[2], uniqueId[3] };

                    handler.Send(response, remote);

                    if (!_emulationAdapter.HasHostedLobby)
                        _emulationAdapter.CreateLobby(_name);
                }
                else
                {
                    LogTrace("REPORT: Validation failed");
                }
            }
            else if (receivedBytes.Length == 5 && receivedBytes[0] == (byte)MessageType.KEEPALIVE)
            {
                // Еще одно поддержание соединения, типа обновления пинга, чтобы убивать мертвые хосты, когда они перестают пинговать
                // this is a server ping, it starts with 0x08, it happens every 20 seconds or so
                LogTrace("REPORT: KEEPALIVE");

                byte[] uniqueId = new byte[4];
                Array.Copy(receivedBytes, 1, uniqueId, 0, 4);
                RefreshServerPing(remote);
            }
        }

        void RefreshServerPing(IPEndPoint remote)
        {
            _emulationAdapter.PingFromLocalLobby(remote);
        }
    }
}
