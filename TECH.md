# Spaghetti Wires: dokumentacja techniczna

Architektura i decyzje programistyczne. Design gry jest w [GDD.md](GDD.md).

---

## 1. Środowisko

| | |
|---|---|
| **Silnik** | Unity 6000.3, URP |
| **Kod gry** | `Assets/_Project/Scripts/` |
| **Testy** | `Assets/_Project/Tests/EditMode/` (Unity Test Framework, NUnit) |
| **Assembly** | Każdy system logiki ma własny `.asmdef`. Testy mają osobne assembly tylko dla edytora. |

### Foldery i assembly
Każdy folder w `Assets/_Project/Scripts/` to osobne assembly `SpaghettiWires.<Nazwa>` i namespace o tej samej nazwie. Foldery tworzymy dopiero wtedy, gdy powstaje w nich pierwszy kod.

| Folder | Zawartość | Zależy od Unity? |
|---|---|---|
| `Networking` | Sieć: struktura, stan techniczny, ruch, fasada (4) | Nie |
| `Building` | Reguły budowy i mapa (5) | Minimalnie |
| `Simulation` | Rozmowy w czasie, awarie, ekonomia, rozwój miasta (6) | Nie |
| `Views` | `NodeView`, `CableView` i inne widoki (7) | Tak |
| `Input` | Narzędzia gracza, wybieranie obiektów myszą | Tak |
| `UI` | HUD, menu, ekrany | Tak |
| `CameraControl` | Kamera izometryczna | Tak |
| `Game` | Składanie systemów na scenie, np. `GameSession` (ekonomia + zegar + UI) | Tak |

Testy: `Assets/_Project/Tests/EditMode/<Nazwa>/`, jedno assembly `SpaghettiWires.Tests.EditMode`.

### Skala
- **1 jednostka Unity = 1 m.**
- **Pole siatki: 4 × 4 m.** Siatka jest logiczna i niewidoczna dla gracza.
- **Dom zajmuje 1 pole** (4 × 4 m, wysokość ok. 6–10 m). Słup stoi na 1 polu i ma ok. 8 m wysokości.
- Odległości w regułach (np. zasięg kabla) podajemy w polach, a długość kabla (`Length`) w metrach.

---

## 2. Zasady ogólne

1. **Model danych jest oddzielony od sceny.** Logika gry to czyste klasy C# (bez `MonoBehaviour`). Obiekty na scenie tylko ją **wyświetlają**.
2. **Odwołania przez Id**, a nie przez referencje do `GameObject` ani do innych obiektów modelu. Każdy rodzaj Id to osobna struktura (`NodeId`, `CableId`, `CallId`) z `int` w środku, więc nie da się ich pomylić ze sobą ani z liczbą.
3. **Widok nigdy nie zmienia modelu bezpośrednio.** Input wysyła polecenie, model je waliduje i emituje zdarzenie, a widok się aktualizuje.
4. **Bez obliczeń co klatkę, jeśli nie trzeba.** Trasa rozmowy liczona jest raz, przy jej rozpoczęciu.
5. **Logikę testujemy testami EditMode**, bez uruchamiania sceny.
6. **Bez abstrakcji na zapas.** To gra na jam: kilka prostych klas zamiast frameworków.

---

## 3. Warstwy

```
┌──────────────────────── Input / narzędzia gracza ───────────────────────┐
│  Kabel, Magistrala, Słup, Miernik (sprawdź stan), Nożyce (usuń)         │
└───────────────────────────────────┬─────────────────────────────────────┘
                                    ▼ polecenia
┌──────────────────────── Reguły (budowa) ────────────────────────────────┐
│  • Mapa: NodeId → pozycja na siatce, klasa słupa                        │
│  • Odległość ≤ limit? Słup ma miejsce? Gracza na to stać?               │
│  • Dopiero gdy wszystko OK → wywołanie Sieci                            │
└───────────────────────────────────┬─────────────────────────────────────┘
                                    ▼
┌──────────────────────── Sieć ───────────────────────────────────────────┐
│  • Struktura: węzły i kable (niezmienne po położeniu)                   │
│  • Stan techniczny: uszkodzone żyły                                     │
│  • Ruch: rozmowy, zajęte żyły, wyszukiwanie trasy                       │
│  NIE WIE: o pozycjach, limitach słupów, pieniądzach, czasie             │
└───────────────────────────────────┬─────────────────────────────────────┘
                                    │ zdarzenia
        ┌───────────────────────────┼──────────────────────────────┐
        ▼                           ▼                              ▼
┌─────────────────────┐  ┌────────────────────────────┐  ┌──────────────────────┐
│ Symulacja           │  │ Widoki (MonoBehaviour)     │  │ UI                   │
│ rozmowy w czasie,   │  │ NodeView, CableView        │  │ pieniądze, reputacja,│
│ awarie, zadowolenie,│  │ słownik Id → widok         │  │ zgłoszenia           │
│ ekonomia, budynki   │  │                            │  │                      │
└─────────────────────┘  └────────────────────────────┘  └──────────────────────┘
```

| Warstwa | Odpowiada za | Zależy od Unity? |
|---|---|---|
| **Sieć** | Struktura, stan techniczny i ruch (4.1) | Nie (`noEngineReferences: true`) |
| **Reguły** | Walidacja akcji gracza: odległości, limity słupów, koszty | Minimalnie (np. `Vector2Int`) |
| **Symulacja** | Upływ czasu: kiedy kto dzwoni, jak długo, awarie, zadowolenie, ekonomia, nowe budynki | Nie |
| **Widoki, Input, UI** | Wyświetlanie i obsługa gracza | Tak |

---

## 4. Sieć

### 4.1 Trzy warstwy informacji o kablu
Informacje o kablu zmieniają się w różnym tempie, z różnych powodów i są używane przez różne systemy, więc trzymamy je w trzech osobnych strukturach połączonych **Id kabla** (podobnie jak komponenty w ECS):

| Warstwa | Dane | Kiedy się zmienia | Kto zmienia |
|---|---|---|---|
| **Struktura** | węzły; kabel: A, B, liczba żył, długość | Przy położeniu i usunięciu kabla. Poza tym nigdy. | Gracz (przez reguły) |
| **Stan techniczny** | liczba uszkodzonych żył | Przy awariach (tylko rośnie) | Symulacja awarii |
| **Ruch** | trwające rozmowy, zajęte żyły, zajęte domy | Ciągle, kilka razy na sekundę | Symulacja rozmów |

```
┌─ Struktura (CableNetwork) ─────────┐   kable niezmienne po położeniu
│  Node:  Id, Kind                   │
│  Cable: Id, A, B, Pairs, Length    │
└────────────────────────────────────┘
        ▲ czytają
┌─ Stan techniczny (CableCondition) ─┐   słownik cableId → liczba uszkodzonych żył
│                                    │   (brak wpisu = 0)
└────────────────────────────────────┘
        ▲ czyta
┌─ Ruch (Traffic) ───────────────────┐
│  trwające rozmowy (Call + trasa)   │   źródło prawdy
│  zajęte żyły na kablu, zajęte domy │   indeksy wyliczane z rozmów
│  wyszukiwanie trasy                │
└────────────────────────────────────┘
```

- **Zajęte żyły to nie fakt o kablu**, tylko wynik tego, jakie rozmowy trwają. Źródłem prawdy są rozmowy, a liczba zajętych żył to indeks utrzymywany przez warstwę ruchu.
- **Wyszukiwanie trasy jest w warstwie ruchu**, bo tylko ona potrzebuje wszystkich trzech informacji. Struktura i stan techniczny nic nie wiedzą o rozmowach.
- **Synchronizacja cyklu życia przez zdarzenia:** struktura emituje „kabel usunięty”, a stan i ruch sprzątają po sobie (ruch zrywa rozmowy na tym kablu). Stan emituje „żyły uszkodzone”, a ruch zrywa nadmiarowe rozmowy.
- Na jam wszystkie trzy warstwy trzyma jedna klasa-fasada (np. `PhoneNetwork`), która jest jedynym punktem wejścia dla reguł i symulacji.

### 4.2 Elementy

| Element | Warstwa | Dane |
|---|---|---|
| **Węzeł** (`Node`) | Struktura | `Id`, `Kind` (dom, słup, centrala). Jeden węzeł na obiekt na mapie. |
| **Kabel** (`Cable`) | Struktura | `Id`, węzeł A, węzeł B, `Pairs` (liczba żył), `Length`. **Niezmienny.** |
| **Uszkodzenia** | Stan techniczny | `cableId → liczba uszkodzonych żył` |
| **Rozmowa** (`Call`) | Ruch | `Id`, węzeł początkowy, węzeł docelowy, trasa (lista kabli), kolejność rozpoczęcia |

- Węzły są **równoważne**: płaski słownik, bez hierarchii i bez wewnętrznych złączy.
- Każdy słup i centrala **przełącza rozmowy** między wszystkimi swoimi kablami. Nie modelujemy, co jest z czym połączone w środku.
- Dom też przekazuje rozmowy, ale ma **jedną linię**: naraz uczestniczy w najwyżej jednej rozmowie, swojej albo cudzej (przechodzącej). Słup i centrala nie mają limitu rozmów, ogranicza je tylko liczba wolnych żył.
- Węzły **nie znają** kabli. Struktura utrzymuje wewnętrzny indeks „węzeł → kable” wyłącznie do szybkiego przeszukiwania grafu.
- Magistrala to po prostu kabel z `Pairs > 1`. Zwykły kabel ma `Pairs = 1`.
- **Liczba uszkodzonych żył wystarczy.** Nie pamiętamy, które żyły padły, bo wszystkie są równoważne. Zwykły kabel jest albo sprawny (0), albo zerwany (1).
- Sieć **nie zna** pozycji ani limitów słupów. `Length` to waga ustawiana przez warstwę reguł przy kładzeniu kabla, na podstawie pozycji węzłów. Jest w strukturze, bo tak jak `Pairs` ustala się ją tylko przy położeniu kabla. Korzysta z niej wyszukiwanie trasy (w warstwie ruchu) i jakość rozmowy.

### 4.3 Wyszukiwanie trasy
- Graf: węzły to wierzchołki, kable to krawędzie.
- **Wolne żyły** = `Pairs − uszkodzone − zajęte`. Kabel jest **dostępny**, jeśli ma co najmniej jedną wolną żyłę.
- Rozmowa szuka **najkrótszej trasy** (suma `Length`, algorytm Dijkstry) po dostępnych kablach.
- **Bez limitu długości trasy.** Długość zwracana jest razem z trasą, a symulacja przelicza ją na jakość rozmowy.
- Trasa jest wybierana **raz**, przy rozpoczęciu rozmowy, i nie zmienia się w trakcie.
- Rozmowa może przechodzić przez każdy typ węzła (dom, słup, centrala).
- Dom jest **dostępny** tylko wtedy, gdy nie uczestniczy w żadnej trwającej rozmowie, ani jako koniec, ani jako węzeł pośredni. Zajęty dom jest pomijany przy wyszukiwaniu trasy. Jeśli zajęty jest dom początkowy lub docelowy, rozmowa od razu dostaje „zajęte”.
- Limit rozmów na węźle (1 dla domu, brak dla słupa i centrali) to wartość zależna od typu węzła, łatwa do zmiany w balansie.

### 4.4 Operacje

| Operacja | Warstwa | Opis |
|---|---|---|
| Dodanie / usunięcie węzła | Struktura | Słup, budynek, centrala. |
| Położenie kabla | Struktura | Między dwoma węzłami, z liczbą żył i długością. |
| Usunięcie kabla (nożyce) | Struktura | Usuwa kabel. Stan techniczny usuwa jego wpis, ruch zrywa rozmowy, które nim szły. **To jedyna forma „naprawy”.** |
| Uszkodzenie żył | Stan techniczny | Zwiększa liczbę uszkodzonych żył o `n` (najwyżej do `Pairs`). Całkowite zerwanie = uszkodzenie wszystkich żył. Jeśli trwających rozmów jest więcej niż sprawnych żył, ruch zrywa **najnowsze** rozmowy. |
| Rozpoczęcie rozmowy | Ruch | Szuka trasy. Jeśli jest, zajmuje po jednej żyle w każdym kablu trasy oraz linię każdego domu na trasie (także początkowego i docelowego) i zwraca rozmowę. Jeśli nie, zwraca „zajęte”. |
| Zakończenie rozmowy | Ruch | Zwalnia żyły i linie domów. |
| Pomiar (miernik) | Stan techniczny | Zwraca liczbę sprawnych żył kabla. |

**Nie ma operacji naprawy.** Uszkodzenia tylko przybywają, aż do usunięcia kabla. Nie ma też automatycznej wymiany: nowy kabel to zawsze osobne położenie kabla.

**Zrywanie rozmów:** gdy kabel traci żyły, a trwających na nim rozmów jest więcej niż sprawnych żył, zrywamy rozmowy od najnowszej. Jest to deterministyczne, więc łatwe do testowania. Zerwana rozmowa zwalnia żyły na wszystkich kablach swojej trasy i linie domów na niej.

### 4.5 Zdarzenia

| Warstwa | Zdarzenia |
|---|---|
| Struktura | dodano / usunięto węzeł, dodano / usunięto kabel |
| Stan techniczny | żyły kabla uszkodzone |
| Ruch | rozmowa rozpoczęta, zakończona, zerwana; zmieniła się zajętość kabla lub węzła |

Widoki podpinają się wybiórczo: grafika kabla pod strukturę (i ewentualne efekty uszkodzeń, np. iskry), a świecenie rozmów pod ruch.

### 4.6 Uszkodzone kable
Nie ma osobnej flagi „martwy”. Kabel jest **sprawny**, **częściowo uszkodzony** albo **zerwany**. Uszkodzony kabel, którego nikt nie zdjął, dalej istnieje w strukturze (i na słupach), ale przenosi mniej rozmów albo żadnej. Dzięki redundancji sieć często działa dalej, a gracz może nie wiedzieć o awarii. To zamierzone.

---

## 5. Reguły (budowa)

Jedyna droga, którą akcje gracza trafiają do sieci.

- **Mapa:** słownik `NodeId → pozycja` na logicznej siatce (niewidocznej dla gracza) oraz klasa słupa.
- **Walidacja przed wywołaniem sieci:**
  - odległość między węzłami ≤ limit (zależny od typu kabla),
  - limit kabli na słupie wg klasy: mały 5, średni 12, duży 30, centrala bez limitu (wartości wstępne),
  - liczą się wszystkie kable, także uszkodzone, a kabel zajmuje **√(liczba żył)** miejsc: zwykły 1, magistrala 4 → 2, 9 → 3, 16 → 4,
  - koszt.
- **Długość kabla** liczona z pozycji i przekazywana do sieci jako `Length`.
- **Podgląd:** ta sama walidacja bez zmiany stanu, żeby kabel mógł świecić na czerwono, zanim gracz puści przycisk.
- **Wszystkie liczby do balansu w jednym miejscu** (klasa z regułami lub `ScriptableObject`).
- Ewentualne ograniczenia dla domów (np. brak magistral) też trafiają tutaj.

Sieć wywołują bezpośrednio tylko testy i ewentualnie generator mapy.

---

## 6. Symulacja

- **Rozmowy w czasie:** kto, kiedy i do kogo dzwoni, ile trwa rozmowa (kilka sekund gry). Rozpoczyna i kończy rozmowy przez operacje sieci.
- **Zadowolenie:** udana rozmowa, jakość zależna od długości trasy, „zajęte”, zerwane rozmowy.
- **Awarie:** wiek kabla, przeciążenie słupa, długość odcinka, zdarzenia. Wywołuje uszkodzenie żył w sieci:
  - **częściowe** (magistrala): kilka żył, częste,
  - **całkowite**: wszystkie żyły, rzadsze.
- Wiek kabla (jeśli będzie potrzebny) to dane symulacji, liczone od momentu położenia kabla. Nie trafia do struktury.
- **Ekonomia i reputacja.**
- **Pojawianie się nowych budynków.**
- Odczytuje stan sieci przez zapytania i zdarzenia, a zmienia go tylko przez jej operacje.

---

## 7. Widoki

- `NodeView`, `CableView` i inne widoki tworzone w odpowiedzi na zdarzenia sieci. Słownik `Id → widok`.
- Kabel: `LineRenderer` albo mesh generowany wzdłuż krzywej zwisu (łańcuchowej), bez fizyki. Magistrala grubsza.
- Trwające rozmowy widoczne na kablach (świecenie, impuls wzdłuż trasy).
- Kliknięcie w scenie: collider widoku zwraca `Id`, a dalej wszystko dzieje się na modelu.

---

## 8. Wydajność

- Model to słowniki i listy Id (struktury z jednym `int`, bez alokacji).
- Dijkstra przy każdej nowej rozmowie: przy setkach węzłów i kilku rozmowach na sekundę to pomijalny koszt.
- Gdyby było trzeba: ograniczyć liczbę nowych rozmów na klatkę albo cache'ować trasy do pierwszej zmiany topologii.

---

## 9. Stan implementacji

- **Struktura sieci** (4.1, 4.2): `CableNetwork`, `Node`, `Cable`, `NodeKind` w `Assets/_Project/Scripts/Networking/` (assembly `SpaghettiWires.Networking`, bez zależności od Unity). Testy: `Assets/_Project/Tests/EditMode/Networking/`.
  - Id węzłów i kabli (`NodeId`, `CableId`) nadaje sieć i nie używa ich ponownie. Domyślne Id (wartość 0) nie wskazuje żadnego obiektu.
  - Usunięcie węzła usuwa też jego kable (najpierw zdarzenia usunięcia kabli, potem węzła).
- **Stan techniczny** (4.1, 4.4): `CableCondition` z referencją do struktury (tylko odczyt i zdarzenie usunięcia kabla).
  - `Damage(cableId, n)` i `Break(cableId)` dodają uszkodzone żyły, najwyżej do `Pairs`. Zdarzenie `PairsDamaged` tylko przy zmianie.
  - `GetWorkingPairs` to wynik pomiaru miernikiem. `DamagedCableIds` przyda się np. do wskaźnika chaosu.
  - Po usunięciu kabla jego wpis znika.
- **Ruch** (4.3, 4.4): `Traffic` i `Call`.
  - `TryStartCall(from, to, out call)` szuka trasy algorytmem Dijkstry (suma `Length`) po kablach z wolną żyłą i przez węzły z wolną linią. Zwraca false, czyli „zajęte”.
  - Pojemność węzła: `Traffic.GetCallCapacity(kind)`, dom 1, słup i centrala bez limitu.
  - `EndCall` zwalnia żyły i linie. Uszkodzenie żył lub usunięcie kabla zrywa najnowsze rozmowy (`CallDropped`).
  - Zajętość: `GetBusyPairs`, `GetFreePairs`, `GetNodeLoad` oraz zdarzenia `CableLoadChanged`, `NodeLoadChanged`.
- **Fasada** `PhoneNetwork`: tworzy trzy warstwy we właściwej kolejności i udostępnia je jako `Structure`, `Condition`, `Traffic`. Nie powiela ich operacji.
- **UI** (`Assets/_Project/Scripts/UI/`, UI Toolkit, elementy tworzone w kodzie, wygląd w `Assets/_Project/UI/*.uss`). Każdy widok ma własny `UIDocument` na wspólnym `PanelSettings`.
  - `ToolbarView`: pasek narzędzi na dole i linia komunikatów.
  - `HudView`: pieniądze i reputacja (lewy górny róg), czas (prawy górny). Tylko wyświetla, wartości ustawia się metodami `Set*`.
  - `PoleTooltipView`: okienko z zajętością miejsc nad słupem pod kursorem, dane z `IBuildActions.TryGetSlotUsage`.
  - `GameOverView`: ekran przegranej, domyślnie ukryty. `Show(czas, powód)` go pokazuje, przycisk wczytuje scenę od nowa.
- **Ekonomia** (`Assets/_Project/Scripts/Simulation/Economy.cs`, assembly `SpaghettiWires.Simulation`, bez zależności od Unity):
  - `Money` (int) i `Reputation` (0–1), wartości startowe w konstruktorze.
  - `CanAfford` i `TrySpend` dla zakupów gracza: gdy go nie stać, nic się nie zmienia. `Earn` dla wpływów.
  - `Charge` dla wydatków przymusowych (utrzymanie, kary). Może zejść poniżej zera, co jest przegraną.
  - `ChangeReputation(delta)` w granicach 0–1. Reputacja 0 to przegrana.
  - Zdarzenia `MoneyChanged`, `ReputationChanged` i `Lost(LossReason)`, to ostatnie tylko raz.
- **`GameSession`** (`Scripts/Game/`): tworzy `Economy` z wartościami startowymi z Inspectora, liczy czas gry (stoi po przegranej), podpina pieniądze, reputację i czas do `HudView`, a przegraną do `GameOverView`. W menu komponentu są akcje debugowe do sprawdzenia HUD i przegranej.
- Do zrobienia: reguły, symulacja, widoki.
