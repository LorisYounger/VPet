# VPet-Simulator Tutorial

**This tutorial only opens auto the first time you start/act on an update** 
*If you can see this file open auto every time you start Desktop pet, that is a bug, please feedback to me*

Desktop pet on by default[Data Calculation](#Data Calculation), After turning on the data calculation, the consumption of desktop pet mood/food will be calculated, and the player needs to interact. You can set the switch and set the pace of play in the settings

The core of this game is desktop pets, more auto action such as walking around, daze, crouching and so on need to stay online to see.

## How 2 play

### Right click to open the menu bar

Right-click again to close

![tut1](Tutorial.assets/tut1.gif)

### Long press the head/body to move

The long press time can be set in the settings

![tut5](Tutorial.assets/tut5.gif)

### Click the head to touch the head

![tut2](Tutorial.assets/tut2.gif)

### Click the body to speak

![tut3](Tutorial.assets/tut3.gif)

### Touch head

![tut4](Tutorial.assets/tut4.gif)

### Touch body

![ss1](Tutorial.assets/ss1.gif)

## Interact

### Feeding

Click to feed to eat and drink, immediately after eating/drinking, half of the fullness and thirst will be replenished, and the remaining half will slowly increase

![ss16](Tutorial.assets/ss16.gif)

### Work

Make money through interact **copywriting** or **Live**, after earning money, you can buy food and drinks or furniture for desktop pet (if there is a community system)

![ss15](Tutorial.assets/ss15.gif)

### Learn

Learn to gain EXP faster

### Sleep

Less stamina exertion and faster recovery of stamina, suitable for stay online

## Data Calculation

Settings-Interact can toggle data calculation and other related operations

![image-20230724101858667](Tutorial.assets/image-20230724101858667.png)

### Data Calculation

After opening, the desktop pet will have a series of needs, such as thirst and hunger. If you just want to observe, turn off data calculation.

### Computing between

Calculate desktop pet state interval. The longer the time, the longer the frequency of desktop pet interaction demand, and the slower the consumption value. And vice versa, you can adjust it to your favorite style

* If you want to feed the water/eat desktop pet only occasionally, the longer the calculation interval, the better
* If you want to desktop pet glow faster, the shorter the calculation interval, the better

### Interaction Cycles

Decide after how many cycles the desktop pet decides to act auto, such as walking around. The longer the cycles, the lower the frequency

![ss18](Tutorial.assets/ss18.gif)

### desktop pet moving

Decide whether the desktop pet is allowed to perform displacement action, and the desktop pet will not move and stay put after closing

#### Smart Moving

When turned on, the desktop pet will only move when the player interact, otherwise it will stay where it is

## Custom links

Add shortcuts/webpages to the custom bar to start the desired function on the go
For more information about how to write keyboard shortcuts, see [Keyboard shortcuts](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.sendkeys?view=windowsdesktop-7.0#remarks) Generic Annotations

![image-20230620063315866](Tutorial.assets/image-20230620063315866.png)

After saving the settings, you can see the customized shortcut keys in the desktop pet menu bar

![image-20230620063216134](Tutorial.assets/image-20230620063216134.png)

## Data Introduction

### Money

It can be used to buy things to eat and drink in Betterbuy, which is very useful

### EXP

Level up the player, the higher the level, the higher the money/experience obtained by working and studying. It also increases the Likeablilty limit

### Stamina

Interacting with the desktop pet (touching the head and body) consumes stamina and converts into mood

### Mood

Maintaining good condition and increasing the speed of EXP acquisition, high mood will also increase Likeablilty.

### Hunger

Over time consumption will get low, low Hungry mean desktop pet hungry. High Hunger restores Stamina and improves health

### Thirst

Over time consumption will get low, low Thirst mean desktop pet thirst. High Thirst restores Stamina and improves health

### Health(hide)

Low health will get sick, sick will not be able to work and study

### Likeablilty(hide)

High Likeablilty can make the body healthier, and will also trigger hidden events such as different speak content.