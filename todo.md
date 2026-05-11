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

Wnioski z uruchomienia `StandPulse.SensorHost` z autostartu Windows w trybie debug:

1. Host działa i odpowiada po named pipe, a stan stabilizuje się jako `Presence`.
2. Na starcie host loguje błędy parsowania dla nie-JSON-owych linii z ESP32/boot-logu oraz czasem dla uciętej pierwszej próbki. Warto później rozważyć ciche ignorowanie takich linii, żeby debug log nie wyglądał jak realna awaria.
3. W starszym logu pojawiały się pojedyncze `radar_read_error` z firmware, ale licznik zostawał na `1/3`, bez przejścia hosta w stan błędu.
4. Debug log szybko dobija do limitu `102400` bajtów, bo zapisuje pełne `RAW` dla każdej próbki. Warto rozważyć rzadsze lub bardziej zwarte logowanie próbek.

Kolejny krok dla tej zmiany:

1. Opublikować poprawiony `StandPulse.SensorHost` do `bin/StandPulse.SensorHost`.
2. Zrestartować działający proces hosta albo Windows autostart i zostawić jeszcze tryb `debug` na jedną obserwację startu.
3. Sprawdzić nowy log: startowy boot-log ESP32 i ucięte linie nie powinny już pojawiać się jako operacyjne `Błąd parsowania próbki`.
4. Jeśli po starcie log jest czysty i `StandPulse.SensorProbe get` zwraca poprawny status, przełączyć autostart z `debug` na `run`.
