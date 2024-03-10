
#include <ArduinoJson.h>
#include <ArduinoJson.hpp>
#include <ESP8266WiFi.h>
#include <WiFiClient.h>
#include <ESP8266WebServer.h>
#include <ESP8266mDNS.h>
#include <PCF8574.h>
#include <RotaryEncoder.h>

#ifndef APSSID
#define APSSID "ESPap"
#define APPSK "thereisnospoon"
#endif


const char *ssid = "ESP8266 mDNS";
const char *password = "12345678";

const int rotaryButton = 0; //PCF Input pulled down, Interrupt
const int magPin = 1; //PCF Input pulled down, Interrupt
const int doorPin = 2; //PCF Input pulled down, Interrupt
const int rateOfFire1 = 3; //PCF Input pulled down, Interrupt
const int rateOfFire2 = 4; //PCF Input pulled down, Interrupt
const int flyWheelTrigger = 5; //PCF Input pulled down, Interrupt
const int fireTrigger = 6; //PCF Input pulled down, Interrupt

const int iSquaredSCL = D1; //ESP
const int iSquaredSDA = D2; //ESP

const int rotaryCW = D3; //ESP Input Pulled Up, Interrupt
const int rotaryACW = D4; //ESP Input Pulled Up, Interrupt
const int PCFintPin = D7; //ESP Input Pulled Up, Interrupt

const int ammoPin = A0; //ESP Input anagloue

const int flyWheelMotorPin = D5; //ESP output
const int triggerMotorPin = D6; //ESP output
const int ledPin = D0; //ESP output

volatile bool R1 = false; // rate of fire 1
volatile bool R2 = false; // rate of fire 2
volatile bool A1 = false; // ammo present
volatile bool A2 = false; // door open
volatile bool A3 = false; // mag inserted
volatile bool S1 = false; // fly wheel trigger
volatile bool S2 = false; // main trigger
volatile bool RB = false; // rotary encoder button
volatile int encoderPos = 0; // rotary encoder count

// Rotary encoder
volatile int oldEncPos = 0;


// pcf interrupt flag
volatile bool flag = false;

// LED 
const long ledCoolDownTime = 500;
long ledTime = ledCoolDownTime;
volatile long rememberTime=0;
bool flashLED = false;

// analog ammo detector
const int ammoPresentThreshold = 25;

// operational Behaviour
bool nerfGunEmulationMode = true;
volatile bool motorsRunning = false;

StaticJsonDocument<192> stateDoc;

ESP8266WebServer server(80);

PCF8574 PCF(0x20);

RotaryEncoder *encoder = nullptr;

void ICACHE_RAM_ATTR pcf_irq()
{
  flag = true;
  R1 = PCF.read(rateOfFire1); // rate of fire 1
  R2 = PCF.read(rateOfFire2); // rate of fire 2
  A2 = PCF.read(doorPin); // door open
  A3 = PCF.read(magPin); // mag inserted
  S1 = PCF.read(flyWheelTrigger); // fly wheel trigger
  S2 = PCF.read(fireTrigger); // main trigger
  RB = PCF.read(rotaryButton); // rotary encoder button
}

IRAM_ATTR void checkPosition()
{
  encoder->tick(); // just call tick() to check the state.
}

void setupPins(){

  pinMode(PCFintPin, INPUT_PULLUP);

  encoder = new RotaryEncoder(rotaryCW, rotaryACW, RotaryEncoder::LatchMode::FOUR3);

  pinMode(flyWheelMotorPin,OUTPUT);
  pinMode(triggerMotorPin,OUTPUT);
  pinMode(ledPin,OUTPUT);

  digitalWrite(flyWheelMotorPin,LOW);
  digitalWrite(triggerMotorPin,LOW);
  digitalWrite(ledPin,LOW);

  attachInterrupt(PCFintPin, pcf_irq, FALLING);
  attachInterrupt(digitalPinToInterrupt(rotaryCW), checkPosition, CHANGE);
  attachInterrupt(digitalPinToInterrupt(rotaryACW), checkPosition, CHANGE);
}

void setupAP(){
  Serial.println();
  Serial.print("Configuring access point...");
  WiFi.softAP(ssid, password);
  IPAddress myIP = WiFi.softAPIP();
  Serial.print("AP IP address: ");
  Serial.println(myIP);
  Serial.println("HTTP server started");

  if(MDNS.begin("esp8266")){
    Serial.println("mDNS resonder Started");
    
  }
  else{
    Serial.println("Error setting up mDNS responder!");
  }

  server.onNotFound(handleNotFound);
  server.begin();
  Serial.println("HTTP server started");
}

byte getRateOfFire(){
  byte rateOfFire = 0;
  if(R1){
    rateOfFire = 1;
  }
  else if(R2){
    rateOfFire = 2;
  }
  return rateOfFire;
}

void getState(){
  stateDoc["RF"] = getRateOfFire(); // rate of fire 1
  stateDoc["A1"] = A1; // ammo pin
  stateDoc["A2"] = A2; // door pin
  stateDoc["A3"] = A3; // mag pin
  stateDoc["S1"] = S1; // fly wheel pin
  stateDoc["S2"] = S2; // trigger pin
  stateDoc["RE"] = encoderPos; // rotaryEncoderCount
  stateDoc["RB"] = RB; // rotary encoder button
  
  String buf;
  serializeJson(stateDoc, buf);
  server.send(200,"application/json",buf);
}

void startLEDflash(){
  flashLED = true;
  server.send(204,"");
}

void stopLEDflash(){
  flashLED = false;
  server.send(204,"");
}

void spinUpTrigger(){
  setDigPinWith204(triggerMotorPin,HIGH);
}

void spinDownDown(){
  setDigPinWith204(triggerMotorPin,LOW);
}

void spinUpFlyWheels(){
  setDigPinWith204(flyWheelMotorPin,HIGH);
}

void spinDownFlyWheels(){
  setDigPinWith204(flyWheelMotorPin,LOW);
}

void setDigPinWith204(int pin, int state){
  digitalWrite(pin,state);
  server.send(204,"");
}

void updateLED(){

  long curTime = millis();
  long diff = curTime-rememberTime;
  if(flashLED || ledTime != ledCoolDownTime){
    ledTime -=diff;
    if(ledTime <=0){
      ledTime = ledCoolDownTime;
      digitalWrite(ledPin, !digitalRead(ledPin));
      if(!flashLED){
        digitalWrite(ledPin, LOW);
      }
    }
  }
  rememberTime = curTime;
}

void updateRE(){
  encoder->tick(); // just call tick() to check the state.
  encoderPos = encoder->getPosition();
  if(oldEncPos != encoderPos) {
    Serial.println(encoderPos);
    oldEncPos = encoderPos;
  }
}

void updatePCF(){
  if(flag){
    Serial.println("PCF Interrupt Recorded");
    flag = false;
  }
}

void updateAmmoDetector(){
  int value = analogRead(ammoPin);
  A1 = false;
  if(value >=ammoPresentThreshold ){
    A1 = true;
  }
}

void emulateNerfGun(){
  if(nerfGunEmulationMode){
    flashLED = !A1;
    bool runFlyWheels = A1 && A2 && A3 && S1;
    bool runTriggerMotor = runFlyWheels && S2;
    digitalWrite(flyWheelMotorPin, runFlyWheels ? HIGH:LOW);
    digitalWrite(triggerMotorPin, runTriggerMotor ? HIGH:LOW);

    motorsRunning = runFlyWheels;
  }
  else if(motorsRunning){
    
    digitalWrite(flyWheelMotorPin, LOW);
    digitalWrite(triggerMotorPin, LOW);
  }
}

void debug(){
  //flashLED = !A1;
  int value = analogRead(ammoPin);
  bool runFlyWheels = A1 && A2 && A3 && S1;
  bool runTriggerMotor = runFlyWheels && S2;
  Serial.print("A1: ");  
  Serial.print(A1);
  Serial.print(" A2: ");  
  Serial.print(A2);
  Serial.print(" A3: ");  
  Serial.print(A3);
  Serial.print(" S1: ");  
  Serial.print(S1);
  Serial.print(" S2: ");  
  Serial.print(S2);
  Serial.print(" RF: ");
  Serial.print(getRateOfFire());
  Serial.print(" M1: ");  
  Serial.print(runFlyWheels);
  Serial.print(" M2: ");  
  Serial.print(runTriggerMotor);
  Serial.println();
}

void setup() {
  setupPins();
  delay(1000);
  Serial.begin(115200);
  PCF.begin();
  //setupAP();
}

void loop() {
  //server.handleClient();
  updateLED();
  updateRE();
  updatePCF();
  updateAmmoDetector();
  emulateNerfGun();
  //debug();
}

void handleNotFound(){
  server.send(404, "text/plain", "404: Not found");
}
