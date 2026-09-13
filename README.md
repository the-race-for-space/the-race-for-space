**The Race for Space - Rival Agencies and New Funding Contracts**

**Rival system:**

This simulates competing agencies who collect their own science, funds and manage their own construction and Kerbals and send them on missions. They can take contracts from this mod only and take a share of the funds away from the player, they also collect science if they get there first. It is basically a text based simulation. No actual assets appear in the KSP universe from these rivals but they do interact with the player currently in two ways.

New funding contracts can be achieved and split between player and rival agencies. (The Rivals do not interact with the stock contract system or any other modded contracts) They can complete stock science experiments before the player using this resource up
    

**UI Includes:**

UI includes one panel to read the rivals simulation and what they are doing. Also includes a list of contracts for the player to receive funding with.

Notifications are sent to the player using stock notifications whenever the rivals achieve something e.g research/construction/missions.

Also a contract panel in flight mod to see current contract/funding objectives.

**Funding Contracts Include:**

20 funding contracts before a Kerbin Orbit
48 - orbiting other bodies including:
16 - Satellite Network contracts one for each body in Kerbin Stock System
16 - probe contracts one for each body in Kerbin Stock System
16 - Crewed contracts one for each body in Kerbin Stock System

**Compatibility**

Currently no support for other mods. e.g the Rivals only use the stock science and this own mods funding contracts. But I should see no other compatibility issues with other mods. Please let me know if you run into issues with compatibility.

**AI Coding and Disclosure**

Programed using AI. Is this Slop than? Well maybe, but I enjoy the end product so it is up to you. I have not run into performance issues yet in my testing. Things function how I want them to and I have attempted to do plenty of testing.

**Performance Cost**

Mods programmed extensively using AI tools are known for performance issues. As we all know AI likes to take shortcuts or make long complicated solutions to what should be simpler code. For disclosure this is how the code is rigged to work. I have not tested this on modded heavy builds. I will be interested to know how it behaves in such instances. I have just tested only using a small number of mods for quality of life things.

The mods run time makes four calls currently:

Live UI - UI updates timers live and responds to the interface live.

Once Every Second - This call every second checks whether the player has completed any pre-orbit contracts criteria. Once the pre-orbit contracts are complete this is no longer checked anymore.

Every 5 Seconds - This is the main part of the simulation where the mod checks every 5 seconds if the following has happened. It checks if the system has reached an end of a day than science expeditions are updated. At the end of every 5 days launch progress is checked for missions. Every 90 days a funding event happens where construction/science and new funding appears and payouts are given.

Every 20 seconds - We run an algorithm to check the number of player satellites in orbit of bodies this is in order to check if the player has complete any satellite funding contracts. This does not change very often so only needs to be done every 20 seconds. (This means it can take up to 20 seconds for a new player satellite to be counted once in orbit)
