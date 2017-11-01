namespace PokeFilterBot
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /** Usage
     * .subs - List all current subscriptions.
     * .sub <pokemon_name> - Subscribes to notifications of messages containing the specified keyword.
     * .unsub - Unsubscribe from all notifications.
     * .unsub <pokemon_name> - Unsubscribes from notifications of messages containing the specified keyword.
     * 
     */

    class MainClass
	{
		public static void Main(string[] args) => MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();

		static async Task MainAsync(string[] args)
		{
            var bot = new FilterBot();
            await bot.Start();

            Console.Read();
            await bot.Stop();
		}
	}

    //public class DiscordCommand
    //{
    //    public string Prefix { get; private set; }

    //    public string Command { get; private set; }

    //    public List<string> Arguments { get; private set; }

    //    public DiscordCommand()
    //    {
    //    }

    //    public DiscordCommand(string prefix, string command, List<string> arguments)
    //    {
    //        Prefix = prefix;
    //        Command = command;
    //        Arguments = arguments;
    //    }

    //    public static DiscordCommand ParseCommand(string line)
    //    {
    //        var lines = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    //        var cmd = new DiscordCommand()
    //        {
    //            Prefix = line[0].ToString(),
    //            Command = lines[0]
    //        };
    //        var list = new List<string>();
    //        foreach (var item in lines)
    //        {
    //            if (lines[0] == item) continue;
    //            list.Add(item);
    //        }
    //        cmd.Arguments = list;

    //        return cmd;
    //    }
    //}
}

/*
    _defaults = {
        'pokemon': {
            'username': "<pkmn>",
            'content':"",
            'icon_url': "https://raw.githubusercontent.com/kvangent/PokeAlarm/master/icons/<pkmn_id>.png",
            'avatar_url': "https://raw.githubusercontent.com/kvangent/PokeAlarm/master/icons/<pkmn_id>.png",
            'title': "A wild <pkmn> has appeared!",
            'url': "<gmaps>",
            'body': "Available until <24h_time> (<time_left>)."
        }

    # Set the appropriate settings for each alert
    def create_alert_settings(self, settings, default):
        alert = {
            'webhook_url': settings.pop('webhook_url', self.__webhook_url),
            'username': settings.pop('username', default['username']),
            'avatar_url': settings.pop('avatar_url', default['avatar_url']),
            'disable_embed': parse_boolean(settings.pop('disable_embed', self.__disable_embed)),
            'content': settings.pop('content', default['content']),
            'icon_url': settings.pop('icon_url', default['icon_url']),
            'title': settings.pop('title', default['title']),
            'url': settings.pop('url', default['url']),
            'body': settings.pop('body', default['body']),
            'map': get_static_map_url(settings.pop('map', self.__map), self.__static_map_key)
        }

        reject_leftover_parameters(settings, "'Alert level in Discord alarm.")
        return alert

    # Send Alert to Discord
    def send_alert(self, alert, info):
        log.debug("Attempting to send notification to Discord.")
        payload = {
            'username': replace(alert['username'], info)[:32],  # Username must be 32 characters or less
            'content': replace(alert['content'], info),
            'avatar_url':  replace(alert['avatar_url'], info),
        }
        if alert['disable_embed'] is False:
            payload['embeds'] = [{
                'title': replace(alert['title'], info),
                'url': replace(alert['url'], info),
                'description': replace(alert['body'], info),
                'thumbnail': {'url': replace(alert['icon_url'], info)}
            }]
            if alert['map'] is not None:
                payload['embeds'][0]['image'] = {'url': replace(alert['map'], {'lat': info['lat'], 'lng': info['lng']})}
        args = {
            'url': alert['webhook_url'],
            'payload': payload
        }
        try_sending(log, self.connect, "Discord", self.send_webhook, args, self.__max_attempts) 
*/
