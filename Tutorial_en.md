# VPet-Simulator Tutorial

**This tutorial only automatically opens the first time you start the program and when you start it after an update** 
*If you can see this file automatically open every time you start Desktop pet, that is a bug. Please report it to me*


[Data Calculation](#Data Calculation), which affects the attribute changes for the Virtual Pet, is on by default, but can be toggled under System > Interact. Data Collection toggles whether or not consumption of the Virutal Pet's attributes, such as stamina, mood, hunger, and thirst, is on. 

The core gameplay is the progression of the Virtual Pet, while automatic actions such as walking around, dazing, crouching, and so on needs the player to be present to witness them.

## How 2 Play

### Right Click to Open/Close the Menu Bar

![tut1](Tutorial.assets/tut1.gif)

### Long Press the Head/Body to Move

The Long Press Interval can be set under Settings > Interact.

![tut5](Tutorial.assets/tut5.gif)

### Click the Head to Pat the Head

![tut2](Tutorial.assets/tut2.gif)

### Click the Body to Speak

![tut3](Tutorial.assets/tut3.gif)

### Touch Head

![tut4](Tutorial.assets/tut4.gif)

### Touch Body

![ss1](Tutorial.assets/ss1.gif)

## Interact

### Feeding

Clicking on Feed > Food/Drink/Medicine will take you to Betterbuy's respective category. Immediately after purchasing something, the Virtual Pet will consume the product, and half of the associated hunger and thirst values will be replenished instantly, while the other half will be added gradually.

![ss16](Tutorial.assets/ss16.gif)

### Work

Make Money through Interact > Work > **Copywriting** or **Live** (unlocked at level 10), which can be used to buy food, drinks, and medicine from BetterMart. There may be furniture for the Virtual Pet available in the future.

![ss15](Tutorial.assets/ss15.gif)

### Learn

Learn increases EXP. Research is unlocked at level 15.

### Sleep

Sleep lowers Stamina exertion and faster recovery of Stamina, which is better for AFKing.

## Data Calculation

Settings > Interact can toggle data calculation and other related operations.

![English Settings Screenshot](Tutorial.assets/Tutorial_EN/VPet_Settings.PNG)

### Data Calculation

While the program is running, Virtual Pet will have a series of needs, such as Mood, Hunger, and Thirst. If you just want to just observe the Virtual Pet without worrying about these needs, turn off data calculation.

### Computing between

Calculate Virtual Pet Data Calculation interval. The longer the interval, the longer the frequency of Virtual Pet interactions and the lower the rate of Consumption. Adjust this to your preference.

* If you only want to feed the Virtual Pet occasionally, a longer calculation interval is better.
* If you want to Virtual Pet grow faster, a shorter calculation interval is better.

### Interaction Cycles

Decide after how many Data Calculation cycles the Virtual Pet decides to automatically move around. The longer the cycles, the lower the frequency of interactions.

![ss18](Tutorial.assets/ss18.gif)

### Virtual Pet moving

Decide whether the Virtual Pet is allowed to perform displacement action, and the Virtual Pet will not move and stay put after closing.

#### Smart Moving

When turned on, the Virtual Pet will only move with player interaction, otherwise it will stay where it is.

## Custom links

Add shortcuts/webpages to the custom bar to start the desired function on the go
For more information about how to write keyboard shortcuts, see [Keyboard shortcuts](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.sendkeys?view=windowsdesktop-7.0#remarks) Generic Annotations

![image-20230620063315866](Tutorial.assets/image-20230620063315866.png)

After saving the settings, you can see the customized shortcut keys in the Virtual Pet menu bar.

![image-20230620063216134](Tutorial.assets/image-20230620063216134.png)

## Data Introduction

### Money

It can be used to buy things in Betterbuy, which affects the attributes of the Virtual Pet.

### EXP

Affects the level of the Virtual Pet. Higher levels increases Money and Experience obtained by working and studying, as well as the Limit for Likeability.

### Stamina

Interacting with the Virtual Pet (touching the head and body) converts Stamina into Mood.

### Mood

High mood will maintain Health and increase EXP Acquisition and Likeability.

### Hunger

Depleted over time. High Hunger (meaning Satiation) restores Stamina and improves Health.

### Thirst

Depleted over time. High Thirst restores Stamina and improves Health.

### Health(hidden)

Low Health will lead to the Virtual Pet getting sick, which makes her unable to work or study.

### Likeablilty(hidden)

High Likeablilty can make the Virtual Pet healthier, and will also trigger hidden events such as different speak content.