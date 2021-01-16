using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GameSpyEmulator
{
    public class GameServerDetails
    {
        readonly Dictionary<string, string> _values;

        /// <summary>
        /// Уникальный идентификатор хоста. Должен быть выдан сервером. Пользовательское свойство.
        /// </summary>
        public string HostId { get; set; }

        /// <summary>
        /// Уникальный хэш относительно локального пользователя. Строится на основе адреса и порта этого хоста.
        /// По хэшу можно установить сопоставление хоста и комнаты автоматча
        /// </summary>
        public string RoomHash { get; set; }

        public GameServerDetails(ReadOnlyDictionary<string, string> properties)
        {
            _values = new Dictionary<string, string>(properties);
        }

        public GameServerDetails()
        {
            _values = new Dictionary<string, string>();
        }

        public Dictionary<string, string> Properties => _values;

        public void Set(string key, string value) => _values[key] = value;
       
        public string this[string key] { get => GetOrDefault(key); set => _values[key] = value; }

        public bool TryGetValue(string key, out string value) => _values.TryGetValue(key, out value);

        public bool Ranked => GameName?.EndsWith("am", StringComparison.Ordinal) ?? false;

        public bool DevMode => GetOrDefault("devmode") != "0";
        public bool IsTeamplay => GetOrDefault("teamplay") == "1";
        public bool HasPlayers => Int32.Parse(GetOrDefault("numplayers")) > 0;
        public string PlayersCount => GetOrDefault("numplayers");

        public string HostIP => GetOrDefault("localip0");
        public string LocalIP => GetOrDefault("localip1");
        public string HostPort => GetOrDefault("hostport");
        public string LocalPort => GetOrDefault("localport");
        
        public string HostName => GetOrDefault("hostname");
        public string StateChanged => GetOrDefault("statechanged");
        public string MaxPlayers => GetOrDefault("maxplayers");
        public string GameVer => GetOrDefault("gamever");
        public string GameName => GetOrDefault("gamename") ?? GetOrDefault("GameName");
        public string GameType => GetOrDefault("gametype");
        public string GameVariant => GetOrDefault("gamevariant");

        /// <summary>
        /// Рейтинг хоста при создании автоматча
        /// </summary>
        public int Score => GetOrDefault("score_").ParseToIntOrDefault();

        public bool LobbyLimited
        {
            get => GetOrDefault("limitedByRating") == "1";
            set => this["limitedByRating"] = value ? "1" : "0";
        }

        public string GetOrDefault(string key)
        {
            _values.TryGetValue(key, out string value);
            return value;
        }

        public bool IsValid => !String.IsNullOrWhiteSpace(HostName) &&
                !String.IsNullOrWhiteSpace(GameVariant) &&
                !String.IsNullOrWhiteSpace(GameVer) &&
                !String.IsNullOrWhiteSpace(GameType) &&
                MaxPlayers != "0";
    }
}
