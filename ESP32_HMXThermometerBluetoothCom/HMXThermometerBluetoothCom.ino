// Include libraries
#include <SPI.h>
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>
#include <Adafruit_MLX90614.h>
#include "BluetoothSerial.h"
#define ONBOARD_LED 2
#define SCREEN_WIDTH 128 // OLED display width, in pixels
#define SCREEN_HEIGHT 32 // OLED display height, in pixels
// Declaration for an SSD1306 display connected to I2C (SDA, SCL pins)
#define OLED_RESET     4 // Reset pin # (or -1 if sharing Arduino reset pin)
Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);
Adafruit_MLX90614 mlx = Adafruit_MLX90614();
BluetoothSerial ESP_BT;

String alarm1State;
String result; //result to send via serial comms
float reading; // initial reading
float newReading; //new temp reading
String message = "";
String UIstate = "S1";
const int offlinePin = 34;
const int alarm1 =  4;      // GPIO of the ALARM
const int ledR =  23;      // GPIO of the RED LED
const int ledG = 19;        // GPIO of the GREEN LED

//SR04
const int trigPin = 18;
const int echoPin = 5;
long duration;
int distance;


float mlxhumanTemp()
{
  float hTemp = 1.5 + mlx.readObjectTempC();
  return hTemp;
}


void LCDprint(String temp, String dist)
{
  display.clearDisplay();
  display.setTextSize(1);             // Draw 2X-scale text
  display.setTextColor(SSD1306_WHITE);
  display.setCursor(0, 0);
  display.print("Temp: ");
  display.setTextSize(2);
  display.print(temp);
  display.setTextSize(1);
  display.print("o");
  display.setTextSize(2);
  display.println("C");
  display.setCursor(0, 18);
  display.setTextSize(1);
  display.print("Range: ");
  display.setTextSize(2);
  display.print(dist); display.println("cm");
  display.display();
  delay(1000);
}
void LCDStart()
{
  display.clearDisplay();
  display.setTextColor(SSD1306_WHITE);
  display.setCursor(0, 0);
  display.setTextSize(2);
  display.println("HMXThermal");
  display.setCursor(0, 18);
  display.setTextSize(1);
  display.println("V3 | Build 0408");
  display.display();
  delay(1000);
}

float readTemp() {


  reading = mlxhumanTemp();
  if (reading > 1000)
  {
    reading = mlxhumanTemp();
  }
  digitalWrite(ledR, HIGH);
  digitalWrite(ledG, HIGH);
  for (int i = 0; i < 10; i++)
  {
    newReading = mlxhumanTemp();
    //remove invalid result
    if (newReading < 1000)
    {
      if (reading < newReading)
      {
        reading = newReading;
      }
    }
    delay(100);

  } //end for loop

  if (reading > 37.5)
  {
    digitalWrite(ledR, HIGH);
    digitalWrite(alarm1, HIGH);
    //digitalWrite(ledY, LOW);
    digitalWrite(ledG, LOW);


  }
  else
  {
    digitalWrite(alarm1,LOW);
    digitalWrite(ledR, LOW);
    //digitalWrite(ledY, LOW);
    digitalWrite(ledG, HIGH);
  }
  String serialTemp = "T=";
  serialTemp += reading;
  serialTemp += "*C";
  Serial.println(String(serialTemp));
  //String LCDResult = String(reading) + "*C, " + String(distance) + "cm";
  LCDprint(String(reading), String(distance));
  ESP_BT.println("("+String(reading)+","+String(distance)+")");
  return reading;
}

void doToggle() {

  if (digitalRead(alarm1)) {
    digitalWrite(alarm1, LOW);
  } else {
    digitalWrite(alarm1, HIGH);
  }
}
void resetalarm() {
  digitalWrite(alarm1, LOW);

}

void printMLXReading() {

  Serial.print("Ambient = "); Serial.print(mlx.readAmbientTempC() * 1.0526);
  Serial.print("*C\tObject = "); Serial.print(mlx.readObjectTempC() * 1.0526); Serial.println("*C");
  Serial.print("Ambient = "); Serial.print(mlx.readAmbientTempF() * 1.0526);
  Serial.print("*F\tObject = "); Serial.print(mlx.readObjectTempF() * 1.0526); Serial.println("*F");
  Serial.println();

}


void setup() {
  // initialize the LEDs pins as an output:
  pinMode(trigPin, OUTPUT); // Sets the trigPin as an Output
  pinMode(echoPin, INPUT); // Sets the echoPin as an Input
  pinMode(ledR, OUTPUT);
  pinMode(ledG, OUTPUT);
  pinMode(alarm1, OUTPUT);
  pinMode(ONBOARD_LED, OUTPUT);
  pinMode(offlinePin, INPUT);
  //Initialize serial and wait for port to open:
  Serial.begin(9600);
  delay(1000);

  ESP_BT.begin("HMXThermalSensor"); //Name of your Bluetooth Signal
  Serial.println("HUBMATRIX Contactless Thermometer V3");
  mlx.begin();
  mlx.writeEmissivity(0.9);
  digitalWrite(ONBOARD_LED, HIGH);
  // SSD1306_SWITCHCAPVCC = generate display voltage from 3.3V internally
  if (!display.begin(SSD1306_SWITCHCAPVCC, 0x3C)) { // Address 0x3C for 128x32
    Serial.println(F("SSD1306 allocation failed"));
    for (;;); // Don't proceed, loop forever
  }
  LCDStart();

}

void measureDistance()
{

  // Clears the trigPin
  digitalWrite(trigPin, LOW);
  delayMicroseconds(2);

  // Sets the trigPin on HIGH state for 10 micro seconds
  digitalWrite(trigPin, HIGH);
  delayMicroseconds(10);
  digitalWrite(trigPin, LOW);

  // Reads the echoPin, returns the sound wave travel time in microseconds
  duration = pulseIn(echoPin, HIGH);

  // Calculating the distance
  distance = duration * 0.034 / 2;

  // Prints the distance on the Serial Monitor
  Serial.print("Distance: ");
  Serial.println(distance);
  
      readTemp();

      if (distance <= 99 && distance > 0)
      {
      }

}

void loop() {

  //Force offline measurement
  /*
  if (digitalRead(offlinePin) == HIGH)
  {
     delay(100);
     if (digitalRead(offlinePin) == HIGH)
     {
        Serial.println("offlinePin=HIGH");
        measureDistance();
        delay(1000);
     }
  } */

  if (UIstate != "S0") {
    
    measureDistance();
    //delay(500);
  }

  if (ESP_BT.available()){
    char incomingChar = ESP_BT.read();
    if (incomingChar != '\n'){
      message += String(incomingChar);
    }
    else{
      message = "";
    }
    Serial.write(incomingChar);  
  }
  // Check received message and control output accordingly
  if (message =="S1"){
    UIstate = "S1";
  }
  else if (message == "clear"){
    doToggle();
  }
  else if (message == "S0"){
    UIstate = "S0";
  }
  delay(20);
}

 
