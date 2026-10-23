#if defined(ARDUINO_SEEED_XIAO_NRF52840_SENSE) || defined(ARDUINO_SEEED_XIAO_NRF52840)
#error "This sketch is intended for XIAO ESP32-C3 with 24GHz mmWave for XIAO."
#endif

// Radar 24GHz mmWave for XIAO nadaje ramki zgodne z HLK-LD2410. Parsujemy je sami,
// bo biblioteka mmwave_for_xiao udostępnia tylko jedną odległość (detection distance),
// a do diagnozy obecności potrzebne są osobno odległości i energie celu ruchomego i nieruchomego.
// Seeed mmWave for XIAO używa D2/D3 jako UART radaru (RX, TX z perspektywy XIAO).
HardwareSerial radarSerial(1);

const unsigned long REPORT_INTERVAL_MS = 200;
const unsigned long RADAR_SILENCE_MS = 1000;
const size_t MAX_FRAME_SIZE = 64;
const uint8_t FRAME_HEADER[] = {0xF4, 0xF3, 0xF2, 0xF1};
const uint8_t FRAME_TAIL[] = {0xF8, 0xF7, 0xF6, 0xF5};
// Typ, 0xAA, stan celu i trzy pola odległości z energiami.
const size_t MIN_PAYLOAD_SIZE = 11;

struct RadarFrame {
  uint8_t mode;
  uint8_t targetState;
  uint16_t movingDistanceCm;
  uint8_t movingEnergy;
  uint16_t staticDistanceCm;
  uint8_t staticEnergy;
  uint16_t detectionDistanceCm;
};

uint8_t frameBuffer[MAX_FRAME_SIZE];
size_t frameLength = 0;
RadarFrame lastFrame;
bool hasNewFrame = false;
unsigned long lastFrameAt = 0;
unsigned long lastReportAt = 0;
unsigned long invalidFrames = 0;

// Zamienia kod stanu celu z ramki na etykietę zgodną z wcześniejszym firmware.
const char *targetStatusToString(uint8_t state) {
  switch (state) {
    case 0x00:
      return "NoTarget";
    case 0x01:
      return "MovingTarget";
    case 0x02:
      return "StaticTarget";
    case 0x03:
      return "BothTargets";
    default:
      return "Unknown";
  }
}

// Obecność oznacza dowolny wykryty cel; próg odległości stosuje dopiero host.
bool isPresent(uint8_t state) {
  return state >= 0x01 && state <= 0x03;
}

// Czyta 16-bitową liczbę little-endian z bufora ramki.
uint16_t readUInt16(const uint8_t *data) {
  return data[0] | (data[1] << 8);
}

// Sprawdza ogon i nagłówek danych celu, a potem zapisuje ramkę jako ostatni odczyt radaru.
void parseFrame(size_t length) {
  const uint8_t *tail = frameBuffer + length - sizeof(FRAME_TAIL);
  if (memcmp(tail, FRAME_TAIL, sizeof(FRAME_TAIL)) != 0) {
    invalidFrames++;
    return;
  }

  const uint8_t *payload = frameBuffer + 6;
  size_t payloadLength = readUInt16(frameBuffer + 4);
  if (payloadLength < MIN_PAYLOAD_SIZE || payload[1] != 0xAA) {
    invalidFrames++;
    return;
  }

  lastFrame.mode = payload[0];
  lastFrame.targetState = payload[2];
  lastFrame.movingDistanceCm = readUInt16(payload + 3);
  lastFrame.movingEnergy = payload[5];
  lastFrame.staticDistanceCm = readUInt16(payload + 6);
  lastFrame.staticEnergy = payload[8];
  lastFrame.detectionDistanceCm = readUInt16(payload + 9);
  hasNewFrame = true;
  lastFrameAt = millis();
}

// Składa bajty z UART radaru w ramki: synchronizacja na nagłówku, długość z pola ramki.
void readRadar() {
  while (radarSerial.available()) {
    uint8_t value = radarSerial.read();

    if (frameLength < sizeof(FRAME_HEADER)) {
      if (value == FRAME_HEADER[frameLength]) {
        frameBuffer[frameLength++] = value;
      } else {
        frameLength = value == FRAME_HEADER[0] ? 1 : 0;
        frameBuffer[0] = value;
      }
      continue;
    }

    frameBuffer[frameLength++] = value;
    if (frameLength < 6) {
      continue;
    }

    size_t expectedLength = 6 + readUInt16(frameBuffer + 4) + sizeof(FRAME_TAIL);
    if (expectedLength > MAX_FRAME_SIZE) {
      invalidFrames++;
      frameLength = 0;
      continue;
    }

    if (frameLength == expectedLength) {
      parseFrame(frameLength);
      frameLength = 0;
    }
  }
}

// Wysyła ostatnią ramkę jako jedną linię JSON do PC.
void reportFrame() {
  char line[256];
  snprintf(
    line,
    sizeof(line),
    "{\"present\":%s,\"targetStatus\":\"%s\",\"distanceCm\":%u,\"movingDistanceCm\":%u,\"movingEnergy\":%u,\"staticDistanceCm\":%u,\"staticEnergy\":%u,\"radarMode\":%u}",
    isPresent(lastFrame.targetState) ? "true" : "false",
    targetStatusToString(lastFrame.targetState),
    lastFrame.detectionDistanceCm,
    lastFrame.movingDistanceCm,
    lastFrame.movingEnergy,
    lastFrame.staticDistanceCm,
    lastFrame.staticEnergy,
    lastFrame.mode
  );
  Serial.println(line);
}

// Zgłasza hostowi, że radar przestał dostarczać poprawne ramki.
void reportRadarSilence(unsigned long now) {
  char line[160];
  snprintf(
    line,
    sizeof(line),
    "{\"event\":\"radar_read_error\",\"millis\":%lu,\"silenceMs\":%lu,\"invalidFrames\":%lu}",
    now,
    now - lastFrameAt,
    invalidFrames
  );
  Serial.println(line);
}

void setup() {
  radarSerial.begin(256000, SERIAL_8N1, D2, D3);
  Serial.begin(115200);
  delay(500);

  Serial.println("{\"event\":\"standpulse_sensor_starting\",\"radarBaud\":256000}");
}

void loop() {
  readRadar();

  unsigned long now = millis();
  if (now - lastReportAt < REPORT_INTERVAL_MS) {
    return;
  }
  lastReportAt = now;

  if (hasNewFrame) {
    reportFrame();
    hasNewFrame = false;
  } else if (now - lastFrameAt > RADAR_SILENCE_MS) {
    reportRadarSilence(now);
  }
}
