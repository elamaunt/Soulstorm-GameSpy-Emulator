using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace GameSpyEmulator
{
    internal static class ParseHelper
    {
        /// <summary>
        /// Выполняет разбор сообщения сервера LOGIN (Client)
        /// </summary>
        public static Dictionary<string, string> ParseMessage(string message, out string query)
        {
            var parsedData = new Dictionary<string, string>();

            string[] responseData = message.Split(new string[] { @"\" }, StringSplitOptions.None);

            if (responseData.Length > 1)
            {
                query = responseData[1];
            }
            else
            {
                query = string.Empty;
                return null;
            }

            for (int i = 1; i < responseData.Length - 1; i += 2)
            {
                if (parsedData.ContainsKey(responseData[i]))
                {
                    parsedData[responseData[i].ToLowerInvariant()] = responseData[i + 1];
                }
                else
                {
                    parsedData.Add(responseData[i].ToLowerInvariant(), responseData[i + 1]);
                }
            }

            return parsedData;
        }

        /// <summary>
        /// Выполняет разбор предварительных данных хоста. Сплитим все в словарь
        /// </summary>
        public static GameServerDetails ParseDetails(string serverVars)
        {
            var serverVarsSplit = serverVars.Split(new string[] { "\x00" }, StringSplitOptions.None);

            var details = new GameServerDetails();

            for (int i = 0; i < serverVarsSplit.Length - 1; i += 2)
            {
                if (serverVarsSplit[i] == "hostname")
                    details.Set(serverVarsSplit[i], Regex.Replace(serverVarsSplit[i + 1], @"\s+", " ").Trim());
                else
                    details.Set(serverVarsSplit[i], serverVarsSplit[i + 1]);
            }

            return details;
        }

        /// <summary>
        /// Упаковывает список хостов в понятный игре формат
        /// </summary>
        public static byte[] PackServerList(IPEndPoint remoteEndPoint, IEnumerable<GameServerDetails> servers, string[] fields, bool needAutomatchServers)
        {
            List<byte> data = new List<byte>(1024);

            // забиваем адрес (ни на что не влияет)
            data.AddRange(remoteEndPoint.Address.GetAddressBytes());

            // забиваем порт (ни на что не влияет)
            byte[] value2 = BitConverter.GetBytes((ushort)remoteEndPoint.Port);
            data.AddRange(BitConverter.IsLittleEndian ? value2.Reverse() : value2);

            // Фиксим вывод, если полей нет
            if (fields.Length == 1 && fields[0] == "\u0004")
                fields = new string[0];

            // забиваем количество полей
            data.Add((byte)fields.Length);
            data.Add(0);

            // забиваем список полей
            foreach (var field in fields)
            {
                data.AddRange(Encoding.UTF8.GetBytes(field));
                data.AddRange(new byte[] { 0, 0 });
            }

            foreach (var server in servers)
            {
                // Вспомогательная фильтрация матчей кастомок и авто
                if (server.TryGetValue("gamename", out string gamename))
                {
                    if (needAutomatchServers && !server.Ranked)
                        continue;

                    if (!needAutomatchServers && server.Ranked)
                        continue;
                }

                // Блок для исключения хостов, которые созданы в режиме разработчика. Иначе можно накрутить статистику.
                if (server.DevMode)
                    continue;

                // Не надо исправлять localport, если юзера в одной подсети. Можно подсунуть свой порт
                var port = (server.HostPort ?? server.LocalPort).ParseToIntOrDefault();
                var retranslationPortBytes = BitConverter.IsLittleEndian ? BitConverter.GetBytes(port).Reverse() : BitConverter.GetBytes(port);

                // Флаги - настройки того, как игра будет понимать указанный ниже адрес. Говорим, что это внешний адрес, который получен путем NAT-проброса.
                // Таким образом игра примет любой адрес, что мы подсунем. В том числе и локалхост или несуществующий адрес и порт.
                // Игра будет передавать этот адрес другим игрокам при подключении в командное авто. В идеале адреса должны статические, тогда авто будет работать сразу же.
                var flags = ServerFlags.UNSOLICITED_UDP_FLAG |
                    ServerFlags.PRIVATE_IP_FLAG |
                    ServerFlags.NONSTANDARD_PORT_FLAG |
                    ServerFlags.NONSTANDARD_PRIVATE_PORT_FLAG |
                    ServerFlags.HAS_KEYS_FLAG;

                data.Add((byte)flags);

                // Информация об адресе хоста. Этот адрес будет использоваться игрой при попытке подключиться. Можно подсунуть свой IP.
                // Не надо исправлять адрес, если пользователи в одной подсети
                var hostIpBytes = IPAddress.Parse(server.HostIP ?? server.LocalIP).GetAddressBytes(); //IPAddress.Loopback.GetAddressBytes();
                data.AddRange(hostIpBytes);
                data.AddRange(retranslationPortBytes);
                data.AddRange(hostIpBytes);
                data.AddRange(retranslationPortBytes);

                data.Add(255);

                for (int i = 0; i < fields.Length; i++)
                {
                    var name = fields[i];
                    var f = GetField(server, name);

                    data.AddRange(Encoding.UTF8.GetBytes(f));

                    if (i < fields.Length - 1)
                    {
                        data.Add(0);
                        data.Add(255);
                    }
                }

                data.Add(0);
            }

            // Завершающий набор байт
            data.Add(0);
            data.Add(255);
            data.Add(255);
            data.Add(255);
            data.Add(255);

            return data.ToArray();
        }

        /// <summary>
        /// Извлекает поле из хоста
        /// </summary>
        private static string GetField(GameServerDetails server, string fieldName)
        {
            var value = server.GetOrDefault(fieldName);
            if (value == null)
                return string.Empty;
            return value;
        }
    }
}
