# Todo

## 1. Testy sensora i obudowy softwareowej

Priorytet: niski.

Warto dodać testy automatyczne dla logiki `StandPulse.SensorHost`, ale nie blokuje to obecnego etapu pracy.

Zakres, który ma największy sens:

1. test parsera próbek JSON z firmware,
2. test mapowania próbek na `presence`, `absence`, `sensorNotAvailable` i `error`,
3. test stabilizacji stanu przez `stateChangeDelaySeconds`,
4. test timeoutu próbek prowadzącego do `sensorNotAvailable`,
5. test serializacji snapshotu `type/status/lastSampleAt/stableSince`,
6. test kopiowalnego klienta `standpulse-sensor-client.cs` dla `GetStatusAsync` i `WatchAsync`.

Testy powinny opierać się głównie o stub/fake źródło próbek, a realny sensor zostawić do ręcznego smoke testu z COM.

## 2. Konfiguracja Shelly Plug S Gen3 w lokalnym Wi-Fi

Priorytet: średni.

Stan na 2026-05-20:

1. Urządzenie z pudełka to Shelly Plug S Gen3 / MTR Gen3, Type F, z Wi-Fi, Bluetooth i Matter.
2. Do integracji ze `StandPulse.SensorHost` najprostsza ścieżka to lokalne HTTP RPC Shelly, bez Matter i bez chmury.
3. Lokalny panel Shelly załadował się po połączeniu telefonu z siecią `ShellyPlugSG3-...`; telefon miał adres `192.168.33.68`.
4. W panelu użyto `Configure WiFi settings` i zapisano domową sieć Wi-Fi.
5. Shelly pokazał `IPv4 address` = `10.1.1.81`.
6. Po przełączeniu telefonu na domowe Wi-Fi adres `http://10.1.1.81` był nieosiągalny.

Do sprawdzenia przy kolejnym podejściu:

1. czy telefon i Shelly są w tej samej sieci/podsieci, a nie w sieci guest/IoT z izolacją klientów,
2. czy panel Shelly przez AP nadal pokazuje `Wi-Fi 1` jako połączone z adresem `10.1.1.81`,
3. czy router widzi Shelly na liście klientów DHCP i czy da się przypisać mu stały adres,
4. czy działa `http://192.168.33.1/rpc/WiFi.GetStatus` z sieci AP Shelly,
5. czy działa `http://10.1.1.81/rpc/Shelly.GetStatus` z domowego Wi-Fi.
