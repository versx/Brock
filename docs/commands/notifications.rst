*************
Notification Commands
*************

Commands to setup notifications if a certain Pokemon spawns or a raid boss takes over a gym. 

**Notes:**

- The 'all' and 'level' parameters are only available to Supporters but everyone is able to use the IV parameter.
- Normal members have a maximum of 25 different Pokemon subscriptions.
- **Everyone** is limited to 50 DM notifications from Brock a minute.
- Supporters have unlimited different Pokemon and Raid subscriptions (Complete pokdex and raid boss list).
- Brock rounds the IVs for the Pokemon so if it's 95.6% and you specified 95.6% you'll still get the notification.
- Please keep your IV values set to decent values. Using 0% IV is ok for rare Pokemon but not common types such as Mankey, Pikachu, Pidgey (eek), etc.
- You are only sent DM notifications of Pokemon spawns for city feed zones that you're assigned to, others will be ignored.


**.pokeme**  

:Usage: ``.pokeme <pokemon(s),all> <IV> <Level>``  
:Description: Subscribe to Pokemon notifications based on the pokedex number or name, minimum IV stats, or minimum level.  
:Examples:  
|   ``.pokeme 147 95``  
|   ``.pokeme pikachu 97``  
|   ``.pokeme 113,242,248 90``  
|   ``.pokeme pikachu,26,129,Dratini 97``  
|   ``.pokeme 113 90 L35`` (Supporters Only: Subscribe to Chansey notifications with minimum IV of 90% and minimum level of 32.)  
|   ``.pokeme all 90`` (Subscribe to all Pokemon notifications with minimum IV of 90%. Excludes Unown)  
|   ``.pokeme all 90 L30`` (Supporters Only: Subscribe to all Pokemon notifications with minimum IV of 90% and minimum level of 30.)  



**.pokemenot**  

:Usage: ``.pokemenot <pokemon(s),all>``  
:Description: Unsubscribe from a one or more or even all subscribed Pokemon notifications by pokedex number or name.  
:Examples:  
|   ``.pokemenot 149``
|   ``.pokemenot pikachu``  
|   ``.pokemenot 3,6,9,147,148,149``  
|   ``.pokemenot bulbasuar,7,tyran``  
|   ``.pokemenot all`` (Removes all subscribed Pokemon notifications.)  



**.raidme**  

:Usage: ``.raidme <pokemon(s),all>``  
:Description: Subscribe to raid boss Pokemon notifications.  
:Examples:  
|   ``.raidme Absol`` (Subscribe to Absol raid notifications.) 
|   ``.raidme Tyranitar,Magikarp`` (Subscribe to Tyranitar and Magikarp raid notifications.) 
|   ``.raidme all`` (Subscribe to all raid boss notifications.) 



**.raidmenot**  

:Usage: ``.raidmenot <pokemon(s),all>``  
:Description: Unsubscribe from a one or more or even all subscribed Raid notifications.  
:Examples:  
|   ``.raidmenot Absol``  
|   ``.raidmenot Tyranitar,Snorlax``  
|   ``.raidmenot all`` (Removes all subscribed Raid notifications.)  



**.enable**  

:Usage: ``.enable``  
:Description: Enables all of your Pokemon and Raid notification subscriptions at once.  
:Example: ``.enable``  



**.disable**  

:Usage: ``.disable``  
:Description: Disables all of your current Pokemon and Raid boss notification subscriptions at once.  
:Example: ``.disable``  



**.info**  

:Usage: ``.info``  
:Description: Shows your current Pokemon and Raid boss notification subscriptions.  
:Example: ``.info``  



**.demo**  

:Usage: ``.demo``  
:Description: Displays a demos regarding how to use Brock.  
:Example: ``.demo``  