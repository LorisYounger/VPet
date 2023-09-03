# VPet Tutorial

<!-- NTS: I'm not familiar with this game, but I'm taking "操作更新" literally here, as the app opens the tutorial if the last time it was seen is before a certain date; this date could be changed when a gameplay update occurs -->
> **This tutorial will only open automatically when the game is started for the first time, or after a gameplay update.** If it opens *every time the game is started*, that is a bug - please report it.

<!-- FIXME; The original sentence is confusing. -->
This game revolves around you taking care of your virtual pet by interacting with it. Additionally, automatic actions such as wandering around, spacing out, squatting, etc. will be performed when the game is idle.

**Data Calculation** is turned on by default, meaning your pet's stats, including stamina, mood, hunger, and thirst, etc. will change naturally; you may enter System > Interact to [switch it off or tune the gameplay pace](#data-calculation).


## Basics

<!-- The code is ugly, I know :pensive: -->

----------
|![tut1](Tutorial.assets/CN/tut1.gif)|<img alt="tut5" src="Tutorial.assets/CN/tut5.gif" width="65%">|![tut2](Tutorial.assets/CN/tut2.gif)|
|--|--|--|
|Right click on your pet to toggle the menu bar.|Long press her head/body to drag her around.<br/>The Long Press Interval can be set under Settings > Interact.|Click on her head to pat her.|
|![tut3](Tutorial.assets/CN/tut3.gif)|<img alt="tut4" src="Tutorial.assets/CN/tut4.gif" width="50%">|![ss1](Tutorial.assets/CN/ss1.gif)|
|Click on her body to talk with her.|Maybe touch her head?|Touch her body, even?|
<!-- Humorous -->


## Interaction

### Feeding

Selecting Feed > Food/Drink/Medicine directs you to the corresponding categories on Betterbuy. Once you purchase an item, your virtual pet will eat/drink it right away, instantly restoring half of the designated hunger or thirst values, with the other half recovering over time.

![ss16](Tutorial.assets/CN/ss16.gif)

### Work

Direct your pet to make money by selecting Interact > Work > **Prepping** or **Live** (unlocked at level 10). After earning money, you can buy food, drinks, or even furniture for your virtual pet (through Workshop subscriptions).

![ss15](Tutorial.assets/CN/ss15.gif)

### Learn

Direct your virtual pet to Learn to gain EXP faster. **Research** is unlocked at Level 15.

### Sleep

Sleeping reduces stamina exertion and promotes faster stamina recovery; ideal for AFKing.


## Data Calculation

Data calculation, along with other related settings, can be changed under Settings > Interact.

![English Settings Screenshot](Tutorial.assets/EN/VPet_Settings.PNG)

### "Data Calculation"

When this is turned on, you'll need to deal with various needs of your virtual pet, such as thirst, hunger, and mood. If you prefer to simply treat your pet as a shimeji (desktop buddy/mascot), keep data calculation off.

### "Computing between"

This is the interval of the routine stat computation. The longer the interval, the less frequent your virtual pet needs attention, and the slower her stats decay. Conversely, the shorter the interval, the faster her stats decay. You can adjust this to your preference.

* If you only want to feed your virtual pet occasionally, a longer calculation interval is better.
* If you want your virtual pet to level up faster, a shorter calculation interval is better.

### "Interaction Cycles"

Affects how many data calculation cycles it takes before your virtual pet decides to move around on its own. The larger this number is, the less frequently your virtual pet moves.

![ss18](Tutorial.assets/CN/ss18.gif)

### "Pet Moving"

Affects whether the virtual pet should move. If this option is toggled off, your virtual pet will stay put.

#### "Smart Moving"

When turned on, your virtual pet will only move after player interaction, and stay put otherwise.


## Custom Links

You can add shortcuts/web pages/keyboard shortcuts to the DIY menu, for easy access to desired functions. For the syntax of keyboard shortcut codes, please refer to [Microsoft's documentation on this subject](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.sendkeys.send?view=windowsdesktop-6.0#remarks). Right-click to perform operations such as sorting and deleting.

![image-20230620063315866](Tutorial.assets/CN/image-20230620063315866.png)

After saving your links, you can see them in the menu bar.

![image-20230620063216134](Tutorial.assets/CN/image-20230620063216134.png)

## Introduction of Stats

<!-- It's still called Likeability -->

|Name|Description|
|----|-----------|
|Money|Can be used to buy things in Betterbuy. It's very useful.|
|EXP|Affects the level of your virtual pet. The higher the level, the more Money/EXP can be obtained from working and studying, and the higher the cap of Likeability is.|
|Stamina|Interacting with your virtual pet (touching its head and body) converts Stamina into Mood.|
|Mood|Keeps your pet healthy and makes EXP gaining quicker. A high value can increase Likeability as well.|
|Hunger|Decays over time. The lower this value is, the hungrier your pet is. A high value restores Stamina and increases Health.|
|Thirst|Decays over time. The lower this value is, the thirstier your pet is. High Thirst value (meaning not thirsty) restores Stamina and improves Health.|
|Health (hidden)|Your virtual pet will be sick when this value is low, which disables Work or Study.|
|Likeability (hidden)|High Likeability makes your virtual pet healthier, and will also cause hidden events (such as special dialog) to occur.|