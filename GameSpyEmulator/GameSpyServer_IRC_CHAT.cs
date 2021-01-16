using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Reality.Net.GameSpy.Servers;
using static GameSpyEmulator.TcpPortHandler;

namespace GameSpyEmulator
{
    public partial class GameSpyServer
    {
        /// <summary>
        /// Все IRC сообщения надо разбивать этими символами
        /// </summary>
        readonly char[] _chatSplitChars = new[] { '\r', '\n' };

        /// <summary>
        /// Вспомогательный объект для дешифрования байт, полученных от игры
        /// </summary>
        ChatCrypt.GDCryptKey _chatClientKey;

        /// <summary>
        /// Вспомогательный объект для шифрования байт перед отправкой игре
        /// </summary>
        ChatCrypt.GDCryptKey _chatServerKey;
        
        /// <summary>
        /// При создании или входе в IRC комнату созданного хоста (актуально только для авто) получает значение хэша этой команты.
        /// По этому хэшу мы сможем сообщить игре об изменениях в комнате.
        /// </summary>
        string _enteredLobbyHash;

        /// <summary>
        /// То же, что и <see cref="_enteredLobbyHash"/>. Имеет смысл только при создании комнаты IRC чата хостом.
        /// Поидее, в оригинальной реализации спая по этому хэшу все связываются в одной комнате, но здесь лишь создается иллюзия для игры, что эти хэши действительно используются.
        /// </summary>
        string _localServerHash;

        /// <summary>
        /// Указывает, вошел ли текущий пользователь в IRC чат
        /// </summary>
        bool _inChat;

        /// <summary>
        /// Указываем, является ли текущие состояние чата шифрованным.
        /// Все сообщения чата необходимо расшифровывать и шифровать, как только это значение стало true.
        /// Чат становится шифрованным после получения CRYPT команды от игры
        /// </summary>
        bool _chatEncoded;

        /// <summary>
        /// Длинное уникальное обозначение пользователя в IRC чате. 
        /// Формат обозначения ник!ид_пользователя@ip
        /// Где ид_пользователя имеет формат XхэшipX|номер_профиля"
        /// хэшip должен быть уникальный, но не обязательно должен быть хэшем. Сойдет и фейковых хэш
        /// </summary>
        string _user;

        /// <summary>
        /// ид_пользователя вырезанный из полного имени <see cref="_user"/>
        /// </summary>
        string _shortUser;

        /// <summary>
        /// Уникальный ник игрока. Не должен содержать спец символов и пробелов
        /// </summary>
        string _name;

        /// <summary>
        /// Обрабатывает получение нового подключения по TCP порту IRC чата
        /// </summary>
        void OnChatAccept(TcpPortHandler handler, TcpClientNode node, CancellationToken token)
        {
            _inChat = false;
            _chatEncoded = false;
        }

        /// <summary>
        /// Обрабатывает новое сообщение, полученное по TCP чата
        /// </summary>
        unsafe void OnChatReceived(TcpPortHandler handler, TcpClientNode node, byte[] buffer, int count)
        {
            // Дешифруем строку, если чат шифрованный
            if (_chatEncoded)
            {
                byte* bytesPtr = stackalloc byte[count];

                for (int i = 0; i < count; i++)
                    bytesPtr[i] = buffer[i];

                ChatCrypt.GSEncodeDecode(_chatClientKey, bytesPtr, count);

                for (int i = 0; i < count; i++)
                    buffer[i] = bytesPtr[i];
            }

            var str = buffer.ToUtf8(count);

            LogTrace(">>>>> " + str);

            // 
            var lines = str.Split(_chatSplitChars, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
                HandleChatLine(handler, node, lines[i]);

        }

        /// <summary>
        /// Обратывает строку IRC чата и вызывает соответствующий метод обработчик команды
        /// </summary>
        void HandleChatLine(TcpPortHandler handler, TcpClientNode node, string line)
        {
            var values = GetIrcChatLineValues(line);

            if (line.StartsWith("LOGIN", StringComparison.OrdinalIgnoreCase)) { HandleLoginCommand(handler, node, values); return; }
            if (line.StartsWith("USRIP", StringComparison.OrdinalIgnoreCase)) { HandleUsripCommand(handler, node, values); return; }
            if (line.StartsWith("CRYPT", StringComparison.OrdinalIgnoreCase)) { HandleCryptCommand(handler, node, values); return; }
            if (line.StartsWith("USER", StringComparison.OrdinalIgnoreCase)) { HandleUserCommand(handler, node, values); return; }
            if (line.StartsWith("NICK", StringComparison.OrdinalIgnoreCase)) { HandleNickCommand(handler, node, values); return; }
            if (line.StartsWith("CDKEY", StringComparison.OrdinalIgnoreCase)) { HandleCdkeyCommand(handler, node, values); return; }
            if (line.StartsWith("JOIN", StringComparison.OrdinalIgnoreCase)) { HandleJoinCommand(handler, node, line, values); return; }
            if (line.StartsWith("MODE", StringComparison.OrdinalIgnoreCase)) { HandleModeCommand(handler, node, values); return; }
            if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase)) { HandleQuitCommand(handler, values); return; }
            if (line.StartsWith("PRIVMSG", StringComparison.OrdinalIgnoreCase)) { HandlePrivmsgCommand(handler, values); return; }
            if (line.StartsWith("SETCKEY", StringComparison.OrdinalIgnoreCase)) { HandleSetckeyCommand(handler, node, line, values); return; }
            if (line.StartsWith("GETCKEY", StringComparison.OrdinalIgnoreCase)) { HandleGetckeyCommand(handler, node, values); return; }
            if (line.StartsWith("TOPIC", StringComparison.OrdinalIgnoreCase)) { HandleTopicCommand(handler, node, values); return; }
            if (line.StartsWith("PART", StringComparison.OrdinalIgnoreCase)) { HandlePartCommand(handler, values); return; }
            if (line.StartsWith("UTM", StringComparison.OrdinalIgnoreCase)) { HandleUtmCommand(handler, line); return; }
            if (line.StartsWith("PING", StringComparison.OrdinalIgnoreCase)) { HandlePingCommand(handler, node, values); return; }

            Debugger.Break();
        }

        /// <summary>
        /// Разбивает строку IRC сообщения на параметры, чтобы в дальнейшем с ними было удобнее работать
        /// </summary>
        /// <param name="line">Строка сообщение</param>
        /// <returns>Массив параметров. Первый элемент будет командой IRC</returns>
        string[] GetIrcChatLineValues(string line)
        {
            var args = new List<string>();
            string prefix;
            string command = string.Empty;

            try
            {
                int i = 0;
                /* This runs in the mainloop :: parser needs to return fast
                 * -> nothing which could block it may be called inside Parser
                 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
                if (line[0] == ':')
                {
                    /* we have a prefix */
                    while (line[++i] != ' ') { }

                    prefix = line.Substring(1, i - 1);
                }
                else
                {
                    prefix = _user;
                }

                int commandStart = i;
                /*command might be numeric (xxx) or command */
                if (char.IsDigit(line[i + 1]) && char.IsDigit(line[i + 2]) && char.IsDigit(line[i + 3]))
                {
                    //replyCode = (ReplyCode)int.Parse(line.Substring(i + 1, 3));
                    i += 4;
                }
                else
                {
                    while ((i < (line.Length - 1)) && line[++i] != ' ') { }

                    if (line.Length - 1 == i) { ++i; }
                    command = line.Substring(commandStart, i - commandStart);
                }

                args.Add(command);

                ++i;
                int paramStart = i;
                while (i < line.Length)
                {
                    if (line[i] == ' ' && i != paramStart)
                    {
                        args.Add(line.Substring(paramStart, i - paramStart));
                        paramStart = i + 1;
                    }
                    if (line[i] == ':')
                    {
                        if (paramStart != i)
                        {
                            args.Add(line.Substring(paramStart, i - paramStart));
                        }
                        args.Add(line.Substring(i + 1));
                        break;
                    }

                    ++i;
                }

                if (i == line.Length)
                {
                    args.Add(line.Substring(paramStart));
                }

            }
            catch (IndexOutOfRangeException)
            {
                LogWarn("Invalid Message: " + line);
                // invalid message
            }

            return args.ToArray();
        }

        public void SendServerPrivateMessageToChat(string message)
        {
            if (_name == null || !_inChat)
                return;

            // Фейк пользователя SERVER в чате, отправка в личку. Игра хавает ;)
            SendToClientChat($":SERVER!XaaaaaaaaX|10008@127.0.0.1 PRIVMSG {_name} :{message}\r\n");
        }

        /// <summary>
        /// Отправка IRC сообщения в игру. Должен соответствовать формату IRC и заканчиваться на \r\n
        /// </summary>
        unsafe void SendToClientChat(string message)
        {
            LogTrace("<<<<<<<<<<< " + message);

            var bytesToSend = message.ToUTF8Bytes();

            // Шифруем сообщение, если соединение уже с режиме шифрование
            if (_chatEncoded)
                fixed (byte* bytesToSendPtr = bytesToSend)
                    ChatCrypt.GSEncodeDecode(_chatServerKey, bytesToSendPtr, bytesToSend.Length);

            _chat.Send(bytesToSend);
        }

        unsafe void SendToClientChat(TcpClientNode node, string message)
        {
            LogTrace("<<<<<<<<<<< " + message);

            var bytesToSend = message.ToUTF8Bytes();

            // Шифруем сообщение, если соединение уже с режиме шифрование
            if (_chatEncoded)
                fixed (byte* bytesToSendPtr = bytesToSend)
                    ChatCrypt.GSEncodeDecode(_chatServerKey, bytesToSendPtr, bytesToSend.Length);

            _chat.Send(node, bytesToSend);
        }

        /// <summary>
        /// Отправляем список публичных комнат в чате.
        /// Сейчас фейкает только один чат рум GPG1 с названием "Room 1"
        /// Игра сама подставит красивое имя из HTTP данных о списке комнат
        /// </summary>
        /// <param name="validate">Строка валидации запроса. Должна быть получена от игры</param>
        void SendChatRooms(TcpPortHandler handler, TcpClientNode node, string validate)
        {
            var bytes = new List<byte>();

            //var remoteEndPoint = handler.RemoteEndPoint;
            //bytes.AddRange(remoteEndPoint.Address.GetAddressBytes());
            bytes.AddRange(IPAddress.Loopback.GetAddressBytes());

            byte[] value2 = BitConverter.GetBytes((ushort)6500);

            bytes.AddRange(BitConverter.IsLittleEndian ? value2.Reverse() : value2);

            bytes.Add(5); // fields count
            bytes.Add(0);


            // Забивает поля, которые нужны игре. В этом же порядке надо будет пихнуть значения дальше для каждой комнаты
            bytes.AddRange("hostname".ToAsciiBytes());
            bytes.Add(0);
            bytes.Add(0);
            bytes.AddRange("numwaiting".ToAsciiBytes());
            bytes.Add(0);
            bytes.Add(0);
            bytes.AddRange("maxwaiting".ToAsciiBytes());
            bytes.Add(0);
            bytes.Add(0);
            bytes.AddRange("numservers".ToAsciiBytes());
            bytes.Add(0);
            bytes.Add(0);
            bytes.AddRange("numplayersname".ToAsciiBytes());
            bytes.Add(0);
            bytes.Add(0);

            // Изначально было 10 комнат в игре, но мы сделаем только одну и весь код написан для синхронизации чата в игре с чатом из лаунчера

            // for (int i = 1; i <= 10; i++)
            // {

            // Странный байт в начале инфы о комнате
            bytes.Add(81);

            // инфа об IP комнаты, но игре на нее пофиг
            var b2 = BitConverter.GetBytes((long)1);

            bytes.Add(b2[3]);
            bytes.Add(b2[2]);
            bytes.Add(b2[1]);
            bytes.Add(b2[0]);

            // инфа о порте комнаты, но игре на нее пофиг
            bytes.Add(0);
            bytes.Add(0);

            // Скрытое название комнаты. Только такой формат принимает с цифрой в конце
            bytes.Add(255);
            bytes.AddRange("Room 1".ToAsciiBytes());
            bytes.Add(0);

            // Количество игроков в комнате
            bytes.Add(255);
            bytes.AddRange(_emulationAdapter.ActivePlayersCount.ToString().ToAsciiBytes());
            bytes.Add(0);

            bytes.Add(255);
            bytes.AddRange("1000".ToAsciiBytes());
            bytes.Add(0);

            bytes.Add(255);
            bytes.AddRange("1".ToAsciiBytes());
            bytes.Add(0);

            bytes.Add(255);
            bytes.AddRange("20".ToAsciiBytes());
            bytes.Add(0);
            // }

            // Непонятный набор байт в конце, но без него не сработает
            bytes.AddRange(new byte[] { 0, 255, 255, 255, 255 });

            var array = bytes.ToArray();

            // Шифруем алгоритмом спая. Участвует строка валидации и уникальный ключ игры
            byte[] enc = GSEncoding.Encode(_gameGSkeyBytes, validate.ToAsciiBytes(), array, array.LongLength);

            handler.Send(node, enc);

            handler.KillClient(node);
        }

        /// <summary>
        /// Фейковый хэш адреса IP
        /// Изначально задуман как уникальный идентификатор пользователя. 
        /// Но так как адреса у нас штука непостоянная, то делаем фейк от ника пользователя.
        /// Один и тот же ник должен давать один и тот же хэш
        /// </summary>
        string GetEncodedIp(string name)
        {
            var builder = new StringBuilder();

            var chars = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGJHKLZXCVBM1234567890";

           
            for (int i = 0; i < 8; i++)
            {
                var ch = name?.ElementAtOrDefault(i);

                if (!ch.HasValue)
                    builder.Append('a');
                else
                    builder.Append(chars[(((int)ch) * name.Length) % (chars.Length - 1)]);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Отправляет игре уведомление об изменении значения по ключе для пользователя в главной комнате
        /// </summary>
        public void SendUserKeyValueChanged(string name, string key, string value)
        {
            if (!_inChat)
                return;

            if (name == null || _name == name)
                return;

            SendToClientChat($":s 702 #GPG!1 #GPG!1 {name} BCAST :\\{key}\\{value}\r\n");
        }

        /// <summary>
        /// Обрабатывает смену ника пользователем.
        /// По сути мы фейкаем, что старый пользователь вышел из чата и новый вошел. Ник как бы изменился.
        /// Не должно вызываться для смены имени локального пользователя
        /// </summary>
        public void SendUserNameChanged(string newName, long? previousProfile = null, string previousName = null)
        {
            if (!_inChat)
                return;

            if (string.Equals(newName, _emulationAdapter.LocalUserName, StringComparison.Ordinal))
                return;

            if (previousName != null && previousProfile.HasValue)
                SendToClientChat($":{previousName}!X{GetEncodedIp(previousName)}X|{previousProfile.Value}@127.0.0.1 PART #GPG!1 :Leaving\r\n");

            if (!_emulationAdapter.IsUserHasActiveProfile(newName))
                return;

            InternalSendUserJoinedToChat(newName);
        }

        /// <summary>
        /// Уведомляем игру, что новый пользователь вошел в чат.
        /// Надо использовать для других юзеров, а не для того, кто авторизован
        /// </summary>
        public void SendUserConnectedToMainChat(string user)
        {
            if (!_inChat)
                return;

            if (string.Equals(user, _emulationAdapter.LocalUserName, StringComparison.Ordinal))
                return;

            if (_emulationAdapter.IsUserHasActiveProfile(user))
            {
                InternalSendUserJoinedToChat(user);
            }
        }

        /// <summary>
        /// Уведомляем игру, что пользователь вышел из чата
        /// Надо использовать для других юзеров, а не для того, кто авторизован
        /// </summary>
        public void SendUserDisconnectedFromMain(string user)
        {
            if (!_inChat)
                return;

            if (string.Equals(user, _emulationAdapter.LocalUserName, StringComparison.Ordinal))
                return;

            if (_emulationAdapter.IsUserHasActiveProfile(user))
                SendToClientChat($":{user}!X{GetEncodedIp(user)}X|{_emulationAdapter.GetUserInGameProfileId(user)}@127.0.0.1 PART #GPG!1 :Leaving\r\n");
        }

        /// <summary>
        /// Отправяем в чат игры уведомление о входе юзера в главную комнату чата. 
        /// Также фейкаем бродкаст от юзера об изменении данных по ключам b_stats и b_flags.
        /// Они нужны игре для того, чтобы принять пользователя в свои ряды и открывать ему статистику. Запрос на статистику при этом игрой будет делаться отдельно
        /// </summary>
        void InternalSendUserJoinedToChat(string name)
        {
            var userProfileId = _emulationAdapter.GetUserInGameProfileId(name);

            SendToClientChat($":{name}!X{GetEncodedIp(name)}X|{userProfileId}@127.0.0.1 JOIN #GPG!1\r\n");

            var bstats = _emulationAdapter.GetUserGlobalKeyValue(name, "b_stats");
            var bflags = _emulationAdapter.GetUserGlobalKeyValue(name, "b_flags");

            if (bstats != null)
                SendToClientChat($":s 702 #GPG!1 #GPG!1 {name} BCAST :\\b_stats\\{bstats}\r\n");
            else
            {
                var stats = _emulationAdapter.GetUserStatsInfo(name);

                SendToClientChat($":s 702 #GPG!1 #GPG!1 {name} BCAST :\\b_stats\\{userProfileId}|{stats.Score1v1}|{stats.StarsCount}|\r\n");
            }

            if (bflags != null)
                SendToClientChat($":s 702 #GPG!1 #GPG!1 {name} BCAST :\\b_flags\\{bflags}\r\n");
        }

        /// <summary>
        /// Фкйуовые хэш на основе ника. Нужен для идентификации команд, применимых к пользователям в IRC комнате
        /// </summary>
        string GetNickHash(string nick)
        {
            if (string.IsNullOrWhiteSpace(nick))
                return "---";

            int v = int.MaxValue;

            for (int i = 0; i < nick.Length; i++)
            {
                v ^= nick[i];
            }

            return Math.Abs(v).ToString();
        }

        /// <summary>
        /// Отправка сообщения в основную комнату чата "как бы" от юзера.
        /// Необходимо для синхронизации чатов. Не вызывать для локального юзера, если это его сообщение из чата, чтобы не было дублирования.
        /// </summary>
        /// <param name="name">Имя юзера, от которого сообщение</param>
        /// <param name="message">Текст сообщения</param>
        public void SendChatMessageToMainChat(string name, string message)
        {
            if (!_inChat)
                return;

            SendToClientChat($":{name}!XaaaaaaaaX|1@127.0.0.1 PRIVMSG #GPG!1 :{message}\r\n");
        }
    }
}
