# Spaghetti Wires: plan działania

Kolejność prac nad grą na podstawie [GDD.md](GDD.md) i [TECH.md](TECH.md). Plan jest ułożony tak, żeby jak najszybciej mieć **grywalną pętlę**, a dopiero potem dokładać awarie, rozszerzenia i oprawę. Każdy etap kończy się czymś, co da się uruchomić i sprawdzić.

---

## Stan obecny

- ✅ GDD i TECH (`develop`)
- ✅ Kamera izometryczna `IsometricCameraController` (`develop`)
- ✅ Model drewnianego słupa, używany jako słup w scenie gry
- ✅ Rdzeń sieci (etap 1), przepisany na model z TECH
- ✅ Scena gry, widoki i narzędzia (etapy 3–4). Akcje gracza obsługuje tymczasowy `SandboxBuildActions`, który prowadzi już prawdziwą sieć `PhoneNetwork`
- ✅ Symulacja rozmów z widocznymi rozmowami (etap 5)
- ✅ UI: pasek narzędzi, HUD, okienko zajętości słupa, ekran przegranej
- ✅ Ekonomia: pieniądze, reputacja, przegrana i płacenie za budowę (część etapu 6)
- ✅ Wyniki rozmów podpięte do ekonomii (etap 6): kamień B w kodzie zamknięty, do sprawdzenia w Unity i do balansu
- ⚠️ Następne kroki: sprawdzenie w Unity, balans liczb, build WebGL, potem reguły budowy zamiast sandboxa (etap 2) i awarie (etap 7)
- ✅ TECH zawiera √(liczba żył) miejsc na słupie i dom przenoszący jedną rozmowę

---

## Mapa zależności

```
0. Porządki
   │
1. Rdzeń sieci (C#, testy) ──────────────┐
   │                                     │
2. Reguły budowy                         │
   │                                     ▼
3. Scena i widoki ──► 4. Input i narzędzia      ← KAMIEŃ A: da się budować sieć
                          │
               5. Symulacja rozmów
                          │
               6. Ekonomia i zadowolenie         ← KAMIEŃ B: grywalna pętla
                          │
         7. Awarie ──► 8. Rozwój miasta i centrala ← KAMIEŃ C: pełne MVP
                          │
         9. Rozszerzenia   10. Oprawa   11. Balans
                          │
                   12. Build i publikacja

Równolegle od etapu 3: modele, dźwięk, UI
```

---

## Etapy

### 0. Porządki (ok. 1 h)
- [x] Uzupełnić TECH o √(liczba żył) miejsc na słupie i dom przenoszący jedną rozmowę.
- [x] Ustalić **skalę**: rozmiar pola siatki w metrach i wielkość domu względem słupa. Pole 4 m, dom zajmuje 1 pole (TECH 1).
- [x] Ustalić strukturę folderów i assembly: `Networking`, `Building` (reguły), `Simulation`, `Views`, `Input`, `UI` (TECH 1).
- [x] Zdecydować, co z gałęziami `Network` (do przepisania) i `WoodenPole` (do włączenia). Obie włączone do `develop`.

### 1. Rdzeń sieci: czysty C#, testy EditMode (ok. 4–6 h)
Fundament wszystkiego, więc warto zrobić go porządnie z testami.

> Kod: `Assets/_Project/Scripts/Networking/`, testy w `Assets/_Project/Tests/EditMode/Networking/`. Opis w TECH 9.
- [x] **Struktura:** `Node` (Id, typ) i niezmienny `Cable` (A, B, liczba żył, długość), indeks węzeł → kable, zdarzenia dodania i usunięcia.
- [x] **Stan techniczny:** słownik `cableId → uszkodzone żyły`, operacja uszkodzenia, pomiar.
- [x] **Ruch:**
  - [x] `Call` z trasą,
  - [x] wyszukiwanie najkrótszej trasy (Dijkstra) po kablach z wolnymi żyłami,
  - [x] dom przenosi naraz jedną rozmowę,
  - [x] zajmowanie i zwalnianie żył,
  - [x] zrywanie najnowszych rozmów przy uszkodzeniu lub usunięciu kabla.
- [x] **Fasada** `PhoneNetwork` jako jedyny punkt wejścia.

### 2. Reguły budowy (ok. 2–3 h)
- [ ] Mapa: `NodeId → pole siatki` oraz klasa słupa.
- [ ] Walidacja: zasięg kabla, miejsca na słupie (√ liczby żył), pieniądze.
  - Tymczasowo: zasięg, miejsca i kolizje z budynkami w `SandboxBuildActions` (już na `PhoneNetwork`, mapa `NodeId → pole` też jest w sandboxie), pieniądze w `PaidBuildActions`. Brakuje osobnej warstwy reguł i klas słupów.
- [ ] Podgląd walidacji bez zmiany stanu (do czerwonego lub zielonego kabla).
- [ ] Wszystkie liczby w jednym `ScriptableObject` z ustawieniami gry.

### 3. Scena i widoki (ok. 4–5 h)
- [x] Podłoże z niewidoczną siatką i scena gry z kamerą.
- [x] `NodeView`: dom (na razie prosty sześcian), słup (prefab `WoodenPole`), centrala.
- [x] `CableView`: kabel ze zwisem (krzywa łańcuchowa) od punktu zaczepienia do punktu zaczepienia.
- [ ] Tworzenie i usuwanie widoków w reakcji na zdarzenia sieci, słownik `Id → widok`.
  - Słownik `Id → widok` jest (`ViewRegistry`) i Id pochodzą z `PhoneNetwork`, ale widoki tworzy sandbox wprost, a nie w reakcji na zdarzenia sieci.

### 4. Input i narzędzia (ok. 4–5 h)
- [x] Pasek narzędzi i klawisze 1–5 (`ToolbarView`, `ToolController`).
- [x] **Słup:** stawianie na wolnym polu.
- [x] **Kabel:** przeciągnij od węzła do węzła, z podglądem i kolorem poprawności.
- [x] **Nożyce:** usuń kabel. **Miernik:** pokaż sprawne żyły (na razie z sandboxa, bez awarii).
- [x] Wybieranie obiektów myszą (raycast → `Id`) i podświetlanie pod kursorem (`ScenePointer`).
- [x] Okienko nad słupem z zajętością miejsc, np. `7/12` (`PoleTooltipView`). Dane na razie z sandboxa przez `IBuildActions.TryGetSlotUsage`.
- [x] Blokady narzędzi: na starcie tylko Kabel, reszta przez `ToolController.Unlock`. Harmonogram odblokowań w `GameProgression`.

> **Kamień milowy A:** można stawiać słupy i łączyć domy kablami. Nic jeszcze nie dzwoni.
> Osiągnięty: budowa działa na prawdziwej sieci przez sandbox. Porządki techniczne (reguły zamiast sandboxa, widoki ze zdarzeń sieci) nie blokują kolejnych etapów.

### 5. Symulacja rozmów (ok. 4 h)
- [x] Generator rozmów: kto do kogo dzwoni i jak często (rośnie z czasem i liczbą domów). `CallSimulation`, `CallSettings`, `CallSimulationRunner`.
- [x] Czas trwania rozmowy, rozpoczynanie i kończenie przez warstwę ruchu. Wyniki jako zdarzenia: `CallConnected`, `CallCompleted`, `CallFailed` (zajęte, brak drogi), `CallDropped`.
- [x] **Widoczność:** świecące kable trwającej rozmowy, ikonka „zajęte” nad domem (`CallTrafficView`, `CallIndicatorsView`).
- [x] Wymuszanie rozmów do testów (`CallDebugInput`, tylko edytor i development build).

### 6. Ekonomia i zadowolenie (ok. 3 h)
- [x] Model pieniędzy i reputacji z wykrywaniem przegranej (`Economy` w `Scripts/Simulation/`).
- [x] Pieniądze za udane rozmowy, koszty budowy.
  - [x] Koszty budowy: słup za stałą cenę, kabel i magistrala od długości (`BuildPrices`, `PaidBuildActions`).
  - [x] Ceny widoczne przed zakupem: cennik przy pasku narzędzi i cena nad podglądem kabla (`CablePriceView`).
  - [x] Pieniądze za udane rozmowy (`CallPayouts`, na scenie `CallEconomyLink`). Płatne po normalnym zakończeniu, zerwana rozmowa nic nie daje.
- [x] Zadowolenie i reputacja: kary za „zajęte” i zerwane rozmowy. Osobna wartość dla braku trasy, zajętego odbiorcy, zajętego dzwoniącego i zerwania, mały plus za udaną rozmowę (`CallPayoutSettings`).
- [x] Po przegranej symulacja rozmów staje razem z zegarem gry.
- [x] Przegrana (reputacja albo pieniądze na zero) i ekran przegranej z restartem.
  - [x] Widok ekranu przegranej z czasem i restartem (`GameOverView.Show(czas, powód)`).
  - [x] Wykrywanie przegranej w `Economy` i pokazanie ekranu przez `GameSession`.
- [x] HUD: pieniądze, reputacja, czas.
  - [x] Widok HUD: pieniądze i reputacja w lewym górnym rogu, czas w prawym (`HudView.SetMoney`, `SetReputation`, `SetTime`).
  - [x] Podpięcie ekonomii i zegara gry (`GameSession`).

> **Kamień milowy B:** grywalna pętla. Budujesz, ludzie dzwonią, zarabiasz albo przegrywasz. **Od tego momentu warto testować na innych osobach.**

### 7. Awarie (ok. 3 h)
- [ ] Symulacja awarii (na MVP tylko całkowite): wiek kabla, przeciążony słup, długi odcinek.
- [ ] Skutki są już w rdzeniu sieci (zrywanie rozmów, omijanie kabla).
- [ ] Opcjonalne podpowiedzi wizualne: iskry, opadający kabel.

### 8. Rozwój miasta i centrala (ok. 3 h)
- [x] Generator nowych budynków na wolnych polach, z rosnącym tempem (`GameProgression`, na razie domy stawia sandbox przez `IHouseSpawner`).
- [x] Startowa mapa: dwa domy na stałych polach w zasięgu kabla, bez centrali.
- [ ] Budowanie centrali przez gracza (super-słup bez limitu, drogi).

> **Kamień milowy C:** pełne MVP z GDD, czyli gra bez końca z rosnącym tempem.

### 9. Rozszerzenia (według priorytetu, ile starczy czasu)
1. [ ] **Magistrale** (4, 9, 16 żył), √(liczba żył) miejsc na słupie, częściowe uszkodzenia. Wysoki priorytet, bo dodaje dużo głębi małym kosztem: rdzeń sieci już to obsługuje.
2. [ ] **Klasy słupów** (mały, średni, duży).
3. [ ] **Jakość rozmowy** zależna od długości trasy.
4. [ ] **Ekran końcowy** ze zdjęciem plątaniny i wskaźnikiem chaosu.
5. [ ] Zdarzenia (burza, ciężarówka).
6. [ ] Ery technologii: najpewniej do wycięcia na jam.

### 10. Oprawa (równolegle od etapu 3)
- [ ] **Modele:** domy w kilku wariantach, słupy trzech klas, centrala. Z prymitywów albo z paczek CC0.
- [ ] **Klimat:** zmierzch lub noc, neony, bloom (URP), lampy.
- [ ] **Dźwięk:** dzwonek, sygnał zajętości, iskrzenie, klik wtyczki, tło miasta, muzyka.
- [ ] **UI:** menu główne, pauza, krótkie podpowiedzi na start (zamiast samouczka).

### 11. Balans i testy (ciągle, od kamienia B)
- [ ] Koszty, zasięgi, limity słupów, częstość rozmów i awarii.
- [ ] Najważniejsze pytania z ryzyk w GDD: czy usuwanie kabli się opłaca, czy magistrale nie dominują, czy chaos naprawdę powstaje.

### 12. Build i publikacja (ok. 3–4 h, nie zostawiać na ostatnią chwilę)
- [ ] **Build WebGL** sprawdzić już przy kamieniu B (WebGL potrafi zaskoczyć).
- [ ] Build Windows.
- [ ] Strona na ldjam.com: opis, zrzuty ekranu, lista źródeł zasobów, informacja o AI.

---

## Podział 72 godzin (orientacyjnie)

| Czas | Cel |
|---|---|
| 0–12 h | Etapy 0–4, **kamień A** |
| 12–24 h | Etapy 5–6, **kamień B**, pierwszy build WebGL |
| 24–36 h | Etapy 7–8, **kamień C (MVP)** |
| 36–56 h | Rozszerzenia (magistrale, klasy słupów), oprawa, balans |
| 56–66 h | Polerowanie, dźwięk, menu, poprawki z testów |
| 66–72 h | Build, strona, zapas na niespodzianki |

Część prac da się zrobić **przed startem jamu**, np. etap 1 jako ogólny system sieci. Trzeba sprawdzić w zasadach Jam, co wolno przygotować wcześniej (zwykle kod bazowy jest dozwolony, ale musi być publiczny lub zgłoszony).

---

## Co wyciąć, jeśli zabraknie czasu

W tej kolejności:
1. ery technologii,
2. zdarzenia pogodowe,
3. jakość rozmowy,
4. klasy słupów,
5. ekran ze zdjęciem plątaniny.

**Magistral nie wycinamy**, bo dają główną decyzję „zostawić czy usunąć”.
