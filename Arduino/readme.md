# Arduino Project Files

This is the folder for your Arduino code files and should include these file types:

- *.ino* (Arduino Sketches)
- *.cpp* (C++ program file)
- *.h* (C++ headers)


I started with HTTP stuff, messed with rotary encoders for a bit and then went messaged with 9DOF. Once the pcb prototype was more completed it became apparent that HTTP was too slow.
So I switch to UDP and then got everything working there. - including the gyro.
The main prototype sketch has support for the rotary encoder to control zoom, although for submission the rotary encoder does not exist due to the modification needed to the scope to make it possible.

The code deployed to the Arduino for submission is under UDPStuff\UDPPrototype\UDPPrototype