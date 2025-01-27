# Overview


# 3rd Party resources
## Phone Holder
- https://www.thingiverse.com/thing:3312779 - Creative Commons Licensed
## Nerf Side Mounts
- https://www.thingiverse.com/thing:4569489 - Creative Commons NonCommercial Licensed
## AA to C Battery Adaptors
- https://www.thingiverse.com/thing:1719966 - Creative Commons Share Alike Licensed
## Nerf Light Holder - used to create back up phone mount
- https://www.thingiverse.com/thing:2649151  - Creative Commons Licensed
## FastIMU/Magdwick Filter library
- https://github.com/LiquidCGS/FastIMU/ / https://github.com/xioTechnologies/Fusion - Both MIT Licensed



# Proposal
## COMP102 Nerfgun Game
#### Early concept of design
![Initial Design Diagram](https://github.com/Larabee0/Uni-Nerfgun-Controller/blob/main/Images/Early%20design%20diagram.png)
#### Game Design
This game will turn a a nerfgun into a video game controller for a first person single player shooter game. This game will be similar to Aimlab - just a simple target shooter with no real enemies or tactical AI as a proof of concept.
The fun part is when you fire the gun it fires both in game and in real life. Although for conviency, this will be a toggleable setting.

#### Physical Design
This game will involve attaching a mobile phone to a nerfgun and having the user physically move themselves and handle the nerfgun in order to move in the virtual environment.

This is not an AR game and isn't really a VR game either, the phone will be physically attached to the nerfgun in such a way that the end scope of nerfgun butts right up to the phone's screen and convers about 40% of it.
The game has two camera perspectives at all times, one visible down the scope and the other visible on the rest of the screen.

###### Scope Camera
The scope and camera perspective for the scope will only be visible by looking down the physical scope of the nerfgun. On the side of this physical scope I will add a a rotary encoder, a device that can spin infinitely in a clockwise and counter clockwise direction and knows what direction it is being turned.
This will be used to zoom in and zoom out the camera perspective.
Rotary encoders provide a nice tactile motion when turning them which I think will feel statisfying. Additionally they have a button accuated by pressing down on the knob, which will be used for resetting the zoom.

###### Physical Feedback
As noted earlier the game will have to option to disable the firing of the gun. under such a state I want to have the gun use the fly wheels like rumble motors in a controller, just spin them up little for feedback.
The physical feedback is important to me for this project, attaching all the nerfgun addons feels satisifying as does inserting the maganise, I hope to enhance this as I have described above and with the scope zoom using a rotary encoder instead 
of a potentimeter. A potentimeter would probably be easier to implement but would not feel as nice to use as the tactile feedback from the rotary encoder.

#### Parts Needed
- A nerfgun (already have)
- Arduino with bluetooth or wifi capablities
- 9 DOF IMU (Gyroscope, Accelerometer and Compass)
- MOSFETS (For switiching the electric motors in the nerfgun on and off)
- Resistors (For forming a potential divide to make use of hte existing ammo detector)
- Rotary Encoder (For Adjusting scope zoom and for providing nice tactile feedback)
- 3D printed components - adaptor box for holding the Arduino and wiring & a mounting adaptor for attaching the phone to the nerfgun securely.
- (potentially) A boost converter to ensure reliable oporation of the Arduino off the nerfgun's battery pack

#### Key Milestones

###### General
- ~~Demonstrating Wireless communication from the Arduino to Unity on a PC~~ - Done
- ~~Demonstrating Wireless communication from the Arduino to Unity on a Mobile Device~~
- ~~Demonstrating the ability to send JSON data from the Arduino to Unity~~ - Done
- ~~Demonstrate the above 3 items but Unity to Arduino.~~

###### Game
- ~~Create a basic concept with the two desired camera perspectives~~ - Done
- Add the ability to change the position and size ratio of the two perspectives on the phone screen.
- ~~Interpret and smooth the IMU data from the Arduino to move the camera perspective in the game.~~
- ~~Improve this motion to be as smooth and natural feeling as possible.~~
- ~~Add a "nerfgun" model to the game to be the virutal representation of the physical one.~~
- ~~Give this similar functionality to the real one - limited ammo, requires two triggers to be pressed to fire, toggleable rate of fire (single shot, burst & full auto)~~
- ~~Add the ability to use the physical the triggers of the gun to be used fire the gun in game and in real life~~
- ~~Add a setting to disable the firing of the physical gun but keep the firing in the game.~~
- ~~Create an environment for  dynamically adding targets into.~~
- ~~Create scoring and accuracy metrics.~~

###### Hardware
- ~~Reverse engineering the mechanisims and switches of the nerfgun~~ - Done
- ~~Stripping the nerfgun of its original control board and soldering on jumper cables for allowing Arduino control of the nerfgun.~~ - Done
- ~~Create an Electrical prototype of the Arduino control of the gun (make sure there is enough I/O and power from the internal battery pack)~~
- Modify the scope to incorporate the rotary encoder
- ~~3D print a mounting bracket for attaching a phone to the gun~~
- 3D print a knob for the rotary encoder

###### Hardware Software
- ~~Write software for the Arduino for recieiving commands from the host application on what the gun should be doing (firing, not firing, preparing to fire [spin up flywheels])~~
- ~~Write software for the Arduino to allow the game to get the control state of the gun (switch positions for the triggers, whether there is ammo, the current position rate of fire selector)~~
- ~~Interruprenting information from IMU to provide meaningful rotational data for the game.~~

