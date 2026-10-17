# Spaghetti Wires (tytuł roboczy)

Gra na **Ludum Dare 60**, temat: **„Old Tech”**.

> Jesteś operatorem sieci telefonicznej w ciasnej azjatyckiej dzielnicy. Miasto rośnie, kable się psują, a Ty nie masz czasu ich naprawiać. Ciągniesz nowe obok starych… i tak powstaje legendarna plątanina kabli jak w Bangkoku.

---

## 1. Koncepcja

| | |
|---|---|
| **Gatunek** | Puzzle + zarządzanie z elementami „naprawiacza” |
| **Widok** | 3D, kamera izometryczna z obrotem i zoomem |
| **Tempo** | Presja czasu, gra stopniowo przyspiesza |
| **Platforma** | PC (Windows, WebGL) |
| **Silnik** | Unity 6000.3, URP |
| **Sesja** | ok. 15–20 minut |

### Filar projektu: chaos powstaje organicznie
Gracz nie robi bałaganu, bo chce, tylko dlatego, że **w danej chwili to najbardziej opłacalna decyzja**. Naprawa starego kabla z czasem przestaje się opłacać, więc gracz ciągnie nowy obok, a stary zostaje na słupach. Ekonomia gry sama produkuje plątaninę.

### Związek z tematem „Old Tech”
- Miedziane kable, centrala telefoniczna, słupy, puszki rozdzielcze.
- Stare kable **nigdy nie znikają**. Każda decyzja zostawia trwały ślad w krajobrazie miasta.
- Końcowa plątanina to wizualna historia sieci.

---

## 2. Pętla rozgrywki

```
Nowy budynek / zgłoszenie
        │
        ▼
Poprowadź kabel ──► za długi? ──► postaw słup
        │              słup pełny? ──► postaw kolejny słup
        ▼
Mieszkańcy płacą abonament ──► pieniądze
        │
        ▼
Awaria! (kabel gdzieś przestał działać)
        │
        ├─► NAPRAW: szukaj miernikiem → tanio, ale wolno
        └─► WYMIEŃ: nowy kabel obok → szybko, ale drogo,
                    stary zostaje na słupach → chaos rośnie
```

---

## 3. Mechaniki

### 3.1 Model sieci (wzorowany na prawdziwej telefonii)
Każdy abonent ma elektrycznie własną linię, ale fizycznie wiele linii biegnie we wspólnym grubym kablu.

```
CENTRALA ══════ magistrala ══════ [puszka na słupie]
                                   │    │    │
                                 dom A dom B dom C   ← przyłącza
```

| Element | Opis |
|---|---|
| **Centrala** | Korzeń sieci. Stąd wychodzą magistrale. |
| **Magistrala** | Gruby, drogi kabel o ograniczonej pojemności (np. 4–8 abonentów). Biegnie przez słupy. |
| **Puszka rozdzielcza** | Ulepszenie słupa. Zamienia magistralę na przyłącza. |
| **Przyłącze** | Cienki, tani kabel o bardzo krótkim zasięgu (1–2 pola) od puszki do domu. |

### 3.2 Kable
- **Maksymalna długość odcinka** między dwoma punktami zaczepienia. Dłuższa trasa wymaga postawienia słupa.
- Kabel **zwisa** zależnie od długości odcinka.
- Kabel ma **wiek**, a stare kable psują się częściej.
- Stan kabla: **sprawny**, **uszkodzony** (na konkretnym odcinku) lub **martwy** (porzucony: wisi, ale nie przewodzi).

### 3.3 Słupy
- Słup ma **limit kabli**. Liczą się **wszystkie** kable, także martwe.
- Słup blisko limitu jest **przeciążony**, co zwiększa ryzyko awarii kabli na nim.
- Pełny słup wymusza postawienie nowego, więc trasy się wydłużają, a chaos rośnie.

### 3.4 Awarie
Awarie nie są czysto losowe. Decyzje gracza wpływają na ich częstość:
- **wiek kabla**,
- **przeciążony słup**,
- **długi odcinek** (duży zwis),
- **kabel nad ulicą** (może go zahaczyć ciężarówka),
- **zdarzenia**: burza, monsun, fajerwerki (rozszerzenie).

Gracz **nie wie, gdzie** kabel się zepsuł. Wie tylko, kto nie ma sygnału:
- dzwoni **jeden dom**, więc uszkodzone jest pewnie jego przyłącze (krótki odcinek, szybka naprawa),
- dzwoni **cała ulica**, więc padła magistrala (długi kabel, wysoka stawka).

### 3.5 Naprawa albo wymiana: kluczowa decyzja

| | Naprawa | Nowy kabel | Zdjęcie starego |
|---|---|---|---|
| **Pieniądze** | Niski koszt | Średni koszt | Darmowe lub tanie |
| **Czas** | Rośnie z długością kabla | Krótki, stały | Średni |
| **Skutek** | Brak | Stary kabel zostaje i zajmuje miejsce na słupach | Zwalnia miejsce na słupach |

Na początku kable są krótkie, więc naprawa się opłaca. Później awarii jest dużo, kable są długie, a klienci tracą cierpliwość, więc **opłaca się wymiana**. Słupy się zapychają, trzeba stawiać nowe, i tak rośnie chaos.

### 3.6 Miernik (woltomierz)
- Gracz klika miernikiem na słup na trasie kabla i dostaje odpowiedź: **„sygnał jest”** albo **„brak sygnału”**.
- Pozwala to zawęzić obszar awarii o połowę (przeszukiwanie połówkowe).
- Każdy pomiar **zabiera czas**.
- Mądry gracz korzysta też z dedukcji: jeśli kilka domów na jednej magistrali nie ma sygnału, uszkodzenie jest we wspólnym fragmencie.
- Opcjonalne podpowiedzi wizualne: iskry, zwisający kabel, ptak na przewodzie.

### 3.7 Rozwój miasta
- Co pewien czas pojawia się **nowy budynek**, który chce usługi.
- Tempo rośnie: więcej budynków to więcej kabli, a więcej kabli to więcej awarii.

### 3.8 Presja i przegrana
- Każdy budynek bez usługi ma **pasek cierpliwości**.
- Gdy się wyczerpie, mieszkańcy rezygnują z abonamentu i spada **reputacja**.
- **Przegrana:** reputacja spada do zera albo kończą się pieniądze.
- **Wynik:** czas przetrwania, liczba obsłużonych domów, „wskaźnik chaosu” (liczba martwych kabli) i zdjęcie plątaniny na koniec.

### 3.9 Ery technologii (rozszerzenie)
Kolejne typy kabli, z których żaden nie znika:
1. **Telefon** (czarny, cienki)
2. **Telewizja kablowa** (biały koncentryk)
3. **Internet dial-up / ADSL** (szary)

Budynek może chcieć kilku usług naraz, więc na słupach robi się jeszcze tłoczniej.

---

## 4. Sterowanie i kamera

| Akcja | Sterowanie |
|---|---|
| Przesuwanie kamery | WSAD / krawędź ekranu |
| Obrót kamery | Q / E lub prawy przycisk myszy |
| Zoom | Kółko myszy |
| Prowadzenie kabla | Klik na punkt startowy → przeciągnij → puść na punkcie docelowym |
| Wybór narzędzia | Pasek na dole lub klawisze 1–4 |

**Narzędzia:** Kabel, Słup (i puszka), Miernik, Nożyce.

**Czytelność w chaosie (kluczowe):**
- najechanie na budynek podświetla wszystkie jego kable aż do centrali,
- kabel za długi lub słup pełny świeci na czerwono, zanim gracz puści przycisk,
- najechanie na słup pokazuje zajętość (np. `7/8`).

---

## 5. Oprawa

- **Styl:** low-poly, ciasna zabudowa, kolorowe szyldy, klimatyzatory na ścianach, kable wszędzie.
- **Oświetlenie (URP):** zmierzch lub noc, neony szyldów, bloom, ciepłe lampy uliczne.
- **Kable:** generowane proceduralnie ze zwisem (krzywa łańcuchowa), bez fizyki.
- **Dźwięk:** szum miasta, skuter, iskrzenie, sygnał telefonu, dzwonek awarii, klik przy podpinaniu kabla.

---

## 6. Architektura techniczna

### Zasada: model danych jest oddzielony od sceny
Logika sieci to **czyste klasy C#** (bez `MonoBehaviour`). Obiekty na scenie tylko ją **wyświetlają**. Odwołania między danymi są przez **Id (int)**, a nie przez referencje do `GameObject`.

### Dwie warstwy

**Warstwa fizyczna: graf miejsc** (co gdzie wisi)
- `Node`: centrala, słup lub budynek. Ma pozycję na siatce, limit kabli i listę wszystkich przechodzących kabli (także martwych).
- `Cable`: trasa przez kolejne węzły (`List<int> Route`), typ (magistrala lub przyłącze), wiek, indeks uszkodzonego odcinka i flaga „martwy”.

**Warstwa logiczna: drzewo usług** (kto ma sygnał)
- `ServiceNode`: centrala → magistrala → puszka → przyłącze → dom.
- Węzeł ma sygnał, jeśli kabel prowadzący do niego jest sprawny, a rodzic ma sygnał.
- Awaria magistrali automatycznie wyłącza całe poddrzewo.

**Dlaczego dwie warstwy:** logicznie sieć jest drzewem, ale fizycznie jeden słup jest wspólny dla kabli z różnych gałęzi, a martwe kable wiszą na nim dalej, choć nie należą już do drzewa.

### Kluczowe operacje
- **Wymiana kabla:** nowy kabel podpinany pod ten sam `ServiceNode`, stary dostaje `IsDead = true`. Znika z drzewa, ale zostaje w warstwie fizycznej.
- **Nożyce:** usuwają kabel z warstwy fizycznej i zwalniają miejsca na słupach.
- **Miernik:** sygnał w punkcie `i` trasy jest, jeśli rodzic ma sygnał i uszkodzony odcinek leży dalej niż `i`.
- **Stan sygnału** przeliczany tylko przy zdarzeniach (awaria, naprawa, nowy kabel), a nie co klatkę.

### Podział odpowiedzialności

```
┌──────────────────── Czysty C# ────────────────────┐
│  Network: warstwa fizyczna + drzewo usług         │
│  Simulation: awarie, cierpliwość, ekonomia,       │
│              nowe budynki                         │
└──────────▲─────────────────────────┬──────────────┘
  polecenia│                         │zdarzenia
┌──────────┴───────────┐   ┌─────────▼────────────────────┐
│ Input / narzędzia    │   │ Widoki (MonoBehaviour)       │
│ Kabel, Słup, Miernik,│   │ NodeView, CableView          │
│ Nożyce               │   │ (słownik Id → widok)         │
└──────────────────────┘   └──────────────────────────────┘
```

1. Model nie zna Unity, poza typami w rodzaju `Vector2Int`.
2. Widok nigdy nie zmienia modelu bezpośrednio. Input wysyła polecenie, model je waliduje i emituje zdarzenie, a widok się aktualizuje.
3. Logikę sieci da się testować testami EditMode bez uruchamiania sceny.

### Mapa
- Logiczna **siatka** (niewidoczna dla gracza). Upraszcza pozycje słupów i budynków oraz liczenie odległości.

---

## 7. Zakres na jam

### Wersja minimalna (MVP, pierwsze 24–36 h)
- [ ] Mapa na siatce: centrala i kilka budynków
- [ ] Kamera izometryczna: przesuwanie, obrót, zoom
- [ ] Prowadzenie kabli z limitem długości odcinka i zwisem
- [ ] Stawianie słupów z limitem kabli
- [ ] Magistrala, puszka i przyłącza
- [ ] Pojawianie się nowych budynków
- [ ] Awarie, miernik, naprawa i wymiana
- [ ] Pieniądze, pasek cierpliwości, reputacja, przegrana
- [ ] Podświetlanie kabli budynku po najechaniu

### Rozszerzenia (jeśli starczy czasu)
- [ ] Nożyce (zdejmowanie martwych kabli)
- [ ] Ery technologii i nowe typy kabli
- [ ] Zdarzenia: burza, monsun, ciężarówka
- [ ] Oprawa: neony, noc, dźwięk, muzyka
- [ ] Ekran końcowy ze zdjęciem plątaniny i wskaźnikiem chaosu
- [ ] Linia towarzyska (dwa domy na jednym przyłączu, taniej)

---

## 8. Ryzyka

| Ryzyko | Jak ograniczyć |
|---|---|
| Plątanina staje się nieczytelna i frustrująca | Podświetlanie kabli, kolory typów, wyróżnianie uszkodzonych |
| Wymiana zawsze lepsza od naprawy (lub odwrotnie) | Balans kosztów czasu i pieniędzy, testy od pierwszego dnia |
| Za dużo systemów na 72 h | Najpierw MVP, rozszerzenia dopiero potem |
| Prowadzenie kabli w 3D jest niewygodne | Zaczepianie do najbliższego węzła, czytelny podgląd przed puszczeniem |
| Wydajność przy setkach kabli | Model jako listy intów, przeliczanie sygnału tylko przy zdarzeniach |

---

## 9. Otwarte pytania
- Gra bez końca („ile wytrzymasz”) czy kolejne dni lub etapy z celem?
- Czy budynki mogą być łączone łańcuchem, czy zawsze przez puszkę na słupie?
- Ile kosztuje i ile czasu zajmuje każda akcja (do ustalenia w testach)?
- Startujemy w **Compo** (48 h, solo) czy w **Jam** (72 h)? Sprawdzić aktualne zasady LD dotyczące treści generowanych przez AI.
