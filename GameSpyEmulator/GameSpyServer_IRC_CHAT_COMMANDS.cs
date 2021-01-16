using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static GameSpyEmulator.TcpPortHandler;

namespace GameSpyEmulator
{
    public partial class GameSpyServer
    {
        byte[] _gameGSNameBytes;
        byte[] _gameGSkeyBytes;
        string _gameGSkey;

        /// <summary>
        /// Делегат для метода обработки успеха входа в лобби
        /// </summary>
        public delegate void EnterInLobbySuccessDelegate(string hostName, string[] members);

        /// <summary>
        /// Делегат для метода обработки неудачи входа в лобби
        /// </summary>
        public delegate void EnterInLobbyFailedDelegate();
        
        void HandleQuitCommand(TcpPortHandler handler, string[] values)
        {
            // Выход из чата не значит выход с сервера. Выход из чата обычно происходит при старте игры.
            // После игры игрок снова войдет в чат, но для других пользователей он всегда остается в чате
            //Restart();
        }

        /// <summary>
        /// Обратываем команду редимов комнаты чата
        /// </summary>
        void HandleModeCommand(TcpPortHandler handler, TcpClientNode node, string[] values)
        {
            var channelName = values[1];

            // Определяем, главный ли чат
            if (channelName.StartsWith("#GPG", StringComparison.OrdinalIgnoreCase))
            {
                // Просто захардкоженный ответ под Soulstorm, типа успех
                SendToClientChat(node, $":s 324 {_name} {channelName} +\r\n");
            }
            else
            {
                // В авот с автоматчем поинтереснее
                if (channelName.StartsWith("#GSP", StringComparison.OrdinalIgnoreCase))
                {
                    // Уникальный хэш комнаты
                    var roomHash = channelName.Split('!')[2];

                    // Извлекаем хоста по хэшу. Если не удастся, значит это локальный хост.
                    if (_lastLoadedLobbies.TryGetValue(roomHash, out GameServerDetails details))
                    {
                        // Отправляем ограничение на количество юзеров в комнате
                        var maxPLayers = _emulationAdapter.GetCurrentLobbyMaxPlayers();

                        if (maxPLayers == 2 || maxPLayers == 4 || maxPLayers == 6 || maxPLayers == 8)
                            SendToClientChat(node, $":s 324 {_name} {channelName} +l {maxPLayers}\r\n");
                        else
                            SendToClientChat(node, $":s 324 {_name} {channelName} +\r\n");
                    }
                    else
                    {
                        // На всякий случай проверяем на соответствие локальный хэш
                        if (roomHash == _localServerHash)
                        {
                            // Обрабатываем установку ограничения на количество юзеров в комнате
                            if (values.Length < 4)
                            {
                                // Это был запрос. Отвечаем
                                var max = _emulationAdapter.GetCurrentLobbyMaxPlayers();

                                if (max > 0 && max < 9)
                                    SendToClientChat(node, $":s 324 {_name} {channelName} +l {max}\r\n");
                                else
                                    SendToClientChat(node, $":s 324 {_name} {channelName} +\r\n");
                            }
                            else
                            {
                                // Это была установка. Задаем ограничение и отправляем успех
                                var maxPlayers = values[3];

                                if (int.TryParse(maxPlayers, out int value))
                                    _emulationAdapter.SetLocalLobbyMaxPlayers(value);

                                SendToClientChat(node, $":{_user} MODE #GSP!whamdowfr!{_enteredLobbyHash} +l {maxPlayers}\r\n");
                            }

                            // CHATLINE MODE #GSP!whamdowfr!Ml39ll1K9M +l 2
                            // CHATLINE MODE #GSP!whamdowfr!Ml39ll1K9M -i-p-s-m-n-t+l+e 2
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Обрабатывает вход в чат.
        /// </summary>
        void HandleJoinCommand(TcpPortHandler handler, TcpClientNode node, string line, string[] values)
        {
            var channelName = values[1];

            // Определяем, главный ли чат
            if (channelName.StartsWith("#GPG", StringComparison.OrdinalIgnoreCase))
            {
                var users = _emulationAdapter.GetUsersInMainChat();

                var builder = new StringBuilder();

                builder.Append($":{_user} JOIN {channelName}\r\n");
                // SendToClientChat(node, $":{_user} JOIN {channelName}\r\n");
                builder.Append($":s 331 {channelName} :No topic is set\r\n");
                // SendToClientChat(node, $":s 331 {channelName} :No topic is set\r\n");

                _inChat = true;

                var playersList = new StringBuilder();

                for (int i = 0; i < users.Length; i++)
                {
                    var user = users[i];

                    playersList.Append(user + " ");
                }

                // Посылаем список юзеров в чате
                builder.Append($":s 353 {_name} = {channelName} :{playersList}\r\n");
                //SendToClientChat(node, $":s 353 {_name} = {channelName} :{playersList}\r\n");
                builder.Append($":s 366 {_name} {channelName} :End of NAMES list\r\n");
                //SendToClientChat(node, $":s 366 {_name} {channelName} :End of NAMES list\r\n");

                SendToClientChat(builder.ToString());
            }
            else
            {
                if (channelName.StartsWith("#GSP", StringComparison.OrdinalIgnoreCase))
                {
                    // Вход в комнату для автоматча
                    // Извлекаем уникальный хэш, чтобы определить сопоставить со списком выданных ранее хостов.
                    var roomHash = channelName.Split('!')[2];

                    LogForUser($"Try to get lobby [{roomHash}]");

                    // Берем хост, если хоста нет. Значит это попытка войти в комнате локального хоста.
                    if (_lastLoadedLobbies.TryGetValue(roomHash, out GameServerDetails details))
                    {
                        // Входим в чужой хост
                        LogForUser($"Try to enter lobby [{roomHash}]");

                        var hostId = details.HostId;

                        // Попытка войти в хост. Сервер решает успех
                        _emulationAdapter.TryEnterInLobby(hostId, _name,
                            new EnterInLobbySuccessDelegate((hostName, members) =>
                            {
                                _enteredLobbyHash = details.RoomHash;
                                Log($"Entered to lobby [{roomHash}]");

                                var playersList = new StringBuilder();

                                // Вошедший должен быть в этом списке
                                for (int i = 0; i < members.Length; i++)
                                {
                                    var member = members[i];

                                    Log($"Player {i} [{GetNickHash(member)}]");
                                    playersList.Append(member + " ");
                                }

                                // Теперь все должны узнать, что юзер вошел. Делается через Broadcast
                                _emulationAdapter.SendLobbyBroadcast($"JOIN {_user}");

                                // Себе отправляем результат сразу.
                                SendToClientChat(node, $":{_user} JOIN {channelName}\r\n");

                                var topic = hostName;

                                // Присылаем список пользователей в комнате
                                SendToClientChat(node, $":s 331 {channelName} :{topic}\r\n");
                                SendToClientChat(node, $":s 353 {_name} = {channelName} :@{playersList}\r\n");
                                SendToClientChat(node, $":s 366 {_name} {channelName} :End of NAMES list\r\n");
                            }),
                            new EnterInLobbyFailedDelegate(() =>
                            {
                                // Не удалось войти в чате, причина не важна. После этого игра попробует создать хост самостоятельно
                                SendToClientChat(node, $":{_user} {channelName} :Bad Channel Mask\r\n");
                            }));
                    }
                    else
                    {
                        // Мы в своем хосте. Выполним требования IRC
                        LogForUser($"This lobby is local [{roomHash}]");
                        LogForUser($"Player 0 [{GetNickHash(_name)}]");

                        _localServerHash = roomHash;
                        _enteredLobbyHash = roomHash;

                        var builder = new StringBuilder();

                        builder.Append($":{_user} JOIN {channelName}\r\n");
                        builder.Append($":s 331 {channelName} :No topic is set\r\n");

                        // Мы только одни будем в комнате в этот момент.
                        builder.Append($":s 353 {_name} = {channelName} :@{_name}\r\n");
                        builder.Append($":s 366 {_name} {channelName} :End of NAMES list\r\n");

                        SendToClientChat(node, builder.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Обрабатывает проверку CD ключа игры. Мы просто всегда возвращаем успех.
        /// Если ключа у юзера нет в реестре, то до этой команды не дойдет. Помогает разблокировка рас.
        /// </summary>
        void HandleCdkeyCommand(TcpPortHandler handler, TcpClientNode node, string[] values)
        {
            LogForUser($"Cdkey check");

            SendToClientChat(node, $":s 706 {_name}: 1 :\"Authenticated\"\r\n");
        }

        /// <summary>
        /// Обратаываем установку полного имени пользователя. 
        /// </summary>
        void HandleUserCommand(TcpPortHandler handler, TcpClientNode node, string[] values)
        {
            // Просто сохраняем себе для дальнейшего использования
            _user = $@"{_name}!{values[1]}@{node.RemoteEndPoint?.Address}";
            _shortUser = values[1];
        }

        /// <summary>
        /// Обратывает команду установки ника. Наследие IRC. Просто отвечаем по стандарту.
        /// </summary>
        void HandleNickCommand(TcpPortHandler handler, TcpClientNode node, string[] values)
        {
            var users = _emulationAdapter.ActivePlayersCount;

            SendToClientChat(node, $":s 001 {_name} :Welcome to the Matrix {_name}\r\n");
           //SendToClientChat(node, $":s 002 {_name} :Your host is xs0, running version 1.0\r\n");
           // SendToClientChat(node, $":s 003 {_name} :This server was created Fri Oct 19 1979 at 21:50:00 PDT\r\n");
            //SendToClientChat(node, $":s 004 {_name} s 1.0 iq biklmnopqustvhe\r\n");
           // SendToClientChat(node, $":s 375 {_name} :- (M) Message of the day - \r\n");
           // SendToClientChat(node, $":s 372 {_name} :- Welcome to GameSpy\r\n");
            //SendToClientChat(node, $":s 251 :There are {users} users and 0 services on 1 servers\r\n");
           // SendToClientChat(node, $":s 252 0 :operator(s)online\r\n");
           // SendToClientChat(node, $":s 253 1 :unknown connection(s)\r\n");
          //  SendToClientChat(node, $":s 254 1 :channels formed\r\n");
          //  SendToClientChat(node, $":s 255 :I have {users} clients and 1 servers\r\n");

            SendToClientChat(node, $":{_user} NICK {_name}\r\n");
        }

        /// <summary>
        /// Обрабатывает команду перехода в шифрованный чат
        /// </summary>
        unsafe void HandleCryptCommand(TcpPortHandler handler, TcpClientNode node, string[] values)
        {
            _chatEncoded = true;
            _gameGSkeyBytes = null;

            // В зависимости от игры. Байты шифрования отличаются. Таблица есть в инете
            // https://gamerecon.net/support/topic/gamespy-supported-games-list/
            // https://github.com/luisj135/nintendo_dwc_emulator/blob/master/gslist.cfg

            // Dawn of War 
            if (values.Contains("dow"))
            {
                _gameGSkey = "dow";
                _gameGSNameBytes = "dow".ToAsciiBytes();
                _gameGSkeyBytes = "VLxgwe".ToAsciiBytes();
            }

            // Dawn of War (более похоже на правду)
            if (values.Contains("whammer40000"))
            {
                _gameGSkey = "whammer40000";
                _gameGSNameBytes = "whammer40000".ToAsciiBytes();
                _gameGSkeyBytes = "uJ8d3N".ToAsciiBytes();
            }

            // Winter Assault
            // ключа почему-то нет в таблице. Возьмем ключ 1 дова.
            // TODO: Надо потестить WA
            if (values.Contains("dowwad"))
            {
                _gameGSkey = "dowwad";
                _gameGSNameBytes = "dowwad".ToAsciiBytes();
                _gameGSkeyBytes = "uJ8d3N".ToAsciiBytes();
            }

            // Dark Crusade
            if (values.Contains("whammer40kdc"))
            {
                _gameGSkey = "whammer40kdc";
                _gameGSNameBytes = "whammer40kdc".ToAsciiBytes();
                _gameGSkeyBytes = "Ue9v3H".ToAsciiBytes();
            }

            // Soulstorm
            if (values.Contains("whamdowfr"))
            {
                _gameGSkey = "whamdowfr";
                _gameGSNameBytes = "whamdowfr".ToAsciiBytes();
                _gameGSkeyBytes = "pXL838".ToAsciiBytes();
            }

            if (_gameGSkeyBytes == null)
            {
                Restart();
                return;
            }

            // Ключ шифрования. Для простоты просто нули. Не тестил, что будет, если не нули.
            var chall = "0000000000000000".ToAsciiBytes();

            var clientKey = new ChatCrypt.GDCryptKey();
            var serverKey = new ChatCrypt.GDCryptKey();

            // Инициализируем структуры-ключи для выполнения алгоритма спая
            fixed (byte* challPtr = chall)
            {
                fixed (byte* gamekeyPtr = _gameGSkeyBytes)
                {
                    ChatCrypt.GSCryptKeyInit(clientKey, challPtr, gamekeyPtr, _gameGSkeyBytes.Length);
                    ChatCrypt.GSCryptKeyInit(serverKey, challPtr, gamekeyPtr, _gameGSkeyBytes.Length);
                }
            }

            // Сохраняем структуры
            _chatClientKey = clientKey;
            _chatServerKey = serverKey;

            // Отправляем идентичные ключи для сервера и клиента. Шифрование в итоге будет полностью совпадать. 
            // С этого момента чат шифрованный
            handler.SendAskii(node, ":s 705 * 0000000000000000 0000000000000000\r\n");
        }

        /// <summary>
        /// Сообщаем игре ее IP. Как правило, этот ответ ни на что не влияет. По крайней мере, я не обнаружил какой-либо зависимости.
        /// Особенность библиотеки GameSpy.
        /// </summary>
        void HandleUsripCommand(TcpPortHandler handler, TcpClientNode node, string[] values)
        {
            SendToClientChat(node, $":s 302  :=+@{node.RemoteEndPoint?.Address}\r\n");
        }

        /// <summary>
        /// Обрабатывает команду логина в чате. В этот момент пользователь уже авторизован, просто сообщаем ему его ID профиля.
        /// </summary>
        void HandleLoginCommand(TcpPortHandler handler, TcpClientNode node, string[] values)
        {
            var nick = values[2];
            var id = _emulationAdapter.GetUserInGameProfileId(nick);

            SendToClientChat(node, $":s 707 {nick} 12345678 {id}\r\n");
            SendToClientChat(node, $":s 687ru: Your languages have been set\r\n");
        }

        /// <summary>
        /// На Ping в чате отвечаем Pong. Поддерживает соединение с чат сервером.
        /// </summary>
        void HandlePingCommand(TcpPortHandler handler, TcpClientNode node, string[] values)
        {
            SendToClientChat(node, $":s PONG :s\r\n");
        }

        /// <summary>
        /// Обратаываем UTM сообщение а чате. Уникальная команда GameSpy. Передаем ее всем участникам чата через Broadcast
        /// </summary>
        void HandleUtmCommand(TcpPortHandler handler, string line)
        {
            _emulationAdapter.SendLobbyBroadcast(line);
        }

        /// <summary>
        /// Обратываем выход пользователя из комнаты чата
        /// </summary>
        void HandlePartCommand(TcpPortHandler handler, string[] values)
        {
            //CHATLINE PART #GSP!whamdowfr!Ml39ll1K9M :
            var channelName = values[1];

            // Определяем, глобальный чат или комната автоматча
            if (channelName == "#GPG!1")
            {
                // Main chat - ignore
            }
            else
            {
                _enteredLobbyHash = null;
                _localServerHash = null;

                _emulationAdapter.LeaveFromCurrentLobby();
            }

            if (!_emulationAdapter.HasLocalUserActiveInGameProfile)
                return;

            LogForUser($"Part sended {channelName}");

            SendToClientChat($":{_user} PART {channelName} :Leaving\r\n");
        }

        /// <summary>
        /// Обратывает установку заголовка комнаты.
        /// Для лобби это всегда имя хоста.
        /// </summary>
        void HandleTopicCommand(TcpPortHandler handler, TcpClientNode node, string[] values)
        {
            // :Bambochuk2!Xu4FpqOa9X|4@192.168.159.128 TOPIC #GSP!whamdowfr!76561198408785287 :Bambochuk2

            _emulationAdapter.SetLobbyTopic(values[2]);
            SendToClientChat(node, $":{_user} TOPIC #GSP!{_gameGSkey}!{_enteredLobbyHash} :{values[2]}\r\n");

            //TOPIC #GSP!whamdowfr!Ml39ll1K9M :elamaunt
            /*var channelName = values[1];

            if (channelName.StartsWith("#GSP", StringComparison.OrdinalIgnoreCase))
            {
                var roomHash = channelName.Split('!')[2];

                if (roomHash == _localServerHash)
                {
                    SteamLobbyManager.SetLobbyTopic(values[2]);
                }
            }*/
        }

        /// <summary>
        /// Обрабатывает сохранение пары ключ-значение глобально в указанной комнате чата.
        /// Все другие пользователи должны после этого получить сообщение Broadcast.
        /// </summary>
        void HandleSetckeyCommand(TcpPortHandler handler, TcpClientNode node, string line, string[] values)
        {
            var channelName = values[1];

            if (channelName == "#GPG!1")
            {
                var keyValues = values[3];

                var pairs = keyValues.Split(':', '\\');

                if (pairs[1] == "username")
                {
                    SendToClientChat(node, $":s 702 #GPG!1 #GPG!1 {values[2]} BCAST :\\{pairs[1]}\\{pairs[2]}\r\n");
                    return;
                }

                SendToClientChat(node, $":s 702 #GPG!1 #GPG!1 {values[2]} BCAST :\\{pairs[1]}\\{pairs[2]}\r\n");

                var dictionary = new Dictionary<string, string>();

                for (int i = 1; i < pairs.Length; i += 2)
                    dictionary[pairs[i]] = pairs[i + 1];

                    _emulationAdapter.SetGlobalKeyValues(dictionary);

                /* for (int i = 1; i < pairs.Length; i += 2)
                     SendToClientChat($":s 702 #GPG!1 #GPG!1 {values[2]} BCAST :\\{pairs[i]}\\{pairs[i + 1]}\r\n");*/
            }
            else
            {
                if (channelName.StartsWith("#GSP", StringComparison.OrdinalIgnoreCase))
                {
                    //var roomHash = channelName.Split('!')[2];

                    var keyValues = values[3];

                    var pairs = keyValues.Split(':', '\\');

                    // Skip first empty entry
                    for (int i = 1; i < pairs.Length; i += 2)
                        _emulationAdapter.SetLobbyKeyValue(pairs[i], pairs[i + 1]);

                    _emulationAdapter.SendLobbyBroadcast(line);
                    HandleRemoteSetckeyCommand(values);
                }
            }
        }

        /// <summary>
        /// Обрабатываем приватное сообщение чата. Либо личное, либо в чат канал
        /// </summary>
        void HandlePrivmsgCommand(TcpPortHandler handler, string[] values)
        {
            // PRIVMSG #GPG!1 :dfg
            var channelName = values[1];

            // Определяем, отправлено ли сообщение в глобальный чат
            if (channelName == "#GPG!1")
            {
                _emulationAdapter.SendChatMessage(values[2]);
            }
            else
            {
                if (channelName.StartsWith("#GSP", StringComparison.OrdinalIgnoreCase))
                {
                    //var roomHash = channelName.Split('!')[2];

                    // Костыль для работы личных сообщений в игре. Удаляем префикс 
                    if (values[1].EndsWith("-thq"))
                        values[1] = values[1].Substring(0, values[1].Length - 4);

                    _emulationAdapter.SendLobbyBroadcast(string.Join(" ", values));
                }
            }
        }

        /// <summary>
        /// Обрабатывает команду IRC на получение значения юзера в комнате чата по массиву ключей
        /// </summary>
        void HandleGetckeyCommand(TcpPortHandler handler, TcpClientNode node, string[] values)
        {
            var channelName = values[1];

            //GETCKEY #GPG!1 * 000 0 :\\username\\b_flags
            var id = values[3];
            var keysString = values[5];

            // Извлекаем список ключей
            var keys = keysString.Split(':', '\\');
            var builder = new StringBuilder();

            if (channelName.StartsWith("#GSP", StringComparison.OrdinalIgnoreCase))
            {
                //var roomHash = channelName.Split('!')[2];

                // Если мы по какой-то причине не в лобби - эмулируем ответ, будто мы в лобби. Защита от багов
                if (!_emulationAdapter.IsInLobbyNow)
                {
                    for (int k = 0; k < keys.Length; k++)
                    {
                        var key = keys[k];

                        if (string.IsNullOrWhiteSpace(key))
                            continue;

                        string value;

                        if (key == "username")
                            value = _shortUser;
                        else
                            value = _emulationAdapter.GetLobbyKeyValue(key);

                        builder.Append($@"\{value ?? string.Empty}");
                    }

                    SendToClientChat(node, $":s 702 {_name} {channelName} {id} :{builder}\r\n");
                }
                else
                {
                    // Если мы в лобби - отправляем нормальные данные по указанным ключам
                    var members = _emulationAdapter.GetLobbyMembers();

                    for (int i = 0; i < members.Length; i++)
                    {
                        builder.Clear();

                        var name = members[i];

                        for (int k = 0; k < keys.Length; k++)
                        {
                            var key = keys[k];

                            if (string.IsNullOrWhiteSpace(key))
                                continue;

                            var value = _emulationAdapter.GetLobbyMemberData(name, key);

                            builder.Append(@"\" + value);
                        }

                        SendToClientChat(node, $":s 702 {_name} {channelName} {name} {id} :{builder}\r\n");
                    }
                }

                SendToClientChat(node, $":s 703 {_name} {channelName} {id} :End of GETCKEY\r\n");
            }
            else
            {
                // Для главного чата эмулируем значения, если их по какой-то причине нет, 
                // или даем нормальные данные, полученные от других клиентов
                if (channelName.StartsWith("#GPG", StringComparison.OrdinalIgnoreCase))
                {
                    var users = _emulationAdapter.GetUsersInMainChat();

                    for (int i = 0; i < users.Length; i++)
                    {
                        var user = users[i];

                        builder.Clear();

                        for (int k = 0; k < keys.Length; k++)
                        {
                            var key = keys[k];

                            if (string.IsNullOrWhiteSpace(key))
                                continue;

                            string value = string.Empty;

                            if (key == "username")
                            {
                                var localName = _emulationAdapter.LocalUserName;

                                if (string.Equals(localName, user, StringComparison.Ordinal))
                                    value = _shortUser;
                                else
                                    value = $"X{GetEncodedIp(user)}X|{_emulationAdapter.GetUserInGameProfileId(user)}";
                            }

                            if (key == "b_stats")
                            {
                                value = _emulationAdapter.GetUserGlobalKeyValue(user, key);

                                if (value == null)
                                {
                                    var stats = _emulationAdapter.GetUserStatsInfo(user);
                                    value = $"{_emulationAdapter.GetUserInGameProfileId(user)}|{stats.Score1v1}|{stats.StarsCount}|";
                                }
                            }

                            if (key == "b_flags")
                            {
                                value = _emulationAdapter.GetUserGlobalKeyValue(user, key);

                                if (value == null)
                                    value = string.Empty;
                            }

                            builder.Append(@"\" + value);
                        }

                        SendToClientChat(node, $":s 702 {_name} {channelName} {user} {id} :{builder}\r\n");
                    }

                    SendToClientChat(node, $":s 703 {_name} {channelName} {id} :End of GETCKEY\r\n");
                }
            }
        }
    }
}
