//Libraries
#include <math.h>
#include <MemoryFree.h>
#include <DS1302.h>
#include <SPI.h>
#include <mrf24j.h>
#include <HX711_ADC.h>

//Pin Declaration:
//TEMPERATURE
const byte PWM_pin = 3;  //Pin for PWM signal to the MOSFET driver (the BJT npn with pullup)
const byte Temp_Counter_Load = 6; //----- pin ---- 
const byte Temp_Sensor_Clock = 9; //----- pin ----
const byte Temp_Voltage_Input = A2; //----- pin ----

//PINS CLOCK RTC
const byte pin_CS_RTC = 8;  //pin Reset
const byte serial_RTC = 7;     // Input/Output
const byte SCK_CTR = 19;

//PINS ANTENNA
const byte pin_Reset_MRF24J40 = 14;
const byte pin_CS_MRF24J40 = 15; 
const byte pin_Int_MRF24J40 = 2; // default interrupt pin on ATmega8/168/328

//PINS FOR THE ACTIVITY
const byte CS_Activity = 18;

//PINS FOR THE SCALE
const byte DOUT = 4;
const byte SCK_scale = 5;


//GLOBAL VARIABLES
//TEMPERATURE
float temperature_previous;
//MRF24J40
long last_time;
long tx_interval = 1000;
//ACTIVITY
const char startOfNumberDelimiter = '<';
const char endOfNumberDelimiter   = '>';
boolean received = false;
int receivedNumber = 0;
boolean negative = false;


//loop counter
int counter = 0;

//INIT OBJECTS
//init RTC clock
DS1302 rtc(pin_CS_RTC, serial_RTC, SCK_CTR);
//init MRF24J40
Mrf24j mrf(pin_Reset_MRF24J40, pin_CS_MRF24J40, pin_Int_MRF24J40);
//init HX711_ADC
HX711_ADC LoadCell(DOUT, SCK_scale);


void setup (){
  Serial.begin(9600);
  //TEMPERATURE PIN SET-UP
  pinMode(PWM_pin,OUTPUT);
  pinMode(Temp_Voltage_Input, INPUT); // Declare input pin for the thermometer
  pinMode(Temp_Sensor_Clock, OUTPUT);
  analogReference(EXTERNAL); // setting up reference of 2.5 volt for the heater

  //ACTIVITY SETUP
  pinMode(CS_Activity, OUTPUT);

  //init MRF24J40 ----------- SETUP -----------------
  mrf.reset();
  mrf.init();
  mrf.set_pan(0xcafe);
  // This is _our_ address
  mrf.address16_write(0x4201);

  // uncomment if you want to receive any packet on this channel
  //mrf.set_promiscuous(true);

  // uncomment if you want to enable PA/LNA external control
  //mrf.set_palna(true);

  // uncomment if you want to buffer all PHY Payload
  //mrf.set_bufferPHY(true);

  attachInterrupt(0, interrupt_routine, CHANGE); // interrupt 0 equivalent to pin 2(INT0) on ATmega8/168/328
  last_time = millis();
  interrupts();

  //SCALE SETUP
  LoadCell.begin();
  //In our scale, we measured our tare once and used setTareOffset() and setCalFactor() to avoid the initial set-up every time we turn-on our mcu
  //To start with this program, you should start with the normal .start() function 
  //Make sure that if using the activity sensor with Serial interface, this is not connected since there is a need to use the Serial input feature of the monitor
  LoadCell.start(2000); //THE NUMBER MAKES IT WAIT A BIT, TO GET A MORE ACCURATE TARE
  //LoadCell.setCalFactor(27.28); // user set calibration value (float)
  //LoadCell.setTareOffset(136550144);

}

void loop(){

  //Get the temperature from the frame
  char* Temperature_Buffer = get_Temperature();
  //Serial.print("Temperature is "); Serial.println(Temperature_Buffer);
  //Serial.print("memory in loop: "); Serial.println(freeMemory());

  //Get weight from the SCALE
   LoadCell.update();

  if(counter == 0){
    //Get time from the RTC
    char* Time_Buffer = get_Time();
    Serial.print("Time is "); Serial.println(Time_Buffer);
    delay(200);

    //Get Activity
    char* Activity_Buffer = get_Activity();
     Serial.print("Activity is "); Serial.println(Activity_Buffer);

    //Get Weight
    char* Weight_Buffer = get_Weight();
    Serial.print("Weight is "); Serial.println(Weight_Buffer);

    //Send buffer to 0x6001 --------------- Add buffers together
    mrf.check_flags(&handle_rx, &handle_tx);


    char Data_Message[67];
    //char space[1] = {'&'};

    snprintf(Data_Message, sizeof(Data_Message), "%.50s%.5s%.6s%.5s", Time_Buffer, Temperature_Buffer, Activity_Buffer, Weight_Buffer);

    Serial.print("Message is "); Serial.println(Data_Message);
    mrf.send16(0x6001, Data_Message);
  }

  counter++;
  if(counter > 29){
    counter = 0;
  }

  delay(100);
}

///////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////GET VALUES////////////////////////////////
///////////////////////////////////////////////////////////////////////////////

char* get_Activity()
  {
    digitalWrite(CS_Activity, HIGH);

    int z = 0;
    z = processedInput();
    if (received) {
      Serial.println(z);
    }

    digitalWrite(CS_Activity, LOW);

    char buff[6];
    sprintf(buff, "%06d", z);
    return buff;
  }

char* get_Temperature(void){
  //variables and constant for reading temperature
  double temp;

  //Time_temp = millis();
  //Variables for PID controller
  bool TURN_ON = false;


  TURN_ON = true;
  digitalWrite(Temp_Counter_Load, HIGH);
  PIDcontrolframe(TURN_ON, temp, temperature_previous);
  digitalWrite(Temp_Counter_Load, LOW);
  char buff[5];

  int temp_int = int(temp * 100);

  sprintf(buff, "%05d", temp_int);
//  Serial.print("Temperature is: "); Serial.println(buff);
//  Serial.print("memory in temp is: "); Serial.println(freeMemory());
  return buff;
}

char* get_Time() {
  // Get the current time and date from the chip.
  Time t = rtc.time();

  // Name the day of the week.
  const String day = dayAsString(t.day);

  // Format the time and date and insert into the temporary buffer.
  char buf[50];
  snprintf(buf, sizeof(buf), "%s%04d%02d%02d%02d%02d%02d",
           day.c_str(),
           t.yr, t.mon, t.date,
           t.hr, t.min, t.sec);

  // Print the formatted string to serial so we can see the time.
  //Serial.println(buf);
  return buf;
}

char* get_Weight(){
  LoadCell.update();
  char buff[5];
  int weight = int(LoadCell.getData());
  sprintf(buff, "%05d", weight);
  return buff;
}
///////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////TEMPERATURE///////////////////////////////
///////////////////////////////////////////////////////////////////////////////

void PIDcontrolframe(bool TURN_ON, double& temp_max, float& temp_prev){

//  Serial.print("Memory free in PID"); Serial.println(freeMemory());
  byte set_temperature = 43;            //Default temperature setpoint.
  double PID_error = 0;
  double previous_error = 0;
  double PID_value = 0;
  //PID constants
  //////////////////////////////////////////////////////////
  byte kp = 100;   byte ki = 0;   byte kd = 50;
  //////////////////////////////////////////////////////////
  float PID_p = 0;    float PID_i = 0;    float PID_d = 0;
  int PID_values_fixed = 0;

  double elapsedTime_temp, Time_temp, Time_tempPrev;


  //function that controls the heater temperature
  // First we read the real value of temperature from the frame
  temp_max = -1000;
  double temp_tmp;

  for(byte i=0; i<8; i++){
    temperature_sensing(temp_tmp);
    if(temp_tmp>temp_max){
      temp_max = temp_tmp;
    }
  }
  temp_max = 0.5*temp_max+0.5*temp_prev;
  temperature_previous = temp_max;

  if (TURN_ON){
    //Next we calculate the error between the setpoint and the real value
    PID_error = set_temperature - temp_max; // why is this "+3" there?, was there in the orignal code. don't get why??
     //Calculate the P value (proportional)
    PID_p = kp* PID_error;

    //Calculate the I (integration) value in a range on +-3
    PID_i = PID_i + (ki * PID_error);
    //For derivative we need real Time_temp to calculate speed change rate
    Time_tempPrev = Time_temp;                            // the previous Time_temp is stored before the actual Time_temp read
    Time_temp = millis();                            // actual Time_temp read
    elapsedTime_temp = (Time_temp - Time_tempPrev) / 1000;
    //Now we can calculate the D value (derivative)
    PID_d = kd*((PID_error - previous_error)/elapsedTime_temp);
    //Final total PID value is the sum of P + I + D
    PID_value = PID_p + PID_i + PID_d;
    //We define PWM range between 0 and 255
    //Serial.println(PID_value);

    if(PID_value < 0){
      PID_value = 0;
    }
    if(PID_value > 255){
     PID_value = 255;
    }
    //Now we can write the PWM signal to the mosfet on digital pin D3
    //Since we activate the MOSFET with a 0 to the base of the BJT, we write 255-PID value (inverted)
    //for analogWrite PWM, if value = 0, always OFF// if value = 255, always ON
    analogWrite(PWM_pin,PID_value);
    previous_error = PID_error;     //Remember to store the previous error for next loop.
    //delay(250); //Refresh rate   ----- RE-TEST maybe we need
   }

  else {
    analogWrite(PWM_pin,0); // turn off PWM signal
  }

  //ADD DELAY MAYBE
}
void temperature_sensing(double& temp){
  //Serial.print("Memory free in sensing "); Serial.println(freeMemory());
  int x = 0;
  while (x<1000){
    if(x == 200){
      double temp_voltage = double(analogRead(Temp_Voltage_Input))*2.5/1024;
      double res_thermistor = 3880*temp_voltage/(2.5-temp_voltage);
      temp = 1/(0.003354016+0.0002569850*log(res_thermistor/10000)+0.000002620131*(pow(log(res_thermistor/10000),2))+0.00000006383091*(pow(log(res_thermistor/10000),3)))-273.15;
    }
    if(x >= 750){
      digitalWrite(Temp_Sensor_Clock, HIGH);
    }else{
      digitalWrite(Temp_Sensor_Clock, LOW);
    }
    x++;
  }
}

///////////////////////////////////////////////////////////////////////////////
////////////////////////////////////RTC CLOCK//////////////////////////////////
///////////////////////////////////////////////////////////////////////////////

String dayAsString(const Time::Day day) {
  switch (day) {
    case Time::kSunday: return "1";
    case Time::kMonday: return "2";
    case Time::kTuesday: return "3";
    case Time::kWednesday: return "4";
    case Time::kThursday: return "5";
    case Time::kFriday: return "6";
    case Time::kSaturday: return "7";
  }
  return "(unknown day)";
}

///////////////////////////////////////////////////////////////////////////////
//////////////////////////////////  ACTIVITY  /////////////////////////////////
///////////////////////////////////////////////////////////////////////////////


int processedInput ()
{

  serial_flush();

  received  = false;
  long time_start = millis();
  while (Serial.available() == 0) {
    if(millis()-time_start > 1000){
       Serial.println("error");
       return 0;
    }
  }

  while (!received) {

    byte c = Serial.read();

    if (c == startOfNumberDelimiter) {
      receivedNumber = 0;
      negative = false;
    }
    else {
      serial_flush();
      return 0;
    }

    while (!Serial.available()) {
    }

    c = Serial.read();

    int i = 0;

    while (c != endOfNumberDelimiter) {

      if (i > 10) {
        return 0;
      }

      switch (c)
      {
        case '0' ... '9':
          receivedNumber = receivedNumber * 10;
          receivedNumber += c - '0';
          break;

        case '-':
          negative = true;
          break;
      }

      while (!Serial.available()) {}

      c = Serial.read();
      i++;
    }

    received = true;
    serial_flush();


    if (negative) {
      return -receivedNumber;
    }
    else {
      return receivedNumber;
    }

  }
}

void serial_flush() {
  while (Serial.available()) {
    Serial.read();
  }
}
///////////////////////////////////////////////////////////////////////////////
//////////////////////////////////MRF24J40///////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////
void handle_rx() {
    //NOT RECEIVING DATA IN BEEHIVE FOR NOW
    /*Serial.print("received a packet ");Serial.print(mrf.get_rxinfo()->frame_length, DEC);Serial.println(" bytes long");

    if(mrf.get_bufferPHY()){
      Serial.println("Packet data (PHY Payload):");
      for (int i = 0; i < mrf.get_rxinfo()->frame_length; i++) {
          Serial.print(mrf.get_rxbuf()[i]);
      }
    }

    Serial.println("\r\nASCII data (relevant data):");
    for (int i = 0; i < mrf.rx_datalength(); i++) {
        Serial.write(mrf.get_rxinfo()->rx_data[i]);
    }

    Serial.print("\r\nLQI/RSSI=");
    Serial.print(mrf.get_rxinfo()->lqi, DEC);
    Serial.print("/");
    Serial.println(mrf.get_rxinfo()->rssi, DEC);*/
}
void handle_tx() {
    if (mrf.get_txinfo()->tx_ok) {
        Serial.println("TX went ok, got ack");
    } else {
        Serial.print("TX failed after ");Serial.print(mrf.get_txinfo()->retries);Serial.println(" retries\n");
    }
}
///////////////////////////////////////////////////////////////////////////////
//////////////////////////////INTERRUPT ROUTINE/////////////////////////////////
///////////////////////////////////////////////////////////////////////////////

void interrupt_routine() {
    mrf.interrupt_handler(); // mrf24 object interrupt routine
}

///////////////////////////////////////////////////////////////////////////////
//////////////////////////////SCALE FUNCTIONS/////////////////////////////////
///////////////////////////////////////////////////////////////////////////////

void calibrate() {
  Serial.println("Place a known weight on scale. Then type the weight in Serial Monitor");
  float m = 0; //KNOWN WEIGHT
  boolean f = 0;
  while (f == 0) {
    LoadCell.update();
    if (Serial.available() > 0) {
      m = Serial.parseFloat();
      if (m != 0) {
        Serial.print("Known mass is: ");
        Serial.println(m);
        f = 1;
      }
      else {
        Serial.println("Invalid value");
      }
    }
  }
  float c = LoadCell.getData() / m;
  //Serial.print("Calibration factor is "); Serial.println(c);
  LoadCell.setCalFactor(c);
  f = 0;
}
