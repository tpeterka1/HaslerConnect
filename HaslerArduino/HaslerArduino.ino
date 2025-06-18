// Credits to Curio Res for most of this file - https://www.youtube.com/watch?v=HRaZLCBFVDE

#define ENCA 26
#define ENCB 27
#define PWM 20
#define IN1 19
#define IN2 18

//globals
long prevT = 0;
int posPrev = 0;
volatile int pos_i = 0;

static float currentTarget = 0;
int targetSpeed = 0;
int targetMotorSpeed = 0;
float maxRampRate = 150; // RPM per second

float v1Filt = 0;
float v1Prev = 0;

float eintegral = 0;
int k = 0;

void setup() {
  Serial.begin(115200);

  //analogWriteFreq(20000);  // 20kHz, so no more annoying beep from motor
  analogWriteFreq(16000);
  //analogWriteFreq(3000);

  pinMode(ENCA,INPUT);
  pinMode(ENCB,INPUT);
  pinMode(PWM,OUTPUT);
  pinMode(IN1,OUTPUT);
  pinMode(IN2,OUTPUT);

  attachInterrupt(digitalPinToInterrupt(ENCA), readEncoder, RISING);
}

void loop() {
  if (Serial.available()) {
    String input = Serial.readStringUntil('\n'); // Read speed until newline
    targetSpeed = input.toInt();
  }
  targetSpeed = constrain(targetSpeed, 0, 120); // Max speed of the speedometer is 120
  targetMotorSpeed = map(targetSpeed, 0, 120, 0, 800); // Map 120 kph to 800 RPM
  if (targetMotorSpeed > 0) {
    targetMotorSpeed = constrain(targetMotorSpeed, 80, 1000); // Lowest I could get the motor to run - 80 RPM
  }

  // read the position
  int pos = 0;
  float velocity2 = 0;
  noInterrupts();  // Disable interrupts
  pos = pos_i;
  interrupts();    // Re-enable interrupts

  long currT = micros();
  float deltaT = ((float) (currT-prevT))/1.0e6;
  float velocity1 = (pos - posPrev)/deltaT;
  posPrev = pos;
  prevT = currT;

  // Convert count/s to RPM
  //116.68?
  float v1 = velocity1/116.16*60.0; // Encoder has 12 detections per 1/4 of motor revolution, motor has 9.68:1 ratio (12*9.68)

  // Low-pass filter (25 Hz cutoff)
  //v1Filt = 0.854*v1Filt + 0.0728*v1 + 0.0728*v1Prev;
  // Low-pass filter (20 Hz cutoff)
  v1Filt = 0.881*v1Filt + 0.0591*v1 + 0.0591*v1Prev;
  v1Prev = v1;

  // Set a target
  //float vt = 300*(sin(currT/1e6)>0); // 300 RPM target switching with 0
  //float vt = 300; // fixed 300 RPM
  float vt = (float)targetMotorSpeed;

  // Ramp gradually
  if (vt > currentTarget) {
    currentTarget += maxRampRate * deltaT;
    if (currentTarget > vt) currentTarget = vt;
  } else if (vt < currentTarget) {
    currentTarget -= maxRampRate * deltaT;
    if (currentTarget < vt) currentTarget = vt;
  }

  // Compute the control signal u
  float kp = 0.275;
  float ki = 0.175;
  //float e = vt-v1Filt;
  float e = currentTarget-v1Filt;
  eintegral = eintegral + e*deltaT;

  // Anti-windup - limit integral term
  float maxIntegral = 255/ki;
  //float maxIntegral = 1457;
  if(eintegral > maxIntegral) eintegral = maxIntegral;
  if(eintegral < -maxIntegral) eintegral = -maxIntegral;
  
  float u = kp*e + ki*eintegral;

  // Set the motor speed and direction
  int dir = 1;
  if (u<0){
    dir = -1;
  }
  int pwr = (int) fabs(u);
  if(pwr > 255){
    pwr = 255;
  }

  if (targetMotorSpeed == 0) {
    pwr = 0;
  }

  //float output = ((float)pwr/255)*100;
  setMotor(dir,pwr,PWM,IN1,IN2);

  //if (k % 3 == 0){ // Prevent sending too much data to the Serial con (fucks up frequency for filtering)
  //  Serial.print("0.0, ");
  //  Serial.print("1200.0, ");
  //  Serial.print(vt);
  //  Serial.print(" ");
  //  Serial.print(v1Filt);
  //  Serial.print(" ");
  //  Serial.print(pwr);
  //  Serial.println();
  //}
  //k = k+1;

  delay(1);
}

void setMotor(int dir, int pwmVal, int pwm, int in1, int in2){
  if (pwmVal == 0 || pwmVal < 80){
    analogWrite(pwm,0);
    digitalWrite(in1,LOW);
    digitalWrite(in2,LOW);
  } else {
    analogWrite(pwm,pwmVal); // Motor speed
    if(dir == 1){ 
      // Turn one way
      digitalWrite(in1,LOW);
      digitalWrite(in2,HIGH);
    }
    else if(dir == -1){
      // Turn the other way
      digitalWrite(in1,HIGH);
      digitalWrite(in2,LOW);
    }
    else{
      // Or dont turn
      digitalWrite(in1,LOW);
      digitalWrite(in2,LOW);    
    }
  }
}

void readEncoder(){
  // Read encoder B when ENCA rises
  int b = digitalRead(ENCB);
  int increment = 0;
  if(b>0){
    // If B is high, increment forward
    increment = 1;
  }
  else{
    // Otherwise, increment backward
    increment = -1;
  }
  pos_i = pos_i + increment;
}