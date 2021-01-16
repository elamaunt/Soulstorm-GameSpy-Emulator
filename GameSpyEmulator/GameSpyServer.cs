using System;
using System.Runtime.CompilerServices;
using static GameSpyEmulator.TcpPortHandler;

namespace GameSpyEmulator
{
    public partial class GameSpyServer
    {
        readonly IEmulationAdapter _emulationAdapter;

        UdpPortHandler _serverReport;
        TcpPortHandler _serverRetrieve;
        TcpPortHandler _clientManager;
        TcpPortHandler _searchManager;
        TcpPortHandler _chat;
        TcpPortHandler _stats;
        TcpPortHandler _http;

        public GameSpyServer(IEmulationAdapter handler)
        {
            _emulationAdapter = handler;
            handler.GameSpyServer = this;

            _serverReport = new UdpPortHandler(27900, OnServerReportReceived, OnError);
            _serverRetrieve = new TcpPortHandler(28910, new RetrieveTcpSetting(), OnServerRetrieveReceived, OnServerRetrieveError);
            
            _clientManager = new TcpPortHandler(29900, new LoginTcpSetting(), OnClientManagerReceived, OnError, OnClientAccept, KillNode);
            _searchManager = new TcpPortHandler(29901, new LoginTcpSetting(), OnSearchManagerReceived, OnError, null, KillNode);

            _chat = new TcpPortHandler(6667, new ChatTcpSetting(), OnChatReceived, OnError, OnChatAccept, RestartServices);
            _stats = new TcpPortHandler(29920, new StatsTcpSetting(), OnStatsReceived, OnError, OnStatsAccept, RestartServices);
            _http = new TcpPortHandler(80, new HttpTcpSetting(), OnHttpReceived, OnError, null, KillNode);

            LogTrace("Services inited");

        }

        #region LOG_METHODS
        void Log(string message)
        {
            _emulationAdapter.LogInfo(message);
        }

        void LogWarn(string message)
        {
            _emulationAdapter.LogWarn(message);
        }

        void LogTrace(string message)
        {
            _emulationAdapter.LogTrace(message);
        }

        void LogForUser(string message)
        {
            _emulationAdapter.LogForUser(message);
        }

        void Log(Exception ex)
        {
            _emulationAdapter.LogException(ex);
        }
        #endregion

        void OnServerRetrieveError(Exception exception, bool send, int port)
        {
            // Получение ошибки при запросе списка хостов периодически получает сброс соединения,
            // Но это поведение должно игнорироваться для этого запроса. Все остальные связи должны работать нормально
            // Log(exception);
        }

        void KillNode(TcpPortHandler handler, TcpClientNode node)
        {
            // Получение 0 байт говорит о том, что клиент хочет прекратить взаимодействие.
            handler.KillClient(node);
        }

        void RestartServices(TcpPortHandler handler, TcpClientNode node)
        {
            // Получение 0 байт говорит о том, что клиент хочет прекратить взаимодействие.
            // Сбрасываем все соединения
            Restart();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Start()
        {
            _serverReport.Start();
            _serverRetrieve.Start();

            _clientManager.Start();
            _searchManager.Start();

            _chat.Start();
            _stats.Start();
            _http.Start();

            LogTrace("Services started");
        }

        void OnError(Exception exception, bool send, int port)
        {
            // Любая ошибка сбрасывает все соединения
            Log(exception);
            Restart();
        }

        void Restart()
        {
            LogForUser($"GameSpy emulator restart");
            Stop();
            _emulationAdapter.LeaveFromCurrentLobby();
            Start();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Stop()
        {
            _challengeResponded = false;
            _inChat = false;
            _enteredLobbyHash = null;
            _localServerHash = null;

            _serverReport.Stop();
            _serverRetrieve.Stop();

            _clientManager.Stop();
            _searchManager.Stop();

            _chat.Stop();
            _stats.Stop();
            _http.Stop();
            StopTimer();

            LogTrace("Services stopped");
        }
    }
}
