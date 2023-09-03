# VPet-Simulator Tutorial

**This tutorial automatically opens only when you start the program for the first time or after an update** 
*If this file pops up every time you start Desktop pet, that is a bug. Please report it to me*

[Data Calculation](#Data Calculation), which affects whether or not there will be changes in the Virtual Pet's attributes, including stamina, mood, hunger, and thirst, etc. This feature is turned on by default, but you can switch it off under System > Interact.

The core gameplay revolves taking care of your Virtual Pet. However, more automatic actions, such as wandering around, spacing out, squatting, etc., can be seen only when the game is idle.

## Basics

### Right Click to Open/Close the Menu Bar

![tut1](Tutorial.assets/CN/tut1.gif)

### Long Press the Head/Body to Move

The Long Press Interval can be set under Settings > Interact.

![tut5](Tutorial.assets/CN/tut5.gif)

### Click on the Head to Pat Her

![tut2](Tutorial.assets/CN/tut2.gif)

### Click on the Body to Speak with Her

![tut3](Tutorial.assets/CN/tut3.gif)

### Touch Head

![tut4](Tutorial.assets/CN/tut4.gif)

### Touch Body

![ss1](Tutorial.assets/CN/ss1.gif)

## Interaction

### Feeding

Selecting Feed > Food/Drink/Medicine directs you to the related categories on Betterbuy. Once you purchase an item, your Virtual Pet eats or drinks it right away, instantly restoring half of the specified hunger or thirst values, with the other half recovering over time.

![ss16](Tutorial.assets/CN/ss16.gif)

### Work

Make Money through Interact > Work > **Prepping** or **Live** (unlocked at level 10). After earning money, you can buy food, drinks, or even furniture for your Virtual Pet (through Workshop subscriptions).

![ss15](Tutorial.assets/CN/ss15.gif)

### Learn

Direct your Virtual Pet to Learn will increase EXP. Research is unlocked at Level 15.

### Sleep

Sleeping reduces stamina exertion and promotes faster stamina recovery; ideal for AFKing.

## Data Calculation

Data calculation, along with other related settings, can be toggled on or off under Settings > Interact.

![English Settings Screenshot](Tutorial.assets/EN/VPet_Settings.PNG)

### Data Calculation

When turned on, the needs system for your Virtual Pet becomes active, causing thirst, hunger, and mood to decrease over time. If you'd rather skip this part of gameplay, simply keep data calculation off.

### Calculation Interval

Calculates status change of your Virtual Pet. The longer the interval, the less frequent your Virtual Pet needs attention, and the slower her needs decay. Conversely, the shorter the interval, the faster her needs decay. You can adjust this to your preference.

* If you only want to feed your Virtual Pet occasionally, a longer calculation interval is better.
* If you want your Virtual Pet to level up faster, a shorter calculation interval is better.

### Interaction Cycles

Affects how many data calculation cycles are allowed to pass before your Virtual Pet decides to move around. The larger the number, the less frequent your Virtual Pet moves.

![ss18](Tutorial.assets/CN/ss18.gif)

### Virtual Pet Moving

Affects whether the Virtual Pet changes positions. Your Virtual Pet will stay put if this option is toggled off.

#### Smart Moving

When turned on, your Virtual Pet will only move after player interaction. Otherwise, she will stay put.

## Custom Shortcuts

Add shortcuts/web pages/keyboard-shortcuts. This allows for quick activations of desired functions. For instructions on how to set up keyboard shortcuts, please refer to [Keyboard shortcuts](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.sendkeys?view=windowsdesktop-7.0#remarks) general annotations. Right-click for sorting/deleting and other operations.

![image-20230620063315866](Tutorial.assets/CN/image-20230620063315866.png)

After saving the settings, you can see your customized shortcuts in your Virtual Pet menu bar.

![image-20230620063216134](Tutorial.assets/CN/image-20230620063216134.png)

## Introduction of Values

### Money

Used to buy things in Betterbuy to change the attributes of your Virtual Pet.

### EXP

Affects the level of your Virtual Pet. Higher levels increase Money and EXP obtained by working and studying, as well as the cap for Affinity value.

### Stamina

Interacting with your Virtual Pet (touching the head and body) converts Stamina into Mood.

### Mood

High mood will maintain good Health and increase EXP gained and Affinity.

### Hunger

Decays over time. High Hunger value (meaning not hungry) restores Stamina and improves Health.

### Thirst

Decays over time. High Thirst value (meaning not thirsty) restores Stamina and improves Health.

### Health (hidden)

Your Virtual Pet will get sick if on low Health, which disables Work or Study.

### Affinity (Likeability) (hidden)

High Affinity makes your Virtual Pet healthier and will also trigger hidden events such as special dialogs.