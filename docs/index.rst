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

### __Commands List Needs To Be Updated__
### Usage
**.team** - Assign yourself to a team role, available teams to join are the following: Valor, Mystic, or Instinct. You can only join one team at a time, type `.iam None` to leave a team.  
	Example: `.team Valor`  
	Example: `.team None`  

**.info** - Shows the your current Pokemon subscriptions and Raid sbuscriptions.  
   Example: `.info`  
  
**.pokeme** - Subscribe to Pokemon notifications via pokedex number.  
   Example: `.pokeme 147 0 96` (Subscribe to Dratini notifications at any CP level with a minimum IV of 96%)  
   Example: `.pokeme 147` (Subscribes to Dratini notifications and uses 0 for the minimum CP and minimum IV values, any Dratini spawns will trigger the notification.)  
   Example: `.pokeme 113,242,248` (Subscribe to the following Pokemon notifications with 0 as the minimum CP and minimum IV values.)  
   
**.pokemenot** - Unsubscribe from a single or multiple Pokemon notifications or even all subscribed Pokemon notifications.  
   Example: `.pokemenot 149`  
   Example: `.pokemenot 3,6,9,147,148,149`  
   Example: `.pokemenot` (Removes all subscribed Pokemon notifications.)  
   
**.raidme** - Subscribe to Pokemon notifications via pokedex number.  
   Example: `.raidme 147 0 96` (Subscribe to Dratini notifications at any CP level with a minimum IV of 96%)  
   Example: `.raidme 147` (Subscribes to Dratini notifications and uses 0 for the minimum CP and minimum IV values, any Dratini spawns will trigger the notification.)  
   Example: `.raidme 113,242,248` (Subscribe to the following Pokemon notifications with 0 as the minimum CP and minimum IV values.)  
   
**.raidmenot** - Unsubscribe from a single or multiple Raid notifications or even all subscribed Raid notifications.  
   Example: `.raidmenot Magikarp`  
   Example: `.raidmenot Absol,Tyranitar`  
   Example: `.raidmenot` (Removes all subscribed Raid notifications.)  
  
**.feedme** - Assign yourself to a specific city feed's role.  
   Example: `.feedme Upland`  
   Example: `.feedme Upland,Ontario,Pomona,Families,Raids,Nests`  
   Example: `.feedme all` (Assigns all available city feed roles to yourself.)  
   
**.feedmenot** - Unassign yourself from a specific city feed's role.  
   Example: `.feedmenot Upland` (Unassign yourself from one specific city feed's role.)  
   Example: `.feedmenot Upland,Ontario,Pomona,Families,Raids,Nests` (Unassign yourself from the specific city feed roles.)  
   Example: `.feedmenot all` (Unassign yourself from all assigned city feed roles.)  
   
**.enable** - Activates the Pokemon and Raid notification subscriptions.  
   Example: `.enable` (Enables all of your Pokemon and Raid notification subscriptions at once.)  
   
**.disable** - Deactivates the Pokemon and Raid notification subscriptions.
   Example: `.disable` (Disables all of your Pokemon and Raid notification subscriptions at once.)  

**.demo** - Demos how to use and setup Brock for notifications.  

**.v**, **.ver**, or **.version** - Display the Brock's current version.  

**.help** - Shows this help message.  


**Raid Lobby System:**  

**.lobby** - Creates a new raid lobby channel.  
	Example: `.lobby Magikarp_4th 34234234234234`  
	
**.ontheway**, **.otw**, **.onmyway**, or **.omw** - Notifies people in the specified lobby that you are on the way with x amount of people and ETA.  
	Example: `.onmyway Magikarp_4th 5mins 3` (Registers that you have 3 people including yourself on the way.)  
	Example: `.otw Magikarp_4th 5mins` (Registers that you are by yourself on the way.)  
	
**.checkin** or **.here** - Checks you into the specified raid lobby notifying everyone that you have arrived to the raid location.  
	Example: `.checkin Magikarp_4th`  

**Custom Commands:**  
	
**Twitter Updates:**

**Administration:**

**.create\_roles** - Creates the required team roles to be assigned when users type the `.team <team>` commmand.  
**.delete\_roles** - Deletes all team roles that Brock created.


**For a fule list of available commands type `.help` in a direct message to Brock. I will update this more later will the full list.**

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
