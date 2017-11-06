# PokeFilterBot

### Usage
**.iam** - Assign yourself to a team role, available teams to join are the following: Value, Mystic, or Instinct. You can only join one team at a time, type `.iam None` to leave a team.  
	Example: `.iam Valor`  
	Example: `.iam None`  

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

**.demo** - Display a demo of the PokeFilterBot.  
**.v**, **.ver**, or **.version** - Display the current PokeFilterBot version.  
**.help** - Shows this help message.  


**Raid Lobby System:**  

**.lobby** - Creates a new raid lobby channel.  
	Example: `.lobby Magikarp_4th 34234234234234`  
	
**.ontheway** - Notifies people in the specified lobby that you are on the way with x amount of people and ETA.  
	Example: `.ontheway Magikarp_4th 5mins 3` (Registers that you have 3 people including yourself on the way.  
	Example: `.ontheway Magikarp_4th 5mins` (Registers that you are by yourself on the way.  
	
**.checkin** - Checks you into the specified raid lobby notifying everyone that you have arrived to the raid location.  
	Example: `.checkin Magikarp_4th`  

If you are the owner of the bot you can execute the following additional commands:  
**.create\_roles** - Creates the required team roles to be assigned when users type the `.iam <team>` commmand.  
**.delete\_roles** - Deletes all team roles that the PokeFilterBot created.