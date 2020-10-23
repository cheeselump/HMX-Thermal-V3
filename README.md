# HMX-Thermal-V3
Source code for HubMatrix Thermal Solution V3 (Used for DP)

This project makes use of an NodeMCU ESP32, MLX90614 and a distance sensor (I used the HC-SR04 but I would highly recommend an IR sensor for greater accuracy),
to contactlessly measure forehead skin temperature of human beings (emissivity set at 0.9). 

Results can be displayed in a SSD1306 LCD, or as I did - be displayed in a C# WPF based self-service kiosk. 

This setup was deployed for hotel use in Singapore. 

You will need the Arduino IDE, and Visual Studio for this code. 
