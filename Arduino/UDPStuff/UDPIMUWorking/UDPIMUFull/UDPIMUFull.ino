#include <ArduinoJson.h>
#include <ArduinoJson.hpp>
#include <ESP8266WiFi.h>
#include <WiFiUdp.h>

#include "Madgwick.h"
#include "AK09918.h"
#include "ICM20600.h"
#include <Wire.h>

const char* ssid = "ESP8266 UDP";
const char* password = "12345678";

WiFiUDP Udp;
const unsigned int gyroSenderPort = 4210;
const unsigned int gunStateSenderPort = 4211;
const unsigned int localUdpPort = 5000;  // local port to listen on
char incomingPacket[255];                // buffer for incoming packets
char replyPacket[] = "Have some Data";   // a reply string to send back

StaticJsonDocument<64> imuDoc;
StaticJsonDocument<80> commandDoc;

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
double roll, pitch;
// Find the magnetic declination at your location
// http://www.magnetic-declination.com/
double declination_shenzhen = -0.55;

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
    Serial.printf("UDP packet contents: %s\n", incomingPacket);
    deserializeJson(commandDoc, incomingPacket);
    CalibrateGyro = commandDoc["CG"];
    if(CalibrateGyro == 2){
      Serial.println("Applying uploaded calibration");
      offset_x = commandDoc["CX"];
      offset_y = commandDoc["CY"];
      offset_z = commandDoc["CZ"];
      commandDoc["CG"] = CalibrateGyro = 0;
    }
    else if (CalibrateGyro == 1){
      Udp.beginPacket("192.168.4.2", gunStateSenderPort);
      serializeJson(commandDoc, Udp);
      Udp.println();
      Udp.endPacket();
      Serial.println("Start figure-8 calibration after 2 seconds.");
      delay(2000);
      calibrateCompass(10000, &offset_x, &offset_y, &offset_z);
    }
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

void setup() {
  // join I2C bus (I2Cdev library doesn't do this automatically)
  Wire.begin();

  err = ak09918.initialize();
  icm20600.initialize();
  ak09918.switchMode(AK09918_POWER_DOWN);
  ak09918.switchMode(AK09918_CONTINUOUS_100HZ);
  Serial.begin(115200);

  err = ak09918.isDataReady();
  while (err != AK09918_ERR_OK) {
      Serial.println("Waiting Sensor");
      delay(100);
      err = ak09918.isDataReady();
  }

  filter.begin(0.2f);
  setupUDP();
    
}

void loop() {
  recieveUDP();
  if(CalibrateGyro != 1){
    imuLoop();
  }
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
  
  // with compass correction
  filter.update(gyro_x, gyro_y, gyro_z, acc_x, acc_y, acc_z, comp_x, comp_y, comp_z);
}

void sendOreintation(){

  // no compass correction
  //filter.updateIMU(gyro_x, gyro_y, gyro_z, acc_x, acc_y, acc_z);
  float W = filter.getQuatW();
  float X = filter.getQuatX();
  float Y = filter.getQuatY();
  float Z = filter.getQuatZ();

  // Serial.print("QW: ");
  // Serial.print(W);
  // Serial.print("\tQX: ");
  // Serial.print(X);
  // Serial.print("\tQY: ");
  // Serial.print(Y);
  // Serial.print("\tQZ: ");
  // Serial.println(Z);
  
  //Serial.println("sendingData");
  imuDoc["W"] = W;
  imuDoc["X"] = X;
  imuDoc["Y"] = Y;
  imuDoc["Z"] = Z;
  //serializeJson(imuDoc, Serial);
  //Serial.println();

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
  delay(100);

  timeStart = millis();

  while ((millis() - timeStart) < timeout) {
      ak09918.getData(&comp_x, &comp_y, &comp_z);

      /* Update comp_x-Axis max/min value */
      if (value_x_min > comp_x) {
          value_x_min = comp_x;
          // Serial.print("Update value_x_min: ");
          // Serial.println(value_x_min);

      } else if (value_x_max < comp_x) {
          value_x_max = comp_x;
          // Serial.print("update value_x_max: ");
          // Serial.println(value_x_max);
      }

      /* Update comp_y-Axis max/min value */
      if (value_y_min > comp_y) {
          value_y_min = comp_y;
          // Serial.print("Update value_y_min: ");
          // Serial.println(value_y_min);

      } else if (value_y_max < comp_y) {
          value_y_max = comp_y;
          // Serial.print("update value_y_max: ");
          // Serial.println(value_y_max);
      }

      /* Update comp_z-Axis max/min value */
      if (value_z_min > comp_z) {
          value_z_min = comp_z;
          // Serial.print("Update value_z_min: ");
          // Serial.println(value_z_min);

      } else if (value_z_max < comp_z) {
          value_z_max = comp_z;
          // Serial.print("update value_z_max: ");
          // Serial.println(value_z_max);
      }

      Serial.print(".");
      delay(100);

  }

  *offsetx = value_x_min + (value_x_max - value_x_min) / 2;
  *offsety = value_y_min + (value_y_max - value_y_min) / 2;
  *offsetz = value_z_min + (value_z_max - value_z_min) / 2;
  Serial.println("");
  sendCalibrationBackUDP();
}