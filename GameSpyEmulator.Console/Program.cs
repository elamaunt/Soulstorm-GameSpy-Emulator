using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using static GameSpyEmulator.GameSpyServer;

namespace GameSpyEmulator
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new GameSpyServer(new ConsoleTestAdapter());

            server.Start();

            Console.WriteLine("Local GameSpy server in work now");

            while (true)
                Thread.Sleep(1000);
        }

        class ConsoleTestAdapter : IEmulationAdapter
        {
            GameSpyServer _server;

            // Данные после входа в глобальный чат
            readonly Dictionary<string, string> _globalKeyValues = new Dictionary<string, string>();

            // Данные после создания лобби
            Guid? _lobbyHostId;

            // Данные после обновления лобби
            GameServerDetails _localLobbyDetails;
           
            // Данные после регистрации
            string _name;
            string _passwordHash;
            string _email;
            long _profileId;

            public GameSpyServer GameSpyServer { set => _server = value; }

            public string LocalUserName => _name;

            public bool HasHostedLobby => _lobbyHostId != null;

            public int ActivePlayersCount => 1;

            public bool IsInLobbyNow => true;

            public bool HasLocalUserActiveInGameProfile => true;

            public void CreateLobby(string name)
            {
                _lobbyHostId = Guid.NewGuid();
                _server.SendServerPrivateMessageToChat("You lobby was created");
            }

            public void TryEnterInLobby(string hostId, string name, EnterInLobbySuccessDelegate successHandler, EnterInLobbyFailedDelegate errorHandler)
            {
                successHandler("TestHost", new string[] { "TestHost", name });

                _server.SendServerPrivateMessageToChat("You entered in lobby");
            }

            public string[] GetUsersInMainChat()
            {
                return new[] { "Vasya", "Petya" };
            }

            public int GetCurrentLobbyMaxPlayers()
            {
                return 2;
            }

            public long GetUserInGameProfileId(string nick)
            {
                return _profileId;
            }

            public string GetLobbyKeyValue(string key)
            {
                return null;
            }

            public IStatsInfo GetLocalPlayerStats()
            {
                return new CustomStatsInfo();
            }

            public GameServerDetails[] GetOpenedLobbies()
            {
                _server.SendServerPrivateMessageToChat("Lobbies requested");
                return new GameServerDetails[0];
            }

            public IStatsInfo GetUserStatsInfo(string name)
            {
                return new CustomStatsInfo();
            }

            public void LeaveFromCurrentLobby()
            {
                _lobbyHostId = null;
            }

            public void OnAutomatchLobbyValidated()
            {
                _server.SendServerPrivateMessageToChat("Automatch Lobby Validated");
            }

            public void OnLocalLobbyClearedByGame()
            {
                _server.SendServerPrivateMessageToChat("Lobby was cleared");
                _lobbyHostId = null;
            }

            public void OnLogout()
            {
            }

            public void OnRemoteUserHasLauncherGame()
            {
                // Activate game process
            }

            public void RequestAllUserNicks(string email)
            {
                _server.SendNicksOffer(new[] { "Test1", "Test2", "Test3" });
            }

            public void RequestLogin(string name)
            {
                _server.SendLoginResponce(new LoginInfo()
                { 
                    Name = name,
                    Email = _email, // Send stored email
                    ProfileId = _profileId, // Send stored profile id
                    PasswordHash = _passwordHash // Send stored password hash
                });
            }

            public void RequestNameCheck(string name)
            {
                _server.SendNameCheckSuccess(name, 1);
            }

            public void TryCreateProfile(string nick, string email, string passwordHash)
            {
                // Save nick
                _name = nick;

                // Save password for new profile ID
                _passwordHash = passwordHash;

                // Save email for profile verification
                _email = email;

                // Generate id for profile. Must be more than 0
                _profileId = 1;

                _server.SendCreateProfileSuccess(_profileId);
            }

            public void SendChatMessage(string message)
            {
                // Send in another chats
            }

            public void SendGameFinishedData(GameFinishedData gameFinishedMessage)
            {
                // Update statistics
            }

            public void SendLobbyBroadcast(string message)
            {
                _server.SendLobbyBroadcast(LocalUserName, message);
            }

            public void SetLobbyKeyValue(string key, string value)
            {
                _server.SendServerPrivateMessageToChat($"Lobby key-value updated: {key}={value}");
            }

            public void SetLobbyTopic(string topic)
            {
                _server.SendServerPrivateMessageToChat($"Lobby topic set: {topic}");
            }

            public void SetLocalLobbyMaxPlayers(int maxPlayers)
            {
                _server.SendServerPrivateMessageToChat($"Lobby max players set: {maxPlayers}");
            }

            public bool ShouldLimitLocalLobbyByRating()
            {
                return false;
            }

            public void UpdateLocalLobbyDetails(GameServerDetails details)
            {
                _server.SendServerPrivateMessageToChat($"Lobby data updated. Hostname: {details.HostName}. HostAddress: {details.HostIP ?? details.LocalIP}:{details.HostPort ?? details.LocalPort}. Mod: {details.GameVariant} {details.GameVer}");
                _localLobbyDetails = details;
            }

            public IStatsInfo GetUserStatsInfo(long profileId)
            {
                return new CustomStatsInfo();
            }

            public string GetUserGlobalKeyValue(string user, string key)
            {
                return _globalKeyValues.GetValueOrDefault(key);
            }

            string[] IEmulationAdapter.GetLobbyMembers()
            {
                return new string[] { LocalUserName };
            }

            public string GetLobbyMemberData(string name, string key)
            {
                return null;
            }

            public bool IsUserHasActiveProfile(string user)
            {
                return true;
            }

            public string GetLocalCreatedLobbyId()
            {
                return _lobbyHostId.ToString();
            }

            public void SetGlobalKeyValues(Dictionary<string, string> pairs)
            {
                foreach (var p in pairs)
                {
                    _globalKeyValues[p.Key] = p.Value;

                    // Передача юзерам
                    _server.SendUserKeyValueChanged(LocalUserName, p.Key, p.Value);
                }
            }

            public void PingFromLocalLobby(IPEndPoint hostAddress)
            {
                // Update local lobby ping here
            }

            /////////////////////// LOGGING //////////////////////
            public void LogForUser(string message)
            {
                Console.WriteLine(message);
            }

            public void LogInfo(string message)
            {
                Console.WriteLine(message);
            }

            public void LogTrace(string message)
            {
                Console.WriteLine(message);
            }

            public void LogWarn(string message)
            {
                Console.WriteLine(message);
            }

            public void LogException(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
