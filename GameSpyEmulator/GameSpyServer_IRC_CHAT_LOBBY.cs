using System;
using System.Diagnostics;

namespace GameSpyEmulator
{
    public partial class GameSpyServer
    {
        /// <summary>
        /// Уведомляем игру, что пользователь ливнул из комнаты IRC, созданной для лобби. Актуально для авто
        /// </summary>
        public void SendLobbyMemberLeft(string name, long profileId)
        {
            LogForUser($"RemoteLeft {_enteredLobbyHash} {GetNickHash(name)}");
            SendToClientChat($":{name}!X{GetEncodedIp(name)}X|{profileId}@127.0.0.1 PART #GSP!whamdowfr!{_enteredLobbyHash} :Leaving\r\n");
        }

        /// <summary>
        /// Обрабатывает строку IRC сообщение от другого пользователя при нахождении в одной скрытой комнате. Актуально для авто.
        /// </summary>
        public void SendLobbyBroadcast(string name, string line)
        {
            Log("RECV-FROM-LOBBY-CHAT " + line);

            var values = GetIrcChatLineValues(line);

            if (line.StartsWith("UTM", StringComparison.OrdinalIgnoreCase)) { HandleRemoteUtmCommand(values); return; }
            if (line.StartsWith("PRIVMSG", StringComparison.OrdinalIgnoreCase)) { HandleRemotePrivmsgCommand(values); return; }

            if (string.Equals(_name, name, StringComparison.Ordinal))
                return;

            if (line.StartsWith("JOIN", StringComparison.OrdinalIgnoreCase)) { HandleRemoteJoinCommand(values); return; }
            if (line.StartsWith("SETCKEY", StringComparison.OrdinalIgnoreCase)) { HandleRemoteSetckeyCommand(values); return; }

            Debugger.Break();
        }

        /// <summary>
        /// Обратывает вход другого пользователя в чат-комнату автоматча
        /// </summary>
        void HandleRemoteJoinCommand(string[] values)
        {
            // :Bambochuk2!Xu4FpqOa9X|4@192.168.159.128 JOIN #GSP!whamdowfr!76561198408785287

            if (string.Equals(values[1], _user, StringComparison.Ordinal))
                return;

            var userValues = values[1].Split(new char[] { '!', '|', '@' });
            var nick = userValues[0];

            LogForUser($"RemoteJoin {_enteredLobbyHash} {GetNickHash(nick)}");

            SendToClientChat($":{values[1]} JOIN #GSP!{_gameGSkey}!{_enteredLobbyHash}\r\n");
        }

        /// <summary>
        /// Обратывает изменения пары ключ-значение у пользователя
        /// </summary>
        void HandleRemoteSetckeyCommand(string[] values)
        {
            var channelName = values[1];

            if (channelName.StartsWith("#GSP", StringComparison.OrdinalIgnoreCase))
            {
                var keyValues = values[3];

                var pairs = keyValues.Split(':', '\\');

                for (int i = 1; i < pairs.Length; i += 2)
                {
                    LogForUser($"RemoteSetckey {_enteredLobbyHash} {GetNickHash(values[2])} [{pairs[i]}] [{pairs[i + 1]}]");
                    SendToClientChat($":s 702 #GSP!{_gameGSkey}!{_enteredLobbyHash} #GSP!{_gameGSkey}!{_enteredLobbyHash} {values[2]} BCAST :\\{pairs[i]}\\{pairs[i + 1]}\r\n");
                }
            }
        }

        /// <summary>
        /// Обратабываем личное сообщение в лобби.
        /// Игра использует это, чтобы сообщить, что матч готов начаться (Нахрена??). 
        /// Но начала происходит на команду UTM
        /// </summary>
        void HandleRemotePrivmsgCommand(string[] values)
        {
            LogForUser($"RemotePrivmsg {_enteredLobbyHash} {values[2]}");

            SendToClientChat($":{_user} PRIVMSG #GSP!{_gameGSkey}!{_enteredLobbyHash} :{values[2]}\r\n");
        }

        /// <summary>
        /// Обратабываем спец команду UTM в лобби. 
        /// Получение этой команды значит автоматч готов начаться
        /// </summary>
        void HandleRemoteUtmCommand(string[] values)
        {
            LogForUser($"RemoteUtm {values[2]}");

            _emulationAdapter.OnRemoteUserHasLaunchedTheGame();
            SendToClientChat($":{_user} UTM #GSP!{_gameGSkey}!{_enteredLobbyHash} :{values[2]}\r\n");
        }
    }
}
