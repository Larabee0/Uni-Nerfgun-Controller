#include <ESP8266WiFi.h>
#include <WiFiUdp.h>

const char* ssid = "ESP8266 UDP";
const char* password = "12345678";

WiFiUDP Udp;
int ledPin = 2;
unsigned int localUdpPort = 5000;  // local port to listen on
char incomingPacket[255];  // buffer for incoming packets
char  replyPacket[] = "Have some Data";  // a reply string to send back

bool latch = false;

void setup()
{
  pinMode(ledPin, OUTPUT);
  digitalWrite(ledPin, LOW);
  delay(1000);
  Serial.begin(115200);
  Serial.print("Configuring access point...");
  Serial.println();

  WiFi.softAP(ssid, password);
  IPAddress myIP = WiFi.softAPIP();
  Serial.print("AP IP address: ");
  Serial.println(myIP);

  Udp.begin(localUdpPort);
  Serial.printf("Now listening at IP %s, UDP port %d\n", myIP.toString().c_str(), localUdpPort);
}


void sendAndRecieve(){
  int packetSize = Udp.parsePacket();
  if (packetSize)
  {
    // receive incoming UDP packets
    Serial.printf("Received %d bytes from %s, port %d\n", packetSize, Udp.remoteIP().toString().c_str(), Udp.remotePort());
    int len = Udp.read(incomingPacket, 255);
    if (len > 0)
    {
      incomingPacket[len] = 0;
    }
    Serial.printf("UDP packet contents: %s\n", incomingPacket);

    // send back a reply, to the IP address and port we got the packet from
    //Udp.beginPacket(Udp.remoteIP(), Udp.remotePort());
    //Udp.write(replyPacket);
    //Udp.endPacket();
  }
}

void sendOnly(){

  int packetSize = Udp.parsePacket();
  if (packetSize){
    latch = true;
    Serial.printf("Received %d bytes from %s, port %d\n", packetSize, Udp.remoteIP().toString().c_str(), Udp.remotePort());
    int len = Udp.read(incomingPacket, 255);
    if (len > 0)
    {
      incomingPacket[len] = 0;
    }
    Serial.printf("UDP packet contents: %s\n", incomingPacket);
  }
  if(latch){
    Udp.beginPacket(Udp.remoteIP(), Udp.remotePort());
    Udp.write(replyPacket);
    Udp.endPacket();
    delay(16);
  }
}

void loop()
{
  sendAndRecieve();
  Udp.beginPacket("192.168.4.2", 4210);
  Udp.write(millis());
  Udp.endPacket();
  delay(10);
}