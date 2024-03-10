#include <ArduinoJson.h>
#include <ArduinoJson.hpp>
#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
#include <PCF8574.h>
#include <RotaryEncoder.h>

#include "Madgwick.h"
#include "AK09918.h"
#include "ICM20600.h"
#include <Wire.h>



// IMU

StaticJsonDocument<64> imuDoc;

const long gyroSendCoolDownTime = 24;
volatile long gyroTime = gyroSendCoolDownTime;
volatile long gyroRememberTime;

volatile byte CalibrateGyro = 0;

Madgwick filter;

AK09918_err_type_t err;
int32_t comp_x, comp_y, comp_z;
AK09918 ak09918;
ICM20600 icm20600(true);
int16_t acc_x, acc_y, acc_z, gyro_x, gyro_y, gyro_z;
int32_t offset_x, offset_y, offset_z;


// pins
const int rotaryButton = 0;     //PCF Input pulled down, Interrupt
const int magPin = 6;           //PCF Input pulled down, Interrupt
const int doorPin = 1;          //PCF Input pulled down, Interrupt
const int rateOfFire1 = 3;      //PCF Input pulled down, Interrupt
const int rateOfFire2 = 4;      //PCF Input pulled down, Interrupt
const int flyWheelTrigger = 5;  //PCF Input pulled down, Interrupt
const int fireTrigger = 2;      //PCF Input pulled down, Interrupt

const int iSquaredSCL = D1;  //ESP
const int iSquaredSDA = D2;  //ESP

const int rotaryCW = D3;   //ESP Input Pulled Up, Interrupt
const int rotaryACW = D4;  //ESP Input Pulled Up, Interrupt
const int PCFintPin = D7;  //ESP Input Pulled Up, Interrupt

const int ammoPin = A0;  //ESP Input anagloue

const int flyWheelMotorPin = D6;  //ESP output
const int triggerMotorPin = D5;   //ESP output
const int ledPin = D0;            //ESP output


volatile bool R1 = false;     // rate of fire 1
volatile bool R2 = false;     // rate of fire 2
volatile bool AMMO1 = false;     // ammo present
volatile bool AMMO2 = false;     // door open
volatile bool AMMO3 = false;     // mag inserted
volatile bool SWITCH1 = false;     // fly wheel trigger
volatile bool SWITCH2 = false;     // main trigger
volatile bool RotaryButton = false;     // rotary encoder button
volatile int encoderPos = 0;  // rotary encoder count

volatile bool SWITCH2_LAST_STATE = false;

// Rotary encoder
volatile int oldEncPos = 0;


// pcf interrupt flag
volatile bool flag = false;
volatile uint8_t PCFpinState;

// LED
const long ledCoolDownTime = 500;
long ledTime = ledCoolDownTime;
volatile long rememberTime = 0;
bool flashLED = false;

// analog ammo detector
const long ldrReadCoolDownTime = 10;
long ldrTime = ldrReadCoolDownTime;
volatile long ldrRememberTime = 0;
byte ammoTimeOutCount = 3;
volatile byte timeOutCount = 0;

const int ammoPresentThreshold = 25;
volatile int shotsFired;
volatile bool burst = false;
volatile byte currentRateOfFire = 0;


// operational Behaviour
bool nerfGunEmulationMode = true;
volatile bool motorsRunning = false;

StaticJsonDocument<192> stateDoc;
StaticJsonDocument<80> commandDoc;

PCF8574 PCF(0x20);

RotaryEncoder* encoder = nullptr;


// wifi
const char* ssid = "ESP8266 UDP";
const char* password = "12345678";

WiFiUDP Udp;
const unsigned int gyroSenderPort = 4210;
const unsigned int gunStateSenderPort = 4211;
const unsigned int localUdpPort = 5000;  // local port to listen on
char incomingPacket[255];                // buffer for incoming packets

// interrupts
void ICACHE_RAM_ATTR pcf_irq() {
  flag = true;
  R1 = PCF.read(rateOfFire1);      // rate of fire 1
  R2 = PCF.read(rateOfFire2);      // rate of fire 2
  AMMO2 = PCF.read(doorPin);          // door open
  AMMO3 = PCF.read(magPin);           // mag inserted
  SWITCH1 = PCF.read(flyWheelTrigger);  // fly wheel trigger
  SWITCH2 = PCF.read(fireTrigger);      // main trigger
  RotaryButton = PCF.read(rotaryButton);     // rotary encoder button
}

void ICACHE_RAM_ATTR pcf_read8_Interrupt(){
  //PCFpinState = PCF.read8();
  flag = true;
}

void setPCFFields(){
  R1 = (PCFpinState & (1 << rateOfFire1)) > 0;      // rate of fire 1
  R2 = (PCFpinState & (1 << rateOfFire2)) > 0;      // rate of fire 2
  AMMO2 =(PCFpinState & (1 << doorPin)) > 0;          // door open
  AMMO3 =(PCFpinState & (1 << magPin)) > 0;           // mag inserted
  SWITCH1 = (PCFpinState & (1 << flyWheelTrigger)) > 0;  // fly wheel trigger
  SWITCH2 = (PCFpinState & (1 << fireTrigger)) > 0;      // main trigger
  RotaryButton = (PCFpinState & (1 << rotaryButton)) > 0;     // rotary encoder button
}

IRAM_ATTR void checkPosition() {
  encoder->tick();  // just call tick() to check the state.
}

// pin set up
void setupPins() {

  pinMode(PCFintPin, INPUT_PULLUP);

  encoder = new RotaryEncoder(rotaryCW, rotaryACW, RotaryEncoder::LatchMode::FOUR3);

  pinMode(flyWheelMotorPin, OUTPUT);
  pinMode(triggerMotorPin, OUTPUT);
  pinMode(ledPin, OUTPUT);

  digitalWrite(flyWheelMotorPin, LOW);
  digitalWrite(triggerMotorPin, LOW);
  digitalWrite(ledPin, LOW);

  attachInterrupt(PCFintPin, pcf_irq, FALLING);
  //attachInterrupt(PCFintPin, pcf_read8_Interrupt, FALLING);
  
  attachInterrupt(digitalPinToInterrupt(rotaryCW), checkPosition, CHANGE);
  attachInterrupt(digitalPinToInterrupt(rotaryACW), checkPosition, CHANGE);
}

// gun runtime

byte getRateOfFire() {
  byte rateOfFire = 0;// single shot mode
  burst = true;
  if (R1) {
    rateOfFire = 1; // three shot mode
  } else if (R2) {
    rateOfFire = 2; // full auto
    burst = false;
  }
  return rateOfFire;
}

void getState() {
  stateDoc["RF"] =  currentRateOfFire = getRateOfFire();  // rate of fire 1
  stateDoc["A1"] = AMMO1;               // ammo pin
  stateDoc["A2"] = AMMO2;               // door pin
  stateDoc["A3"] = AMMO3;               // mag pin
  stateDoc["S1"] = SWITCH1;               // fly wheel pin
  stateDoc["S2"] = SWITCH2;               // trigger pin
  stateDoc["RE"] = encoderPos;       // rotaryEncoderCount
  stateDoc["RB"] = RotaryButton;               // rotary encoder button

  //Serial.print("Out going packet size: ");
  //Serial.println(stateDoc.memoryUsage());
  //serializeJson(stateDoc, Serial);
  //Serial.println();
  Udp.beginPacket("192.168.4.2", gunStateSenderPort);
  serializeJson(stateDoc, Udp);

  Udp.println();
  Udp.endPacket();
}

void updateLED() {

  long curTime = millis();
  long diff = curTime - rememberTime;
  if (flashLED || ledTime != ledCoolDownTime) {
    ledTime -= diff;
    if (ledTime <= 0) {
      ledTime = ledCoolDownTime;
      digitalWrite(ledPin, !digitalRead(ledPin));
      if (!flashLED) {
        digitalWrite(ledPin, LOW);
      }
    }
  }
  rememberTime = curTime;
}

void updateRE() {
  encoder->tick();  // just call tick() to check the state.
  encoderPos = encoder->getPosition();
  if (oldEncPos != encoderPos) {
    Serial.println(encoderPos);
    oldEncPos = encoderPos;
    sendControllerStateUDP();
  }
}

void updatePCF() {
  if (flag) {
    sendControllerStateUDP();
    if(!SWITCH2){
       shotsFired = 0;
    }
    flag = false;
  }
}

void updateAmmoDetector() {
  
  long curTime = millis();
  long diff = curTime - ldrRememberTime;
  ldrTime -= diff;
  if(ldrTime <= 0){
    ldrTime = ldrReadCoolDownTime;
    int value = analogRead(ammoPin);
    if(value >= ammoPresentThreshold && AMMO1 == false){
      AMMO1 = true;
      timeOutCount = 0;
    }
    else if(value <= ammoPresentThreshold && AMMO1 == true) {
      if(timeOutCount < ammoTimeOutCount){
        timeOutCount +=1;
      }
      else{
        AMMO1 = false;
      }
      shotsFired+=1;
      if(nerfGunEmulationMode && SWITCH2){
        Udp.beginPacket("192.168.4.2", gunStateSenderPort);
        Udp.println("");
        Udp.endPacket();
      }
    }
  }
  ldrRememberTime = curTime;
}

void emulateNerfGun() {
  flashLED = !AMMO1;
  if (nerfGunEmulationMode) {
    bool runFlyWheels = AMMO1 && AMMO2 && AMMO3 && SWITCH1;
    bool burstLockOut = !burst || (currentRateOfFire == 0 && shotsFired < 1) || (currentRateOfFire == 1 && shotsFired < 3);
    bool runTriggerMotor = runFlyWheels && SWITCH2 && burstLockOut;
    digitalWrite(flyWheelMotorPin, runFlyWheels ? HIGH : LOW);
    digitalWrite(triggerMotorPin, runTriggerMotor ? HIGH : LOW);

    motorsRunning = runFlyWheels;
  } else if (motorsRunning) {

    digitalWrite(flyWheelMotorPin, LOW);
    digitalWrite(triggerMotorPin, LOW);
  }
}


// UDP runtime
void setupUDP() {
  Serial.print("Configuring access point...");
  Serial.println();

  WiFi.softAP(ssid, password);
  IPAddress myIP = WiFi.softAPIP();
  Serial.print("AP IP address: ");
  Serial.println(myIP);

  Udp.begin(localUdpPort);
  Serial.printf("Now listening at IP %s, UDP port %d\n", myIP.toString().c_str(), localUdpPort);
}

void recieveUDP() {

  int packetSize = Udp.parsePacket();
  if (packetSize) {
    int len = Udp.read(incomingPacket, 255);
    if (len > 0) {
      incomingPacket[len] = 0;
    }
    deserializeJson(commandDoc, incomingPacket);
    CalibrateGyro = commandDoc["CG"];
    nerfGunEmulationMode = commandDoc["EM"];
    //Serial.println(nerfGunEmulationMode);
    handleCalibrateGyro();
  }
}

void sendControllerStateUDP() {
  getState();
}

void debug() {
  //flashLED = !A1;
  int value = analogRead(ammoPin);
  bool runFlyWheels = AMMO1 && AMMO2 && AMMO3 && SWITCH1;
  bool runTriggerMotor = runFlyWheels && SWITCH2;
  Serial.print("A1: ");
  Serial.print(AMMO1);
  Serial.print(" A2: ");
  Serial.print(AMMO2);
  Serial.print(" A3: ");
  Serial.print(AMMO3);
  Serial.print(" S1: ");
  Serial.print(SWITCH1);
  Serial.print(" S2: ");
  Serial.print(SWITCH2);
  Serial.print(" RF: ");
  Serial.print(getRateOfFire());
  Serial.print(" RB: ");
  Serial.print(RotaryButton);
  Serial.print(" M1: ");
  Serial.print(runFlyWheels);
  Serial.print(" M2: ");
  Serial.print(runTriggerMotor);
  Serial.println();
}

void setup() {
  setupPins();
  delay(500);
  Serial.begin(115200);
  setupIMU();
  delay(500);
  PCF.begin();
  setupUDP();
}

void loop() {
  updateAmmoDetector();
  recieveUDP();
  imuLoop();
  updateLED();
  updateRE();
  updatePCF();
  emulateNerfGun();
}

void handleCalibrateGyro(){
  if(CalibrateGyro == 2){
    Serial.println("Applying uploaded calibration");
    offset_x = commandDoc["CX"];
    offset_y = commandDoc["CY"];
    offset_z = commandDoc["CZ"];
    commandDoc["CG"] = CalibrateGyro = 0;
  }
  else if (CalibrateGyro == 1){

    nerfGunEmulationMode = false;
    emulateNerfGun();

    Udp.beginPacket("192.168.4.2", gunStateSenderPort);
    serializeJson(commandDoc, Udp);
    Udp.println();
    Udp.endPacket();
    Serial.println("Start figure-8 calibration after 2 seconds.");

    delay(2000);
    calibrateCompass(10000, &offset_x, &offset_y, &offset_z);
    nerfGunEmulationMode = commandDoc["EM"];
  }
}

void sendCalibrationBackUDP(){
  Serial.println("Sending back data");
  commandDoc["EM"] = 0;
  commandDoc["CG"] = 3;
  commandDoc["CX"] = offset_x;
  commandDoc["CY"] = offset_y;
  commandDoc["CZ"] = offset_z;
  //CalibrateGyro = 0;

  serializeJson(commandDoc, Serial);
  Udp.beginPacket("192.168.4.2", gunStateSenderPort);
  serializeJson(commandDoc, Udp);
  Udp.println();
  Udp.endPacket();
  Serial.println("Sent back data");
}

void setupIMU(){
  
  Wire.begin();
  err = ak09918.initialize();
  icm20600.initialize();
  ak09918.switchMode(AK09918_POWER_DOWN);
  ak09918.switchMode(AK09918_CONTINUOUS_100HZ);
  err = ak09918.isDataReady();
  while (err != AK09918_ERR_OK) {
      Serial.println("Waiting Sensor");
      delay(100);
      err = ak09918.isDataReady();
  }

  filter.begin(0.2f);
}

void imuLoop(){
  if(CalibrateGyro != 1){
    long curTime = millis();
    long diff = curTime - gyroRememberTime;
    gyroRememberTime = curTime;
    gyroTime -= diff;
    //updateIMUFilter();
    if(gyroTime <= 0){
      gyroTime = gyroSendCoolDownTime;
      updateIMUFilter();
      sendOreintation();
    }
  }
}

void updateIMUFilter(){
  // get linear acceleration
  acc_x = icm20600.getAccelerationX();
  acc_y = icm20600.getAccelerationY();
  acc_z = icm20600.getAccelerationZ();

  // get rotation acceleration
  gyro_x = icm20600.getGyroscopeX();
  gyro_y = icm20600.getGyroscopeY();
  gyro_z = icm20600.getGyroscopeZ();
  
  // get compass data
  ak09918.getData(&comp_x, &comp_y, &comp_z);
  comp_x = comp_x - offset_x;
  comp_y = comp_y - offset_y;
  comp_z = comp_z - offset_z;
  
  filter.update(gyro_x, gyro_y, gyro_z, acc_x, acc_y, acc_z, comp_x, comp_y, comp_z);
}

void sendOreintation(){

  float W = filter.getQuatW();
  float X = filter.getQuatX();
  float Y = filter.getQuatY();
  float Z = filter.getQuatZ();

  imuDoc["W"] = W;
  imuDoc["X"] = X;
  imuDoc["Y"] = Y;
  imuDoc["Z"] = Z;

  Udp.beginPacket("192.168.4.2", gyroSenderPort);
  serializeJson(imuDoc, Udp);

  Udp.println();
  Udp.endPacket();
}

void calibrateCompass(uint32_t timeout, int32_t* offsetx, int32_t* offsety, int32_t* offsetz) {
  int32_t value_x_min = 0;
  int32_t value_x_max = 0;
  int32_t value_y_min = 0;
  int32_t value_y_max = 0;
  int32_t value_z_min = 0;
  int32_t value_z_max = 0;
  uint32_t timeStart = 0;

  ak09918.getData(&comp_x, &comp_y, &comp_z);

  value_x_min = comp_x;
  value_x_max = comp_x;
  value_y_min = comp_y;
  value_y_max = comp_y;
  value_z_min = comp_z;
  value_z_max = comp_z;

  
  digitalWrite(ledPin, HIGH);
  delay(2000);
  delay(50);
  digitalWrite(ledPin, LOW);
  delay(50);
  digitalWrite(ledPin, HIGH);
  delay(50);
  digitalWrite(ledPin, LOW);
  delay(50);
  digitalWrite(ledPin, HIGH);
  delay(50);
  digitalWrite(ledPin, LOW);
  delay(50);
  digitalWrite(ledPin, HIGH);

  timeStart = millis();

  while ((millis() - timeStart) < timeout) {
      ak09918.getData(&comp_x, &comp_y, &comp_z);

      if (value_x_min > comp_x) {
          value_x_min = comp_x;

      } else if (value_x_max < comp_x) {
          value_x_max = comp_x;
      }

      if (value_y_min > comp_y) {
          value_y_min = comp_y;

      } else if (value_y_max < comp_y) {
          value_y_max = comp_y;
      }

      if (value_z_min > comp_z) {
          value_z_min = comp_z;

      } else if (value_z_max < comp_z) {
          value_z_max = comp_z;
      }

      digitalWrite(ledPin, !digitalRead(ledPin));
      delay(100);

  }

  *offsetx = value_x_min + (value_x_max - value_x_min) / 2;
  *offsety = value_y_min + (value_y_max - value_y_min) / 2;
  *offsetz = value_z_min + (value_z_max - value_z_min) / 2;
  delay(50);
  digitalWrite(ledPin, LOW);
  delay(50);
  digitalWrite(ledPin, HIGH);
  delay(50);
  digitalWrite(ledPin, LOW);
  delay(50);
  digitalWrite(ledPin, HIGH);
  Serial.println("");
  sendCalibrationBackUDP();
  delay(25);
  digitalWrite(ledPin, LOW);
}