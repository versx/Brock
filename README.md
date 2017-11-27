# BrockBot

### Setup
Discord Permissions

### Usage
**.team** - Assign yourself to a team role, available teams to join are the following: Valor, Mystic, or Instinct. You can only join one team at a time, type `.iam None` to leave a team.  
	Example: `.team Valor`  
	Example: `.team None`  

**.info** - Shows the your current Pokemon subscriptions and which channels to listen to.  

**.setup** - Include Pokemon from the specified channels to be notified of.  
   Example: `.setup channel1,channel2`  
   Example: `.setup channel1`  
   
**.remove** - Removes the selected channels from being notified of Pokemon.  
   Example: `.remove channel1,channel2`  
   Example: `.remove single_channel1`  
   
**.sub** - Subscribe to Pokemon notifications via pokedex number.  
   Example: `.sub 147`  
   Example: `.sub 113,242,248`  
   
**.unsub** - Unsubscribe from a single or multiple Pokemon notification or even all subscribed Pokemon notifications.  
   Example: `.unsub 149`  
   Example: `.unsub 3,6,9,147,148,149`  
   Example: `.unsub` (Removes all subscribed Pokemon notifications.)
   
**.enable** - Activates the Pokemon notification subscriptions.  
**.disable** - Deactivates the Pokemon notification subscriptions.

**.demo** - Demos how to use and setup Brock.  
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

If you are the owner of the bot you can execute the following additional commands:  
**.create\_roles** - Creates the required team roles to be assigned when users type the `.team <team>` commmand.  
**.delete\_roles** - Deletes all team roles that Brock created.


### Config Example
```json
{
  "ownerId": 000000000000000000,
  "commandsChannel": "bot",
  "commandsPrefix": ".",
  "sponsorRaidChannelPool": [
    375047782821111111,
    366049816180000000,
  ],
  "sponsorRaidKeywords": [
    "Sprint",
    "Starbucks",
    "McDonalds"
  ],
  "sponsorRaidsWebHook": "https://discordapp.com/api/webhooks/37483000000000000/jiujlkjljlkjljk",
  "allowTeamAssignment": true,
  "authToken": "lkjljlksjdfsfd",
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
  "startupMessageWebHook": "https://discordapp.com/api/webhooks/3743000000000/12132djlkjlkdsjlksdj",
  "sendWelcomeMessage": true,
  "welcomeMessage": "Hello {username}, welcome to versx's discord server!\r\nI am here to help you with certain things if you require them such as notifications of Pokemon that have spawned as well as setting up Raid Lobbies. To see a full list of my available commands please send me a direct message containing `.help`.",
  "notifyMemberJoined": true,
  "notifyMemberLeft": true,
  "notifyMemberBanned": true,
  "notifyMemberUnbanned": true,
}
```