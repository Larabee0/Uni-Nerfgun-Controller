# Unity Project Files

Particuarly importnat scripts (Assets/Scripts)

# Arduino to Unity Communication via UDP Sockets
- UDPCommuincator (Scripts/UDP) - base class for talking to UDP sockets which the arduino uses for this project.
- GyroScopeCommunication (Scripts/UDP) - recives gyroscope data from the arduino.
- GunCommunication (Scripts/UDP) - receives gun data and can send commands back to the arduino.

# Misc
PersistantOptions - how the gyro calibration data is stored between hardware and application runtimes.
JsonClasses (Scripts/UDP) - Contains data structures for deserializing json from UDP packets into and also serailizing commands into Json for sending.

# Virtual Gun
VirtualNerfgun - script used to represent the virtual nerfgun, commands the firing of the in game gun.
VirtualNerfdart - script attached to little dart looking prefabs to give them a kick when they spawn.

# Gameplay Loop
- GameArbiter - runs the game spawns targets and keeps track of the player's stats.
- TargetPlane - class for picking random positions on a plane for targets to be spawned at
- Target - How the game knows if the player hit a target.
- RaisableTarget - script used for raising a target out of the ground.


