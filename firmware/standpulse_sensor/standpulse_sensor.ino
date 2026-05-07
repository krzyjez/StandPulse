#if defined(ARDUINO_SEEED_XIAO_NRF52840_SENSE) || defined(ARDUINO_SEEED_XIAO_NRF52840)
#error "This sketch is intended for XIAO ESP32-C3 with 24GHz mmWave for XIAO."
#endif

#include <mmwave_for_xiao.h>

// Seeed mmWave for XIAO uses D2/D3 for the radar UART.
// The constructor arguments are RX, TX from the XIAO perspective.
HardwareSerial radarSerial(1);
Seeed_HSP24 radar(radarSerial);

Seeed_HSP24::RadarStatus radarStatus;

const char *targetStatusToString(Seeed_HSP24::TargetStatus status) {
  switch (status) {
    case Seeed_HSP24::TargetStatus::NoTarget:
      return "NoTarget";
    case Seeed_HSP24::TargetStatus::MovingTarget:
      return "MovingTarget";
    case Seeed_HSP24::TargetStatus::StaticTarget:
      return "StaticTarget";
    case Seeed_HSP24::TargetStatus::BothTargets:
      return "BothTargets";
    case Seeed_HSP24::TargetStatus::ErrorFrame:
      return "ErrorFrame";
    default:
      return "Unknown";
  }
}

bool isPresent(Seeed_HSP24::TargetStatus status) {
  return status == Seeed_HSP24::TargetStatus::MovingTarget ||
         status == Seeed_HSP24::TargetStatus::StaticTarget ||
         status == Seeed_HSP24::TargetStatus::BothTargets;
}

void setup() {
  radarSerial.begin(256000, SERIAL_8N1, D2, D3);
  Serial.begin(115200);
  delay(500);

  Serial.println("{\"event\":\"standpulse_sensor_starting\",\"radarBaud\":256000}");
}

void loop() {
  int retryCount = 0;
  const int maxRetries = 10;

  do {
    radarStatus = radar.getStatus();
    retryCount++;
  } while (radarStatus.targetStatus == Seeed_HSP24::TargetStatus::ErrorFrame &&
           retryCount < maxRetries);

  if (radarStatus.targetStatus != Seeed_HSP24::TargetStatus::ErrorFrame) {
    char line[160];
    snprintf(
      line,
      sizeof(line),
      "{\"present\":%s,\"targetStatus\":\"%s\",\"distanceMm\":%d,\"radarMode\":%d}",
      isPresent(radarStatus.targetStatus) ? "true" : "false",
      targetStatusToString(radarStatus.targetStatus),
      radarStatus.distance,
      radarStatus.radarMode
    );
    Serial.println(line);
  } else {
    char line[80];
    snprintf(line, sizeof(line), "{\"event\":\"radar_read_error\",\"millis\":%lu}", millis());
    Serial.println(line);
  }

  delay(200);
}
