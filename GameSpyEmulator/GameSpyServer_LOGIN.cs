using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Reality.Net.Extensions;
using Reality.Net.GameSpy.Servers;
using static GameSpyEmulator.TcpPortHandler;

namespace GameSpyEmulator
{
    public partial class GameSpyServer
    {
        Timer _keepAliveSessionTimer;
        volatile int _sessionHeartbeatState;

        string _serverChallenge;
        string _clientChallenge;

        long _profileId;
        string _email;
        string _loginResponseUID;
        
        /// <summary>
        /// Обрабатывает входящие TCP соединения для сервера LOGIN (Client)
        /// </summary>
        void OnClientAccept(TcpPortHandler handler, TcpClientNode node, CancellationToken token)
        {
            //Обновляем челендж для нового соединения
            _serverChallenge = RandomHelper.GetString(10);
            handler.SendAskii(node, $@"\lc\1\challenge\{_serverChallenge}\id\1\final\");
        }

        /// <summary>
        /// Обрабатывает входящее сообщение сервера LOGIN (Client)
        /// </summary>
        void OnClientManagerReceived(TcpPortHandler handler, TcpClientNode node, byte[] buffer, int count)
        {
            // Несколько сообщения может быть за раз - сплитим
            var messages = buffer.ToUtf8(count).Split(new string[] { @"\final\" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < messages.Length; i++)
                HandleClientManagerMessage(handler, node, messages[i]);
        }

        /// <summary>
        /// Обрабатывает одно сообщение сервера
        /// </summary>
        private void HandleClientManagerMessage(TcpPortHandler handler, TcpClientNode node, string mes)
        {
            LogTrace("CLIENT " + mes);
            var pairs = ParseHelper.ParseMessage(mes, out string query);

            if (pairs == null || string.IsNullOrWhiteSpace(query))
                return;

            // Исправление бага, когда игра по какой-то причине соединяет логин и почту в одну строку. Разбиваем, иначе не будет работать алгоритм хэширования при логине
            if (pairs.ContainsKey("name") && !pairs.ContainsKey("email"))
            {
                var parts = pairs["name"].Split('@');

                if (parts.Length > 2)
                {
                    pairs["name"] = parts[0];
                    pairs["email"] = parts[1] + "@" + parts[2];
                }
            }

            switch (query)
            {
                case "login":
                    HandleLogin(node, pairs);
                    RestartUserSessionTimer(node);
                    break;
                case "logout":
                    _emulationAdapter.LeaveFromCurrentLobby();
                    _emulationAdapter.OnLogout();
                    break;
                case "registernick":
                    handler.SendAskii(node, string.Format(@"\rn\{0}\id\{1}\final\", pairs["uniquenick"], pairs["id"]));
                    break;
                case "ka":
                    handler.SendAskii(node, $@"\ka\\final\");
                    break;
                case "status":
                    HandleStatus(node, pairs);
                    break;
                case "newuser":
                    {
                        var nick = pairs["nick"];
                        var email = pairs["email"];
                        var password = GSUtils.DecryptPassword(pairs["passwordenc"]);
                        var passHash = password.ToMD5();

                        _emulationAdapter.TryCreateProfile(nick, email, passHash);
                    }
                    break;
                case "getprofile":
                    // TODO
                    break;
                default:
                    Debugger.Break();
                    break;
            }
        }

        /// <summary>
        /// Обрабатывает запрос статуса для авторизованного пользователя.
        /// Также в этом запрос поидее надо передавать список друзей и их статусы
        /// </summary>
        void HandleStatus(TcpClientNode node, Dictionary<string, string> pairs)
        {
            // TODO: этот метод требует доработки

            _clientManager.SendAskii(node, $@"\bdy\{0}\list\\final\");

            var status = pairs.GetOrDefault("status") ?? "0";
            var statusString = pairs.GetOrDefault("statstring") ?? "Offline";
            var locString = pairs.GetOrDefault("locstring") ?? "-1";

            string lsParameter;

            if (string.IsNullOrWhiteSpace(locString) || locString == "0")
                lsParameter = string.Empty;
            else
                lsParameter = "|ls|" + locString;

            var statusResult = $@"\bm\100\f\{_profileId}\msg\|s|{status}{lsParameter}|ss|{statusString}\final\";

            _clientManager.SendAskii(node, statusResult);
        }

       /// <summary>
       /// Обрабатывает запрос логина
       /// </summary>
        void HandleLogin(TcpClientNode node, Dictionary<string, string> pairs)
        {
            /*var (activeMod, activeVersion) = ActiveModDetector.DetectCurrentMode();

            LogForUser($"Active mod [{activeMod}] [{activeVersion}]");

            CoreContext.ThunderHawkModManager.CurrentModName = activeMod;
            CoreContext.ThunderHawkModManager.CurrentModVersion = activeVersion;

            if (activeMod == null || activeVersion == null)
            {
                LogForUser($"HandleLogin invalid mode");

                _clientManager.Send(node, DataFunctions.StringToBytes(@"\error\\err\0\fatal\\errmsg\You can login only with ThunderHawk active mod.\id\1\final\"));
                CoreContext.SystemService.ShowMessageWindow($"Temporary entry is allowed only with the active {CoreContext.ThunderHawkModManager.ValidModName} {CoreContext.ThunderHawkModManager.ValidModVersion} mod.");
                return;
            }

            if (!activeMod.Equals(CoreContext.ThunderHawkModManager.ValidModName, StringComparison.OrdinalIgnoreCase) && !activeMod.Equals(CoreContext.ThunderHawkModManager.JBugfixModName, StringComparison.OrdinalIgnoreCase))
            {
                if (!activeVersion.Equals(CoreContext.ThunderHawkModManager.ValidModVersion, StringComparison.OrdinalIgnoreCase))
                {
                    LogForUser($"HandleLogin invalid mode {activeMod}");

                    _clientManager.Send(node, DataFunctions.StringToBytes(@"\error\\err\0\fatal\\errmsg\You can login only with ThunderHawk active mod.\id\1\final\"));
                    CoreContext.SystemService.ShowMessageWindow($"Temporary entry is allowed only with the active {CoreContext.ThunderHawkModManager.ValidModName} {CoreContext.ThunderHawkModManager.ValidModVersion} mod.");
                    return;
                }
            }*/

            // Выполняем обработку данных при попытке авторизации в игре.
            // Выполнение этой операции игра может ждать долго. Поэтому можно спокойно делать запросы на сервер
            // Отдаем право определить результат адаптеру.

            // Исправление бага, когда игра по какой-то причине соединяет логин и почту в одну строку. Разбиваем, иначе не будет работать алгоритм хэширования при логине
            if (pairs.ContainsKey("uniquenick"))
                _name = pairs["uniquenick"];
            else
            {
                var parts = pairs["user"].Split('@');
                _name = parts[0];
                _email = parts[1] + "@" + parts[2];
            }

            _clientChallenge = pairs.GetOrDefault("challenge");
            _loginResponseUID = pairs.GetOrDefault("response");

            LogForUser($"HandleLogin send request");
            _emulationAdapter.RequestLogin(_name);
        }


        /// <summary>
        /// Отправляем игре хэш код на основе информации при авторизации.
        /// Если хэш код совпадет в игре, то это говорит об успехе авторизации
        /// На этом этапе также можно отправить ошибку и отклонить авторизацию
        /// </summary>
        public void SendLoginResponce(LoginInfo loginInfo)
        {
            LogForUser($"LoginResponce [{loginInfo.Name}] [{loginInfo.Email}] [{loginInfo.ProfileId}]");

            _email = loginInfo.Email;
            _profileId = loginInfo.ProfileId;

            /*if (CoreContext.LaunchService.GetCurrentModName() != "thunderhawk")
            {
                _clientManager.Send(DataFunctions.StringToBytes(@"\error\\err\0\fatal\\errmsg\You can login only with ThunderHawk active mod.\id\1\final\"));
                MessageBox.Show("You can login only with ThunderHawk active mod.");
            }
            else*/

            _sessionHeartbeatState = 0;
            _clientManager.Send(DataFunctions.StringToBytes(LoginHelper.BuildProofOrErrorString(loginInfo, _loginResponseUID, _clientChallenge, _serverChallenge)));
        }

        void OnSearchManagerReceived(TcpPortHandler handler, TcpClientNode node, byte[] buffer, int count)
        {
            var str = buffer.ToUtf8(count);
            var pairs = ParseHelper.ParseMessage(str, out string query);


            switch (query)
            {
                case "nicks":
                    {
                        // \\nicks\\\\email\\elamaunt3@gmail.com\\passenc\\J4PGhRi[\\namespaceid\\7\\partnerid\\0\\gamename\\whamdowfr\\final\\

                        if (!pairs.ContainsKey("email") || (!pairs.ContainsKey("passenc") && !pairs.ContainsKey("pass")))
                        {
                            handler.SendAskii(node, @"\error\\err\0\fatal\\errmsg\Invalid Query!\id\1\final\");
                            return;
                        }

                        // Чей-то тестовый код
                        /*
                        string password = String.Empty;
                        if (pairs.ContainsKey("passenc"))
                        {
                            password = GSUtils.DecryptPassword(pairs["passenc"]);
                        }
                        else if (pairs.ContainsKey("pass"))
                        {
                            password = pairs["pass"];
                        }

                        password = password.ToMD5();*/

                        _emulationAdapter.RequestAllUserNicks(pairs["email"]);
                        return;
                    }
                case "check":
                    {
                        string name = String.Empty;

                        if (String.IsNullOrWhiteSpace(name))
                        {
                            if (pairs.ContainsKey("uniquenick"))
                            {
                                name = pairs["uniquenick"];
                            }
                        }
                        if (String.IsNullOrWhiteSpace(name))
                        {
                            if (pairs.ContainsKey("nick"))
                            {
                                name = pairs["nick"];
                            }
                        }

                        if (String.IsNullOrWhiteSpace(name))
                        {
                            handler.SendAskii(node, @"\error\\err\0\fatal\\errmsg\Invalid Query!\id\1\final\");
                            return;
                        }

                        _emulationAdapter.RequestNameCheck(name);
                        return;
                    }
                default:
                    break;
            }

            Debugger.Break();
        }

        /// <summary>
        /// Возвращаем неудачу при регистрации нового аккаунта
        /// </summary>
        public void SendCreateProfileFail()
        {
            LogForUser($"NewUserReceived error already exists!");
            _clientManager.SendAskii(@"\error\\err\516\fatal\\errmsg\This account name is already in use!\id\1\final\");
        }

        /// <summary>
        /// Возвращаем игре успех при регистрации нового аккаунта
        /// </summary>
        public void SendCreateProfileSuccess(long profileId)
        {
            LogForUser($"NewUserReceived success {profileId}");
            _clientManager.SendAskii(string.Format(@"\nur\\userid\{0}\profileid\{1}\id\1\final\", profileId + 10000000, profileId));
        }

        /// <summary>
        /// Возвращаем игре информацию о занятости ника. Ник занят :(
        /// </summary>
        public void SendNameCheckFail(string name)
        {
            LogForUser($"NameCheckReceived [{name}] doesn't exist");

            _searchManager.SendAskii(String.Format(@"\error\\err\265\fatal\\errmsg\Username [{0}] doesn't exist!\id\1\final\", name));
        }

        /// <summary>
        /// Возвращаем игре информацию о занятости ника. Ник свободен!
        /// </summary>
        public void SendNameCheckSuccess(string name, long profileId)
        {
            LogForUser($"NameCheckReceived [{name}] has id {profileId}");

            _searchManager.SendAskii($@"\cur\0\pid\{profileId}\final\");
        }

        /// <summary>
        /// Генерируем предложение ников для игрока, которых ввел уже существующий ник при регистрации
        /// </summary>
        /// <param name="nicks"></param>
        public void SendNicksOffer(string[] nicks)
        {
            LogForUser($"Nicks received {nicks?.Length}");

            if (nicks.IsNullOrEmpty())
            {
                _searchManager.SendAskii(@"\nr\0\ndone\\final\");
                return;
            }

            _searchManager.SendAskii(GenerateNicks(nicks));
        }

        /// <summary>
        /// Говорим игре, что текущая попытка логина оказалась неудачной
        /// </summary>
        public void SendLoginError(string name)
        {
            LogForUser($"LoginError {name}");

            _clientManager.SendAskii(@"\error\\err\0\fatal\\errmsg\Invalid Query!\id\1\final\");
        }

        /// <summary>
        /// Оформляем ники в понятный игре вид
        /// </summary>
        string GenerateNicks(string[] nicks)
        {
            string message = @"\nr\" + nicks.Length;

            for (int i = 0; i < nicks.Length; i++)
                message += String.Format(@"\nick\{0}\uniquenick\{0}", nicks[i]);

            message += @"\ndone\final\";
            return message;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void RestartUserSessionTimer(TcpClientNode node)
        {
            // Должен быть активен после авторизации и периодически (ровно раз в две минуты) проверять соединение, иначе
            // игра перестанет держать соединение активным и может игнорировать какие-то запросы
            StopTimer();
            _sessionHeartbeatState = 0;

            // Первое проигрывание таймера сделаем раньше на 5 секунд
            _keepAliveSessionTimer = new Timer(KeepAliveCallback, node, TimeSpan.FromSeconds(120 - 5), TimeSpan.FromMinutes(2));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void StopTimer()
        {
            _keepAliveSessionTimer?.Dispose();
            _keepAliveSessionTimer = null;
        }

        void KeepAliveCallback(object state)
        {
            _sessionHeartbeatState++;

            LogTrace("sending keep alive");

            // Если не удалось оправить игре ka (keep alive) метку, значит игра уже не в лобби спая. Перезапускаем все соединения, если это так.
            if (!_clientManager.SendAskii((TcpClientNode)state, @"\ka\\final\"))
            {
                Restart();
                return;
            }

            // every 2nd keep alive request, we send an additional heartbeat
            if ((_sessionHeartbeatState & 1) == 0)
            {
                LogTrace("sending heartbeat");
                if (!_clientManager.SendAskii((TcpClientNode)state, String.Format(@"\lt\{0}\final\", RandomHelper.GetString(22, "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ][") + "__")))
                {
                    Restart();
                    return;
                }
            }
        }
    }
}
