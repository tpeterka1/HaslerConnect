The HaslerConnect program is made to gather current speed info from train simulators and send them to a microcontroller controlling a Hasler RT9 mechanical speedometer using a DC motor.
This repo now also contains the Arduino/RPPico code used to actually drive the motor and the stl files for 3D printing the parts. The motor used is the Pololu 4842.
The Hasler RT9 I have is made for 800 RPM to reach max speed, code adjustment is necessary for use with other configurations.

**Currently supported simulators**
- Train Simulator Classic (RailWorks) by Dovetail - RailDriver dll integration
- Tran Sim World 6 by Dovetail - Utilising new HTTP API
- Train Driver 2 by TTSK - OCR

**Building**
- Build using Jet Brains Rider/VS 2022 for .NET 8.0