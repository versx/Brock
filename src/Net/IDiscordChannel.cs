//namespace BrockBot.Net
//{
//    using System.Collections.Generic;
//    using System.Threading.Tasks;

//    public interface IDiscordChannel
//    {
//        ulong Id { get; }

//        string Name { get; }

//        string Mention { get; }

//        Task SendMessageAsync(string content = null, bool tts = false, IDiscordEmbed embed = null);

//        Task<IReadOnlyList<IDiscordMessage>> GetMessagesAsync();

//        Task<IDiscordMessage> GetMessageAsync(ulong messageId);
//    }

//    public interface IDiscordEmbed
//    {
//        ulong Id { get; }
//    }

//    public interface IDiscordMessage
//    {
//        ulong Id { get; }
//    }

//    public interface IDiscordGuild
//    {
//        ulong Id { get; }

//        IReadOnlyList<IDiscordChannel> Channels { get; }

//        Task<IDiscordMember> GetMemberAsync(ulong userId);
//    }

//    public interface IDiscordMember
//    {
//        ulong Id { get; }
//    }
//}