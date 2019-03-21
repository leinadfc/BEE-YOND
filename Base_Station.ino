//Libraries
#include <SPI.h>
#include <mrf24j.h>
#include <ESP8266WiFi.h>
#include <FirebaseArduino.h>

//PINS SETUP
const int pin_reset = 4;
const int pin_cs = 15; // default CS pin on ATmega8/168/328
const int pin_interrupt = 2; // default interrupt pin on ATmega8/168/328


//INIT OBJECTS
Mrf24j mrf(pin_reset, pin_cs, pin_interrupt);

//GLOBAL VARIABLES
//MRF24J40
long last_time;
long tx_interval = 1000;

//FIREBASE
String Message_Upload;
long int Last_Counter;

//SET FIREBASE AND WIFI AUTHORIZATION DATA
#define FIREBASE_HOST "Database_URL"
#define FIREBASE_AUTH "SECRET_TOKEN"
#define WIFI_SSID "WiFi_NAME"
#define WIFI_PASSWORD "PASSWORD"


void setup() {
  Serial.begin(9600);

  //Setup --------------- WiFi
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  while (WiFi.status() != WL_CONNECTED) {
    Serial.print(".");
    delay(500);
  }
  Serial.println();
  Serial.print("connected: ");
  Serial.println(WiFi.localIP());

  //Setup --------------- FIREBASE
  Firebase.begin(FIREBASE_HOST, FIREBASE_AUTH);
  Last_Counter = Firebase.getString("Counter/Node/cnt/").toInt(); 
  Serial.print("Initial Counter is "); Serial.println(Last_Counter); 
  if (!Firebase.success())
  {
    Serial.println("Folder contains no data");
  }

  //Setup --------------- MRF24J40
  mrf.reset();
  mrf.init();

  mrf.set_pan(0xcafe);
  // This is _our_ address
  mrf.address16_write(0x6001);

  // uncomment if you want to receive any packet on this channel
  //mrf.set_promiscuous(true);

  // uncomment if you want to enable PA/LNA external control
  //mrf.set_palna(true);

  attachInterrupt(pin_interrupt, interrupt_routine, CHANGE); // interrupt 0 equivalent to pin 2(INT0) on ATmega8/168/328
  last_time = millis();
  interrupts();
}



void loop() {
    mrf.check_flags(&handle_rx, &handle_tx);
    
}

void handle_rx() {

    Serial.print("received a packet ");Serial.print(mrf.get_rxinfo()->frame_length, DEC);Serial.println(" bytes long");
    
    if(mrf.get_bufferPHY()){
      Serial.println("Packet data (PHY Payload):");
      for (int i = 0; i < mrf.get_rxinfo()->frame_length; i++) {
          Serial.print(mrf.get_rxbuf()[i]);
      }
    }
    

    char get_Message[100];
    
    for(int i = 0; i < mrf.rx_datalength(); i++){
      Serial.println(char(mrf.get_rxinfo()->rx_data[i]));
      char test = char(mrf.get_rxinfo()->rx_data[i]); 
      get_Message[i] = test; 
    }
    Serial.println(get_Message); 

 

    //increase COUNTER folder
    Last_Counter++;
    Message_Upload = String(get_Message);
    String counter = String(Last_Counter);
    
    Serial.print("Message: "); Serial.println(Message_Upload); 
    String URL_address = "Data/" + counter + "/Data_Package";  
    Serial.println(URL_address); 
    //UPLOAD FIREBASE
    Firebase.setString("Counter/Node/cnt", counter);
    Firebase.setString(URL_address, Message_Upload);

    /*
    Serial.print("\r\nLQI/RSSI=");
    Serial.print(mrf.get_rxinfo()->lqi, DEC);
    Serial.print("/");
    Serial.println(mrf.get_rxinfo()->rssi, DEC);
    */
}

void handle_tx() {
    //NOT SENDING DATA RIGHT NOW
    /*
    if (mrf.get_txinfo()->tx_ok) {
        Serial.println("TX went ok, got ack");
    } else {
        Serial.print("TX failed after ");Serial.print(mrf.get_txinfo()->retries);Serial.println(" retries\n");
    }
    */
}


//////////////////////////////////////////////////////////////////////////////
///////////////////////////////////INTERRUPTS///////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////

void interrupt_routine() {
    mrf.interrupt_handler(); // mrf24 object interrupt routine
}
