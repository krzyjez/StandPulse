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
