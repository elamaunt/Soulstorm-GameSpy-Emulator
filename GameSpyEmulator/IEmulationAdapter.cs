using System;
using System.Collections.Generic;
using System.Net;
using static GameSpyEmulator.GameSpyServer;

namespace GameSpyEmulator
{
    public interface IEmulationAdapter
    {
        /// <summary>
        /// Сюда задается активный сервер, куда был отправлен этот адаптер.
        /// Необходимо сохранить ссылку для дальнейшего использования при реализации методов интерфейса <see cref="IEmulationAdapter"/>.
        /// </summary>
        GameSpyServer GameSpyServer { set; }

        /// <summary>
        /// Проброс метода логгирования
        /// </summary>
        void LogInfo(string message);

        /// <summary>
        /// Проброс метода логгирования
        /// </summary>
        void LogException(Exception ex);

        /// <summary>
        /// Дополнительный метод логгирования, для показа пользователю
        /// </summary>
        void LogForUser(string message);

        /// <summary>
        /// Проброс метода логгирования
        /// </summary>
        void LogTrace(string message);

        /// <summary>
        /// Проброс метода логгирования
        /// </summary>
        void LogWarn(string message);

        /// <summary>
        /// Имя локального пользователя, авторизованное в игре. Не должно быть null.
        /// Должно быть присвоено сразу после успешного входа
        /// </summary>
        string LocalUserName { get; }

        /// <summary>
        /// Должно быть true, если текущий юзер захостил игру в авто
        /// </summary>
        bool HasHostedLobby { get; }

        /// <summary>
        /// Должен возвращать количество игроков, активных на сервере
        /// </summary>
        int ActivePlayersCount { get; }

        /// <summary>
        /// Должно быть true, когда текущий юзер находится в скрытой чат комнате. Актуально только для авто
        /// </summary>
        bool IsInLobbyNow { get; }

        /// <summary>
        /// Должно быть true, когда текущий юзер имеет активный профиль в игре (создал аккаунт и вошел в него один раз)
        /// </summary>
        bool HasLocalUserActiveInGameProfile { get; }

        /// <summary>
        /// Вызывается, когда пришло сообщение о старте игры. Актуально только для авто
        /// </summary>
        void OnRemoteUserHasLauncherGame();

        /// <summary>
        /// Необходимо оповестить всех в лобби о том, что игрок ливнул через 
        /// <see cref="GameSpyServer.SendLobbyMemberLeft(string, long)"/> ;
        /// </summary>
        void LeaveFromCurrentLobby();

        /// <summary>
        /// Создает публичное лобби, которое может быть найдено через поиск игр.
        /// Не должно быть видно другим юзерам, пока не будет вызвана функция <see cref="UpdateLocalLobbyDetails(GameServerDetails)"/>
        /// И параметр сервера <see cref="GameServerDetails.IsValid"/> не вернет значение true.
        /// Необходимо создать для нового хоста уникальный идентификатор, который потом будет засетан в свойство <see cref="GameServerDetails.HostId"/>.
        /// </summary>
        /// <param name="name">Ник юзера</param>
        void CreateLobby(string name);

        /// <summary>
        /// Вызывается, когда игра запросила удаление лобби, созданное через <see cref="CreateLobby(string)"/>
        /// Также этот метод вызовется при старте игры.
        /// </summary>
        void OnLocalLobbyClearedByGame();

        /// <summary>
        /// Вызывается, когда игра присылает данные об игре и флаги <see cref="GameServerDetails.IsValid"/> и <see cref="GameServerDetails.Ranked"/> имеют значение true
        /// </summary>
        void OnAutomatchLobbyValidated();

        /// <summary>
        /// Возвращает необходимость установить флаг <see cref="GameServerDetails.LobbyLimited"/> при получении данных о хосте от игры.
        /// Данный флаг может быть использован для исключения лобби при большом отрыве по рейтингу. Его стоит считывать при выдаче списка лобби самостоятельно.
        /// </summary>
        bool ShouldLimitLocalLobbyByRating();

        /// <summary>
        /// Вызывается при получении информации о созданном хосте от игры. Вызывается один раз при создании лобби. Потом еще раз для кастомок через несколько секунд или через 60 секунд для автоматча.
        /// Затем вызов повторяется примерно каждые 60 секунд. Когда игра закрывает хост, то будет вызван <seealso cref="OnLocalLobbyClearedByGame"/>
        /// </summary>
        void UpdateLocalLobbyDetails(GameServerDetails details);

        /// <summary>
        /// Должен вернуть список открытых лобби для подключения в игре. Используется и для кастомок и для автоматча.
        /// Игра сама отфильтрует игры авто/не авто.
        /// Но мы должны самостоятельно отфильтровать игры по активному моду в игре, иначе игра забагуется.
        /// Также каждый хост должен иметь уникальный идентификатор сервера <see cref="GameServerDetails.HostId"/>.
        /// </summary>
        GameServerDetails[] GetOpenedLobbies();

        /// <summary>
        /// Возвращает уникальный номер профиля по нику в игре
        /// </summary>
        /// <param name="nick">Зарегистированный профиль игрока</param>
        long GetUserInGameProfileId(string nick);

        /// <summary>
        /// Вызывается при попытке игрока пойти в автоматч лобби.
        /// Идентифицировать лобби нужно по уникальному идентификатору, который был получен из свойства <see cref="GameServerDetails.HostId"/>.
        /// Задать эти значения необходимо самостоятельно.
        /// </summary>
        /// <param name="hostId">Уникальный идентификатор хоста</param>
        /// <param name="name">Ник юзера</param>
        /// <param name="successHalder">Необходимо вызвать, чтобы отправить игре успех входа в лобби</param>
        /// <param name="errorHandler">Необходимо вызвать, чтобы отправить игре неудачу входа в лобби</param>
        void TryEnterInLobby(string hostId, string name, EnterInLobbySuccessDelegate successHalder, EnterInLobbyFailedDelegate errorHandler);

        /// <summary>
        /// Необходимо вернуть список игроков, который сейчас находятся в главном чате игры.
        /// Эти ники игроков должны быть зарегистрированы и иметь уникальные внутриигровые long идентификаторы, которые будут запрашиваться через <seealso cref="GetUserInGameProfileId(string)"/>
        /// </summary>
        string[] GetUsersInMainChat();

        /// <summary>
        /// Возвращает ограничение на количество пользователей к указанной комнате. Актуально для автоподбора
        /// </summary>
        int GetCurrentLobbyMaxPlayers();

        /// <summary>
        /// Задает ограничение на количество пользователей к указанной комнате. Актуально для автоподбора.
        /// Вызывается, если текущий юзер захостил игру в авто. Сервер должен учитывать это значение при попытке входа в хост.
        /// </summary>
        void SetLocalLobbyMaxPlayers(int max);

        /// <summary>
        /// Возвращает строку значение по запросу от игры. Игра соответственно сперва вызывает <see cref="SetLobbyKeyValue(string, string)(string[])"/>, чтоб задать значения. 
        /// Эти значения должны преедаваться между всеми игроками в комнате. Для главного чата это метод <see cref="GameSpyServer.SendUserKeyValueChanged(string, string, string)"/>.
        /// </summary>
        string GetLobbyKeyValue(string key);

        /// <summary>
        /// Возвращает юзеров, находящихся в скрытой комнате. Вызывается сразу после того, как пользовател пошел в комнату.
        /// Должно содержать самого пользователя, который делает запрос.
        /// Актуально для автоматча.
        /// </summary>
        string[] GetLobbyMembers();

        /// <summary>
        /// Возвращает строку значение указанного пользователя. Игра соответственно сперва вызывает <see cref="SetLobbyKeyValue(string, string)"/>, чтоб задать значения. 
        /// Эти значения должны передаваться между всеми игроками в комнате. Для лобби чата это метод <see cref=""/>.
        /// </summary>
        string GetLobbyMemberData(string name, string key);

        /// <summary>
        /// Должен вернуть уникальный идентификатор для локально созданного хоста. Предполагается, что этот ID был выделен при вызове <see cref="CreateLobby(string)"/>
        /// </summary>
        string GetLocalCreatedLobbyId();

        /// <summary>
        /// Метод для передачи IRC сообщений от других игроков в лобби. Актуально для авто.
        /// Работает в связке с методом <see cref="GameSpyServer.SendLobbyBroadcast(string, string)"/>. Этот метод сервера должен быть вызван для всех игроков в лобби, когда кто-то из игроков отправил Broadcast
        /// </summary>
        void SendLobbyBroadcast(string message);

        /// <summary>
        /// Вызывается при отправке сообщения в главный чат локальным пользователем. Здесь можно продублировать сообщение в другие чаты.
        /// Сообщение в игре появляется автоматически.
        /// </summary>
        void SendChatMessage(string message);

        /// <summary>
        /// Вызывается каждый раз, когда пользователь сохраняет значение в комнате лобби.
        /// Значение необходимо преедать всем другим пользователям, чтобы оно было доступно при вызове <see cref="GetLobbyKeyValue(string)"/>
        /// </summary>
        void SetLobbyKeyValue(string key, string value);

        /// <summary>
        /// Вызывается, когда игра задает глобальные параметры для локального пользователя, который только вошел в чат.
        /// Эти параметры должны быть переданы всем пользователям чата для получения через метод <see cref="GetUserGlobalKeyValue(string, string)"/>
        /// А также непосредственно после вызова все пользователи должны получить вызов <see cref="GameSpyServer.SendUserKeyValueChanged(string, string, string)"/> для каждой пары ключ-значение.
        /// Без правильного использования не будет открываться статистика пользователя в чате.
        /// </summary>
        void SetGlobalKeyValues(Dictionary<string, string> pairs);

        /// <summary>
        /// Вызывается 
        /// </summary>
        void OnLogout();

        /// <summary>
        /// Запрашивает попытку создать профиль с указанными параметрами.
        /// Результат необходимо отправить в <see cref="GameSpyServer.SendCreateProfileSuccess(string, long?, string)"/> в случае успеха или 
        /// в <see cref="GameSpyServer.SendCreateProfileFail"/> в случае неудачи.
        /// </summary>
        void TryCreateProfile(string nick, string email, string passwordHash);

        /// <summary>
        /// Игра запрашивает подтверждение авторизации. Необходимо сопоставить владение данным провилем с пользователем клиента и вызвать метод с данными регистрации <see cref="GameSpyServer.SendLoginResponce(LoginInfo)"/>.
        /// </summary>
        void RequestLogin(string name);

        /// <summary>
        /// Запрашивает все ники (профили) пользователя на основе его почты.
        /// Результат необходимо передать в метод <see cref="GameSpyServer.SendNicksOffer(string[])"/>
        /// </summary>
        void RequestAllUserNicks(string email);

        /// <summary>
        /// Запрос проверки занятости ника. Результат необходимо передать в метод <see cref="GameSpyServer.SendNameCheckSuccess(string, long)"/> в случае успеха или
        /// в <see cref="GameSpyServer.SendNameCheckFail(string)"/> в случае неудачи
        /// </summary>
        void RequestNameCheck(string name);

        /// <summary>
        /// Запрашивает статистику пользователя по указанному ID профиля. Желательно, чтобы метод отрабатывал быстро
        /// </summary>
        IStatsInfo GetUserStatsInfo(long profileId);

        /// <summary>
        /// Запрашивает статистику пользователя по указанному имени профиля. Желательно, чтобы метод отрабатывал быстро
        /// </summary>
        IStatsInfo GetUserStatsInfo(string name);

        /// <summary>
        /// Вызывается после завершения матча, когда игра получила отправила информацию. 
        /// Может быть вызвано несколько раз для одного матча разными игроками.
        /// Последний вызов будет содержать наиболее полные данные, но любой вызов будет содержать данные о том, как он завершился.
        /// </summary>
        void SendGameFinishedData(GameFinishedData data);

        /// <summary>
        /// Игра запрашивает установку заголовка для созданной комнаты автоматча.
        /// Обычно это ник игрока-хоста.
        /// </summary>
        void SetLobbyTopic(string topic);

        /// <summary>
        /// Запрашивает статистику авторизованного локального пользователя.
        /// </summary>
        IStatsInfo GetLocalPlayerStats();

        /// <summary>
        /// Запрашивает глобальное значение пользователя по нику и ключу. Предварительно эти данные заполняются после входа в чат методом <see cref="SetGlobalKeyValues(string[])"/>.
        /// Если значения еще нет - можно отправить <see langword="null"/>.
        /// </summary>
        string GetUserGlobalKeyValue(string user, string key);

        /// <summary>
        /// Запрашивается наличие у указанного профиля идентификатора профиля. Другими словами указанный ник должен был быть зарегистрирован через систему GameSpy.
        /// Если этого не было, то ник не должен появляться в интерфейсе игры (список игроков).
        /// </summary>
        bool IsUserHasActiveProfile(string user);

        /// <summary>
        /// Вызывается, когда лобби, созданное локальным игроков отправляет Ping. Этот вызов повторяется каждые 20 секунд.
        /// Если вызовы прекратились, то хост можно удалять.
        /// Подходит для реализации удаления мертвых хостов
        /// </summary>
        void PingFromLocalLobby(IPEndPoint hostAddress);
    }
}
