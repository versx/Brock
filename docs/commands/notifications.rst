*************
Notification Commands
*************

Commands to setup notifications if a certain Pokemon spawns or a raid boss takes over a gym. 


**.pokeme**  

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

:Description: Unsubscribe from a one or more or even all subscribed Pokemon notifications by pokedex number or name.  
:Examples:  
|   ``.pokemenot 149``
|   ``.pokemenot pikachu``  
|   ``.pokemenot 3,6,9,147,148,149``  
|   ``.pokemenot bulbasuar,7,tyran``  
|   ``.pokemenot all`` (Removes all subscribed Pokemon notifications.)  



**.raidme**  

:Description: Subscribe to raid boss Pokemon notifications.  
:Examples:  
|   ``.raidme Absol`` (Subscribe to Absol raid notifications.) 
|   ``.raidme Tyranitar,Magikarp`` (Subscribe to Tyranitar and Magikarp raid notifications.) 
|   ``.raidme all`` (Subscribe to all raid boss notifications.) 



**.raidmenot**  

:Description: Unsubscribe from a one or more or even all subscribed Raid notifications.  
:Examples:  
|   ``.raidmenot Absol``  
|   ``.raidmenot Tyranitar,Snorlax``  
|   ``.raidmenot all`` (Removes all subscribed Raid notifications.)  



**.enable**  

:Description: Enables all of your Pokemon and Raid notification subscriptions at once.  
:Example: ``.enable``  



**.disable**  

:Description: Disables all of your current Pokemon and Raid boss notification subscriptions at once.  
:Example: ``.disable``  



**.info**  

:Description: Shows your current Pokemon and Raid boss notification subscriptions.  
:Example: ``.info``  



**.demo**  

:Description: Displays a demos regarding how to use Brock.  
:Example: ``.demo``  