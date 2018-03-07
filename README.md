# BrockBot

### Setup
Grant Brock the following Discord permissions:

**General Permissions:**

- Manage Roles (Required for Team Assignment)
- Manage Channels (Required for Raid Lobbies)

**Text Permissions:**

- Send Messages (Required)
- Manage Messages (Required for Raid Lobbies and Advertisement Posting)
- Read Message History (Required)

### Documentation
Documenation can be found at <http://brock.rtfd.io>  

**For a full list of available commands type `.help` in a direct message to Brock or visit <http://brock.rtfd.io>.**

### Available Text Replacement Variables

**Welcome Message Variables:**
- {server}: Returns the current guild's name.
- {username}: Returns the user that's receiving the welcome message's name.
- {mention}: Returns the user that's receiving the welcome message's mention tag.
- {users}: Returns the total amount of users in the current guild.

**Advertisement Message Variables:**
- {server}: Returns the current guild's name.
- {bot}: Returns the bot command channel's mention tag.

### Config Example
```json
{
  "ownerId": 0000000000,
  "adminCommandsChannelId": 000000000,
  "commandsChannelId": 00000000000,
  "commandsPrefix": ".",
  "sponsoredRaids": {
    "channelPool": [
      00000000000,
      00000000001,
	  00000000002
    ],
    "keywords": [
		"Sprint",
		"Starbucks",
		"McDonalds",
		"Boost"
	],
    "webHook": "<SPONSORED_RAIDS_DISCORD_CHANNEL_WEBHOOK_ADDRESS>"
  },
  "allowTeamAssignment": true,
  "teamRoles": [
    "Valor",
    "Mystic",
    "Instinct"
  ],
  "cityRoles": [
    "Upland",
    "Ontario",
    "Pomona",
    "EastLA",
    "Raids",
    "Families",
    "Nests",
    "LongBeach",
    "SantaMonica",
    "Newport",
    "Disneyland",
    "UniversalStudios"
  ],
  "authToken": "<DISCORD_BOT_AUTH_TOKEN>",
  "sendStartupMessage": true,
  "startupMessages": [
    "Whoa, whoa...alright I'm awake.",
    "No need to push, I'm going...",
    "That was a weird dream, wait a minute...",
    "Circuits fully charged, let's do this!",
    "What is this place? How did I get here?",
    "Looks like we're not in Kansas anymore...",
    "Hey...watch where you put those mittens!"
  ],
  "startupMessageWebHook": "<STARTUP_MESSAGE_DISCORD_CHANNEL_WEBHOOK_ADDRESS>",
  "sendWelcomeMessage": true,
  "welcomeMessage": "Hello {username}, welcome to **{server}**'s server!\r\nMy name is Brock and I'm here to help you with certain things if you require them such as notifications of Pokemon that have spawned as well as setting up Raid Lobbies or even assigning yourself to a team or city role. To see a full list of my available commands please send me a direct message containing `.help`.",
  "notifyMemberJoined": true,
  "notifyMemberLeft": true,
  "notifyMemberBanned": true,
  "notifyMemberUnbanned": true,
  "twitterUpdates": {
    "consumerKey": "<TWITTER_CONSUMER_KEY>",
    "consumerSecret": "<TWITTER_CONSUMER_SECRET>",
    "accessToken": "<TWITTER_ACCESS_TOKEN>",
    "accessTokenSecret": "<TWITTER_ACCESS_TOKEN_SECRET>",
    "postTwitterUpdates": true,
    "users": [
      2839430431
    ],
    "updatesChannelWebHook": "<TWITTER_UPDATES_DISCORD_CHANNEL_WEBHOOK_ADDRESS>"
  },
  "advertisement": {
    "enabled": true,
    "lastMessageId": 00000000000,
    "postIntervalMinutes": 5,
    "message": ":arrows_counterclockwise: Welcome to **{server}**'s server, to assign yourself to a city feed or team please review the pinned messages in the {bot} channel.",
    "channelId": 0000000000000,
	"messageThreshold": 8
  },
  "nearbyNests": {
	"Ruben S. Ayala Park": 77,
	"Upland Memorial Park": 258,
	"John Galvin Park": 124,
	"Homer F. Briggs Park": 10,
	"Cabrillo Park": 27,
	"Red Hill Community Park": 86,
	"Upland Hills Country Club": 140
  },
  "customCommands": {
    "lolcat": "https://i.giphy.com/media/PUBxelwT57jsQ/200.gif"
  }
}

```