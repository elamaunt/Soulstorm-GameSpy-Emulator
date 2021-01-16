using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using static GameSpyEmulator.TcpPortHandler;

namespace GameSpyEmulator
{
    public partial class GameSpyServer
    {
        volatile int _sessionCounter;

        /// <summary>
        /// Обрабатывает получение нового TPC соединения для получения статистики игроков
        /// </summary>
        void OnStatsAccept(TcpPortHandler handler, TcpClientNode node, CancellationToken token)
        {
            // Отправляем challenge сервера шифрованный через Xor. Надо отправить, но особо ни на что не влияет
            // Все сообщения также надо будет прогонять через Xor.
            handler.Send(GSUtils.XorBytes(@"\lc\1\challenge\KNDVKXFQWP\id\1\final\", "GameSpy3D", 7));
        }

        /// <summary>
        /// Обрабатывает полученное сообщение по TCP соединению для запросов статистики
        /// </summary>
        void OnStatsReceived(TcpPortHandler handler, TcpClientNode node, byte[] buffer, int count)
        {
            var str = Encoding.UTF8.GetString(GSUtils.XorBytes(buffer, 0, count - 7, "GameSpy3D"), 0, count);

            LogTrace("STATS " + str);

            // Авторизация на сервере. Выдаем случайный ключ сессии (не обязательно)
            if (str.StartsWith(@"\auth\\gamename\", StringComparison.OrdinalIgnoreCase))
            {
                var sesskey = Interlocked.Increment(ref _sessionCounter).ToString("0000000000");

                handler.Send(node, GSUtils.XorBytes($@"\lc\2\sesskey\{sesskey}\proof\0\id\1\final\", "GameSpy3D", 7));
                return;
            }

           // Игра присылает серверу ID авторизованного профиля. Но мы и так его знаем, поэтому просто фейкаем успех
            if (str.StartsWith(@"\authp\\pid\", StringComparison.OrdinalIgnoreCase))
            {
                var pid = GetPidFromInput(str, 12);
                var profileId = long.Parse(pid);

                handler.Send(node, GSUtils.XorBytes($@"\pauthr\{pid}\lid\1\final\", "GameSpy3D", 7));
                return;
            }

            // Запрос данных профиля. Надо отправить данные по списку запрошенных ключей.
            // Всегда запрашивается статистика
            if (str.StartsWith(@"\getpd\", StringComparison.OrdinalIgnoreCase))
            {
                // \\getpd\\\\pid\\87654321\\ptype\\3\\dindex\\0\\keys\\\u0001points\u0001points2\u0001points3\u0001stars\u0001games\u0001wins\u0001disconn\u0001a_durat\u0001m_streak\u0001f_race\u0001SM_wins\u0001Chaos_wins\u0001Ork_wins\u0001Tau_wins\u0001SoB_wins\u0001DE_wins\u0001Eldar_wins\u0001IG_wins\u0001Necron_wins\u0001lsw\u0001rnkd_vics\u0001con_rnkd_vics\u0001team_vics\u0001mdls1\u0001mdls2\u0001rg\u0001pw\\lid\\1\\final\\
                // \getpd\\pid\87654321\ptype\3\dindex\0\keys\pointspoints2points3starsgameswinsdisconna_duratm_streakf_raceSM_winsChaos_winsOrk_winsTau_winsSoB_winsDE_winsEldar_winsIG_winsNecron_winslswrnkd_vicscon_rnkd_vicsteam_vicsmdls1mdls2rgpw\lid\1\final\
                var profileId = GetPidFromInput(str, 12);

                var keysIndex = str.IndexOf("keys") + 5;
                var keys = str.Substring(keysIndex);
                var keysList = keys.Split(new string[] { "\u0001", "\\lid\\1\\final\\", "final", "\\", "lid" }, StringSplitOptions.RemoveEmptyEntries);

                var keysResult = new StringBuilder();
                var stats = _emulationAdapter.GetUserStatsInfo(profileId.ParseToLongOrDefault());

                for (int i = 0; i < keysList.Length; i++)
                {
                    var key = keysList[i];

                    keysResult.Append("\\" + key + "\\");

                    switch (key)
                    {
                        case "points": keysResult.Append(stats.Score1v1); break;
                        case "points2": keysResult.Append(stats.Score2v2); break;
                        case "points3": keysResult.Append(stats.Score3v3_4v4); break;
                        case "stars": keysResult.Append(stats.StarsCount); break;
                        case "games": keysResult.Append(stats.GamesCount); break;
                        case "wins": keysResult.Append(stats.WinsCount); break;
                        case "disconn": keysResult.Append(stats.Disconnects); break;
                        case "a_durat": keysResult.Append(stats.AverageDuration); break;
                        case "m_streak": keysResult.Append(stats.Best1v1Winstreak); break;
                        case "f_race": keysResult.Append(stats.FavouriteRace); break;

                         // Ключи, которые не используюся игрой, но запрашиваются. Может на что-то и влияет, но я ничего не обнаружил
                        /* case "SM_wins": keysResult.Append("0"); break;
                         case "Chaos_wins": keysResult.Append("0"); break;
                         case "Ork_wins": keysResult.Append("0"); break;
                         case "Tau_wins": keysResult.Append("0"); break;
                         case "SoB_wins": keysResult.Append("0"); break;
                         case "DE_wins": keysResult.Append("0"); break;
                         case "Eldar_wins": keysResult.Append("0"); break;
                         case "IG_wins": keysResult.Append("0"); break;
                         case "Necron_wins": keysResult.Append("0"); break;
                         case "lsw": keysResult.Append("0"); break;
                         case "rnkd_vics": keysResult.Append("0"); break;
                         case "con_rnkd_vics": keysResult.Append("0"); break;
                         case "team_vics": keysResult.Append("0"); break;
                         case "mdls1": keysResult.Append("0"); break;
                         case "mdls2": keysResult.Append("0"); break;
                         case "rg": keysResult.Append("0"); break;
                         case "pw": keysResult.Append("0"); break;*/
                        default:
                            keysResult.Append("0");
                            break;
                    }
                }

                handler.Send(node, GSUtils.XorBytes($@"\getpdr\1\lid\1\pid\{profileId}\mod\{stats.ModifiedTimeTick}\length\{keys.Length}\data\{keysResult}\final\", "GameSpy3D", 7));

                return;
            }

            // Игра присылает обновление данных профиля по списку ключей. 
            // Игнорируем, потому что статистика обновляется другим способов. Просто фейкаем успех
            if (str.StartsWith(@"\setpd\", StringComparison.OrdinalIgnoreCase))
            {
                var pid = GetPidFromInput(str, 12);

                var lidIndex = str.IndexOf("\\lid\\", StringComparison.OrdinalIgnoreCase);
                var lid = str.Substring(lidIndex + 5, 1);

                var timeInSeconds = (ulong)((DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
                // \setpd\\pid\3\ptype\1\dindex\0\kv\1\lid\1\length\413\data\\ckey\5604 - 7796 - 6425 - 0127 - DA96\system\Nr.Proc:8, Type: 586, GenuineIntel, unknown: f = 6,m = 12, Fam: 6, Mdl: 12, St: 3, Fe: 7, OS: 7, Ch: 15\speed\CPUSpeed: 3.5Mhz\os\OS NT 6.2\lang\Language: Русский(Россия), Country: Россия, User Language:Русский(Россия), User Country:Россия\vid\Card: Dx9: Hardware TnL, NVIDIA GeForce GTX 1080, \vidmod\Mode: 1920 x 1080 x 32\mem\2048Mb phys. memory
                handler.Send(node, GSUtils.XorBytes($@"\setpdr\1\lid\{lid}\pid\{pid}\mod\{timeInSeconds}\final\", "GameSpy3D", 7));

                return;
            }

            // Игры присылает информацию о завершенной игре (камстока или автоматч)
            // Отсюда можно обновлять статистику игроков. Может быть прислано несколько раз от разных игроков в конце одного и того же матча,
            // поэтому необходима защита на сервере от двойного засчитывания статистики
            if (str.StartsWith(@"\updgame\", StringComparison.OrdinalIgnoreCase))
            {
                var gamedataIndex = str.IndexOf("gamedata");

                // Обрезаем конец сообщения
                var finalIndex = str.IndexOf("final");
                var gameDataString = str.Substring(gamedataIndex + 9, finalIndex - gamedataIndex - 10);

                var valuesList = gameDataString.Split(new string[] { "\u0001", "\\lid\\1\\final\\", "\\" }, StringSplitOptions.None);

                // Преобразываем все пары ключ-значение в словарь для удобства
                var dictionary = new Dictionary<string, string>();

                for (int i = 0; i < valuesList.Length - 1; i += 2)
                {
                    if (i == valuesList.Length - 1)
                        continue;

                    dictionary[valuesList[i]] = valuesList[i + 1];
                }

                if (!dictionary.TryGetValue("Mod", out string v))
                {
                    dictionary.Clear();

                    for (int i = 1; i < valuesList.Length - 1; i += 2)
                    {
                        if (i == valuesList.Length - 1)
                            continue;

                        dictionary[valuesList[i]] = valuesList[i + 1];
                    }
                }

                // Использованный мод и его версия
                var mod = dictionary["Mod"];
                var modVersion = dictionary["ModVer"];

                var playersCount = int.Parse(dictionary["Players"]);

                for (int i = 0; i < playersCount; i++)
                {
                    // Dont process games with AI
                    if (dictionary["PHuman_" + i] != "1")
                    {
                        LogTrace($"Stats socket: GAME WITH NONHUMAN PLAYER");
                        return;
                    }
                }

                var gameInternalSession = dictionary["SessionID"];
                var teamsCount = int.Parse(dictionary["Teams"]);
                var version = dictionary["Version"];

                // Строим уникальные идентификатор сессии игры, чтобы в дальнейшем не зачислить дважды одну и ту же игру
                var uniqueGameSessionBuilder = new StringBuilder(gameInternalSession);

                for (int i = 0; i < playersCount; i++)
                {
                    uniqueGameSessionBuilder.Append('<');
                    uniqueGameSessionBuilder.Append(dictionary["player_" + i]);
                    uniqueGameSessionBuilder.Append('>');
                }

                var uniqueSession = uniqueGameSessionBuilder.ToString();

                // Строим объекты с данным игроков
                var players = new PlayerData[playersCount];

                for (int i = 0; i < players.Length; i++)
                {
                    var player = new PlayerData();

                    player.Name = dictionary["player_" + i];
                    player.Race = dictionary["PRace_" + i];
                    player.Team = int.Parse(dictionary["PTeam_" + i]);
                    player.FinalState = (PlayerFinalState)Enum.Parse(typeof(PlayerFinalState), dictionary["PFnlState_" + i]);

                    players[i] = player;
                }

                // Собираем окончательный объект с данными
                var gameFinishedMessage = new GameFinishedData
                {
                    Map = dictionary["Scenario"],
                    SessionId = uniqueSession,
                    Duration = long.Parse(dictionary["Duration"]),
                    ModName = dictionary["Mod"],
                    ModVersion = dictionary["ModVer"],
                    Players = players,
                    IsRateGame = dictionary["Ladder"] == "1"
                };

                _emulationAdapter.SendGameFinishedData(gameFinishedMessage);

                //DowstatsReplaySender.SendReplay(gameFinishedMessage);

                return;
            }

            // Создание новой игры до регистрации статистики. Никакой полезной информации нет, поэтому игнорируем. 
            // Вся логика произойдет в момент отправки данных об игре.
            if (str.StartsWith(@"\newgame\", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // На случай, если есть еще какие-то команды
            Debugger.Break();
        }
       
        /// <summary>
        /// Извлекает ID профиля из строки запроса для STATS сервера
        /// </summary>
        string GetPidFromInput(string input, int start)
        {
            int end = start;
            while (true)
            {
                var ch = input[end++];

                if (!char.IsDigit(ch))
                    break;
            }

            return input.Substring(start, end - start - 1);
        }
    }
}
