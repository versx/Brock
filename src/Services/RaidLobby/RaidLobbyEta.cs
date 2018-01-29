namespace BrockBot.Services.RaidLobby
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RaidLobbyEta
    {
        NotSet = 0,
        Here,
        One,
        Two,
        Three,
        Four,
        Five,
        Ten,
        Fifteen,
        Twenty,
    }
}