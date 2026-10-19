# Spaghetti Wires (tytuł roboczy)

Gra na **Ludum Dare 60**, temat: **„Old Tech”**.

> Jesteś operatorem sieci telefonicznej w azjatyckiej dzielnicy. Miasto rośnie, kable się psują, a Ty nie masz czasu ich naprawiać. Ciągniesz nowe obok starych… i tak powstaje legendarna plątanina kabli jak w Bangkoku.

---

## 1. Koncepcja

| | |
|---|---|
| **Gatunek** | Puzzle + zarządzanie z elementami „naprawiacza” |
| **Widok** | 3D, kamera izometryczna z obrotem i zoomem |
| **Tempo** | Na początku powolna, potem gra stopniowo przyspiesza |
| **Platforma** | PC (Windows, WebGL) |
| **Silnik** | Unity 6000.3, URP |
| **Tryb** | Gra bez końca: grasz, dopóki nie przegrasz (etapy z celami być może później) |
| **Sesja** | ok. 15–20 minut do przegranej |
| **Format** | Ludum Dare **Jam** (72 h) |

### Filar projektu: chaos powstaje organicznie
Gracz nie robi bałaganu, bo chce, tylko dlatego, że **w danej chwili to najbardziej opłacalna decyzja**. Znalezienie i zdjęcie uszkodzonego kabla kosztuje czas, więc pod presją gracz ciągnie nowy obok, a stary zostaje na słupach. Ekonomia gry sama produkuje plątaninę.

Drugie źródło chaosu: **sieć działa mimo awarii**. Rozmowy same omijają uszkodzone kable, więc gracz często nawet nie wie, że coś jest zepsute. Dokłada kolejne kable, a zepsute dalej wiszą.

### Związek z tematem „Old Tech”
- Miedziane kable, centrala telefoniczna, słupy, magistrale.
- Stare kable **nigdy nie znikają**. Każda decyzja zostawia trwały ślad w krajobrazie miasta.
- Końcowa plątanina to wizualna historia sieci.

---

## 2. Pętla rozgrywki

```
Nowy budynek
        │
        ▼
Poprowadź kabel ──► za daleko? ──► postaw słup
        │              słup pełny? ──► postaw kolejny / większy słup
        ▼
Mieszkańcy dzwonią do siebie
        │
        ├─► jest wolna droga  → rozmowa, pieniądze, zadowolenie
        │                       (długa trasa = gorsza jakość)
        └─► brak wolnej drogi → „zajęte”, zadowolenie spada
                                → dołóż kable / magistralę
        ▼
Awaria! (część żył albo cały kabel przestał działać, często niezauważenie)
        │
        ├─► POSPRZĄTAJ: znajdź miernikiem, usuń nożycami, połóż nowy
        │               (np. od razu magistralę) → wolniej, ale słup ma miejsce
        ├─► DOŁÓŻ: nowy kabel obok → szybko, stary zostaje na słupach
        │          → chaos rośnie
        └─► NIE WIESZ: sieć omija awarię, gracz dokłada kable → chaos rośnie
```

---

## 3. Mechaniki

### 3.1 Model sieci: każdy słup sam przełącza rozmowy
Sieć to węzły (domy, słupy, centrala) połączone kablami. **Każdy słup działa jak mała, „inteligentna” centrala**: sam przekazuje rozmowę do właściwego kabla. Gracz nie projektuje wnętrza słupów, tylko decyduje, gdzie stawia słupy i ile przepustowości prowadzi między nimi.

```
 dom A ──┐                                ┌── apteka
         │ kabel (1 żyła)                  │
       [SŁUP 1] ════ magistrala 4 żyły ════ [SŁUP 2]
         │                                │
 dom B ──┘                                └── dom C

 A dzwoni do apteki:  A → Słup 1 → (1 z 4 żył magistrali) → Słup 2 → apteka
 Magistrala ma jeszcze 3 wolne żyły dla innych rozmów.
```

**Rozmowa:**
1. Mieszkaniec domu A chce zadzwonić do budynku B.
2. Rozmowa szuka **najkrótszej drogi**, na której każdy kabel ma **wolną żyłę**.
3. Na czas rozmowy (kilka sekund gry) zajmuje **po jednej żyle** w każdym kablu na trasie.
4. Trasa jest wybierana **raz, na początku rozmowy**, i nie zmienia się w trakcie.
5. Po rozmowie żyły się zwalniają.

- **Brak wolnej drogi** oznacza sygnał zajętości i spadek zadowolenia.
- **Długość trasy nie jest ograniczona**, ale długa trasa pogarsza **jakość rozmowy** i zadowolenie. Gracz ma powód, żeby budować krótkie drogi, a nie łączyć wszystkiego naokoło miasta.
- **Więcej kabli zawsze się opłaca**, bo to więcej przepustowości. To napędza chaos.

| Element | Opis |
|---|---|
| **Dom / budynek** | Węzeł sieci. Tu zaczynają się i kończą rozmowy. |
| **Słup** | Węzeł, który przełącza rozmowy. Ma limit kabli zależny od klasy (3.3). |
| **Centrala** | „Super-słup”: przełącza jak słup, ale ma **nieograniczoną** liczbę kabli. Słup nigdy nie staje się centralą. **Nie ma jej na początku gry**: gracz buduje ją sam w miarę rozwoju sieci. |
| **Kabel** | Łączy dwa węzły. Ma 1 żyłę, czyli przenosi jedną rozmowę naraz. |
| **Magistrala** | Kabel z wieloma żyłami (4, 9, 16): wiele rozmów naraz. Na słupie zajmuje tyle miejsc, ile wynosi **pierwiastek z liczby żył** (3.3). |

**Łączenie budynek–budynek jest dozwolone.** Można zrobić łańcuch dom → dom → dom. Na początku to tanie i wygodne.

**Dom przekazuje rozmowy innych, ale blokuje przy tym sam siebie.** Gdy rozmowa przechodzi przez dom, jego linia jest zajęta, więc mieszkańcy tego domu nie mogą w tym czasie ani dzwonić, ani odbierać. Dom przenosi naraz **jedną rozmowę**: swoją albo cudzą. Dzięki temu dom-hub nie zastąpi słupa: im więcej rozmów przez niego idzie, tym częściej jego mieszkańcy słyszą „zajęte” i tym bardziej są niezadowoleni.

### 3.2 Kable
- **Maksymalna długość odcinka** między dwoma punktami zaczepienia. Dłuższa trasa wymaga postawienia słupa.
- Kabel **zwisa** zależnie od długości odcinka (wstępnie element graficzny, być może w przyszłości będzie wpływało na logikę gry).
- Kabel ma **wiek**, a stare kable mają większe prawdopodobieństwo awarii.
- Kabel ma **liczbę żył**: zwykły kabel 1, magistrala więcej. Każda żyła to jedna rozmowa naraz.
- Stan kabla to **liczba uszkodzonych żył**:
  - **sprawny**: wszystkie żyły działają,
  - **częściowo uszkodzony** (tylko magistrala): część żył nie działa, kabel przenosi mniej rozmów,
  - **zerwany**: żadna żyła nie działa.
- Uszkodzenia **nie znikają**. Kabla nie da się naprawić, można go tylko **usunąć** (3.5). Uszkodzony kabel, którego nikt nie zdjął, dalej wisi i zajmuje miejsce na słupach.
- Kable kładzie się zawsze **od węzła do węzła** (słup, dom, centrala). Nie ma automatycznej wymiany kabla.

### 3.3 Słupy
Słupy mają **klasy** o różnym limicie kabli (np. drewniany słup uniesie mniej niż metalowy):

| Klasa | Limit kabli (wstępnie) |
|---|---|
| Mały | 5 |
| Średni | 12 |
| Duży | 30 |
| Centrala | bez limitu |

- Limit dotyczy **miejsc** na słupie. Liczą się **wszystkie** kable, także uszkodzone.
- Kabel zajmuje tyle miejsc, ile wynosi **pierwiastek z liczby jego żył**:

| Kabel | Żyły | Miejsca na słupie | Żył na jedno miejsce |
|---|---|---|---|
| Zwykły | 1 | 1 | 1 |
| Magistrala 4 | 4 | 2 | 2 |
| Magistrala 9 | 9 | 3 | 3 |
| Magistrala 16 | 16 | 4 | 4 |

- **Uzasadnienie fizyczne:** przekrój kabla rośnie proporcjonalnie do liczby żył, więc jego średnica rośnie jak pierwiastek, a miejsce w uchwycie na słupie zależy od średnicy.
- **Rozmiary magistral to kwadraty** (4, 9, 16), żeby liczba miejsc była całkowita.
- Magistrala jest wydajniejsza od zwykłych kabli, więc w pewnym momencie opłaca się nią zastępować kilka zwykłych kabli. Zajmuje jednak więcej niż jedno miejsce, co jest **łagodną wadą**. Trochę zmniejsza to chaos, ale cienkie kable kładzione na szybko dalej go tworzą. Do sprawdzenia w balansie. Jeśli magistrale dalej będą dominować, główną przeciwwagą może być cena.
- Słup blisko limitu jest **przeciążony**, co zwiększa ryzyko awarii kabli na nim.
- Pełny słup wymusza postawienie nowego lub większego, więc trasy się wydłużają, a chaos rośnie.

### 3.4 Awarie
Awarie nie są czysto losowe. Decyzje gracza wpływają na ich częstość:
- **wiek kabla**,
- **przeciążony słup**,
- **długi odcinek** (duży zwis),
- **kabel nad ulicą** (może go zahaczyć ciężarówka),
- **zdarzenia**: burza, monsun, fajerwerki (rozszerzenie).

Rodzaje awarii (tak jak w rzeczywistości):

| Awaria | Przyczyna | Skutek | Jak często |
|---|---|---|---|
| **Częściowa** (magistrala) | Woda pod osłoną, przetarcie, korozja | Padają niektóre żyły, kabel przenosi mniej rozmów | Częsta, zwykle niezauważona |
| **Całkowita** | Zerwanie: ciężarówka, burza, przeciążony słup | Padają wszystkie żyły | Rzadsza, widoczna |

Skutki awarii:
- rozmowy, które szły uszkodzoną żyłą, **zrywają się**. Jeśli sprawnych żył zostało mniej niż trwających rozmów, zrywają się **najnowsze** rozmowy,
- kolejne rozmowy **omijają** kabel bez wolnych sprawnych żył, jeśli jest inna droga,
- jeśli innej drogi nie ma, budynek jest **odcięty**.

**Redundancja ukrywa awarie i to jest zaleta.** Gracz często nie wie, że kabel jest zepsuty, bo sieć dalej działa. Widzi najwyżej, że częściej jest „zajęte” albo rozmowy idą dłuższą drogą. Dokłada kolejne kable, a zepsute wiszą dalej. Chaos rośnie także z niewiedzy.

### 3.5 Naprawa = usunięcie: posprzątać czy dołożyć?
Kabla **nie da się naprawić**. Tak też często jest w rzeczywistości: zamiast naprawiać zepsute pary, monter przepina abonenta na sprawną parę, a zepsute zostają w kablu na zawsze.

**Akcje na kablu (tylko dwie):**
1. **Sprawdź stan** (miernik, 3.6).
2. **Usuń** (nożyce).

**Akcja na węźle (słup, dom, centrala):**
- **Dodaj kabel** (zwykły lub magistralę) do innego węzła.

Kluczowa decyzja przy awarii:

| | Dołożyć nowy kabel obok | Posprzątać: usunąć stary i położyć nowy | Nic nie robić |
|---|---|---|---|
| **Pieniądze** | Koszt kabla | Koszt kabla (+ ewentualnie koszt usunięcia) | 0 |
| **Czas** | Krótki | Dłuższy: trzeba znaleźć uszkodzony kabel miernikiem i go zdjąć | 0 |
| **Skutek** | Stary kabel zostaje i zajmuje miejsce na słupach | Słup ma wolne miejsce | Mniejsza przepustowość, dłuższe trasy |

Na początku sieć jest mała, więc awarie widać od razu i sprzątanie się opłaca. Później awarii jest dużo, a klienci tracą cierpliwość, więc **opłaca się dokładanie**. Słupy się zapychają, trzeba stawiać nowe, i tak rośnie chaos.

**Przewidywane zachowanie graczy:** usuwając uszkodzony kabel, gracz często położy w jego miejsce **magistralę**, bo jest wydajniejsza. Awaria staje się okazją do rozbudowy sieci. Żeby zwykły kabel dalej miał sens, magistrala ma wady: zajmuje więcej miejsc na słupie (3.3), a ewentualnie także wyższa cena, krótszy zasięg albo brak możliwości podłączenia do domu (do ustalenia w balansie).

**Częściowo uszkodzona magistrala: zostawić czy usunąć?** Uszkodzonych żył nie da się naprawić (w prawdziwych kablach w praktyce też się tego nie robi). Gracz decyduje, czy magistrala z częścią sprawnych żył jest jeszcze warta miejsca, które zajmuje. Naturalny próg wynika z liczby miejsc na słupie:

| Magistrala 4 (2 miejsca) | Sprawne żyły | Żył na miejsce | W porównaniu ze zwykłymi kablami |
|---|---|---|---|
| Sprawna | 4 | 2 | Lepsza |
| 1 żyła padła | 3 | 1,5 | Wciąż lepsza |
| 2 żyły padły | 2 | 1 | Tak samo dobra |
| 3 żyły padły | 1 | 0,5 | Gorsza, marnuje miejsce |

Do tego dochodzi niewiedza: bez pomiaru gracz nie wie, ile żył jeszcze działa. Decyzja jest więc dwustopniowa: **czy mierzyć**, a potem **czy usunąć**.

### 3.6 Miernik: sprawdź stan kabla
- Gracz sprawdza miernikiem kabel i dostaje **liczbę sprawnych żył** (np. `2/4 sprawne`). Dla zwykłego kabla to po prostu „działa / nie działa”.
- To **jedyny** sposób, żeby na pewno dowiedzieć się o uszkodzeniu. Najechanie kursorem pokazuje tylko zajęte żyły, nie uszkodzone.
- Pozwala znaleźć uszkodzone kable, także te, o których gracz nie wiedział.
- Każdy pomiar **zabiera czas**.
- Mądry gracz korzysta też z dedukcji: jeśli rozmowy w jakimś rejonie nagle idą naokoło albo są zajęte, coś tam pewnie padło.
- Opcjonalne podpowiedzi wizualne: iskry, zwisający kabel, ptak na przewodzie.

### 3.7 Rozwój miasta
- Co pewien czas pojawia się **nowy budynek**, którego mieszkańcy chcą dzwonić.
- Tempo rośnie: więcej budynków to więcej rozmów i więcej kabli, a więcej kabli to więcej awarii.

### 3.8 Presja i przegrana
- Gra **nie ma końca**. Tempo rośnie, aż gracz przestanie nadążać.
- Udane rozmowy dają **pieniądze** i **zadowolenie**. Długa trasa daje mniej zadowolenia.
- Rozmowa „zajęta”, zerwana lub budynek bez połączenia obniżają zadowolenie. Niezadowoleni mieszkańcy rezygnują z abonamentu i spada **reputacja**.
- **Przegrana:** reputacja spada do zera albo kończą się pieniądze.
- **Wynik:** czas przetrwania, liczba udanych rozmów, „wskaźnik chaosu” (liczba uszkodzonych i zbędnych kabli) i zdjęcie plątaniny na koniec.

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
| Wybór narzędzia | Pasek na dole lub klawisze 1–5 |

**Narzędzia:**
- **Kabel**, **Magistrala**: dodaj kabel od węzła do węzła,
- **Słup** (z wyborem klasy),
- **Miernik**: sprawdź stan kabla,
- **Nożyce**: usuń kabel.

**Czytelność w chaosie (kluczowe):**
- trwające rozmowy widać na kablach (np. świecąca żyła lub impuls biegnący trasą),
- najechanie na budynek pokazuje trasy jego rozmów,
- kabel za długi lub słup pełny świeci na czerwono, zanim gracz puści przycisk,
- najechanie na słup pokazuje zajętość (np. `7/12`), a na kabel liczbę zajętych żył (np. `3/4`). Uszkodzeń **nie** pokazuje, do tego jest miernik.

---

## 5. Oprawa

- **Styl:** low-poly, ciasna zabudowa, kolorowe szyldy, klimatyzatory na ścianach, kable wszędzie.
- **Oświetlenie (URP):** zmierzch lub noc, neony szyldów, bloom, ciepłe lampy uliczne.
- **Kable:** generowane proceduralnie ze zwisem (krzywa łańcuchowa), bez fizyki. Grubość kabla rośnie jak **pierwiastek z liczby żył**, tak samo jak liczba zajmowanych miejsc na słupie, więc wygląd zgadza się z mechaniką.
- **Słupy:** klasy różnią się wyglądem (np. drewniany, betonowy, stalowy).
- **Dźwięk:** szum miasta, skuter, iskrzenie, sygnał telefonu, sygnał zajętości, dzwonek awarii, klik przy podpinaniu kabla.

### Zasoby zewnętrzne
- Startujemy w **Jam**, więc wolno używać gotowych zasobów.
- Modele: proste low-poly, najlepiej na licencji **CC0** (np. Kenney, Quaternius). Najlepiej jedna lub dwie paczki, żeby styl był spójny.
- Prowadzimy **listę źródeł** wszystkich zasobów i podajemy ją na stronie gry.
- AI: zasady Ludum Dare nie ograniczają korzystania z AI. Na stronie gry wspominamy, przy czym pomagało.

---

## 6. Architektura techniczna

Przeniesiona do [TECH.md](TECH.md).

---

## 7. Zakres na jam

### Wersja minimalna (MVP, pierwsze 24–36 h)
- [ ] Mapa na siatce: kilka budynków na start (bez centrali)
- [ ] Budowanie centrali przez gracza
- [ ] Kamera izometryczna: przesuwanie, obrót, zoom
- [ ] Prowadzenie kabli z limitem długości odcinka i zwisem
- [ ] Stawianie słupów z limitem kabli (na start jedna klasa)
- [ ] Rozmowy: wyszukiwanie trasy, zajmowanie i zwalnianie żył, sygnał zajętości
- [ ] Pojawianie się nowych budynków
- [ ] Awarie całkowite, miernik, usuwanie kabli (nożyce)
- [ ] Pieniądze, zadowolenie, reputacja, przegrana
- [ ] Widoczne trwające rozmowy na kablach

### Rozszerzenia (jeśli starczy czasu)
- [ ] Magistrale (kable wielożyłowe) i ich częściowe uszkodzenia
- [ ] Klasy słupów (mały, średni, duży)
- [ ] Jakość rozmowy zależna od długości trasy
- [ ] Ery technologii i nowe typy kabli
- [ ] Zdarzenia: burza, monsun, ciężarówka
- [ ] Oprawa: neony, noc, dźwięk, muzyka
- [ ] Ekran końcowy ze zdjęciem plątaniny i wskaźnikiem chaosu

---

## 8. Ryzyka

| Ryzyko | Jak ograniczyć |
|---|---|
| Plątanina staje się nieczytelna i frustrująca | Widoczne rozmowy, podświetlanie tras, kolory typów kabli |
| Gracz nie rozumie, dlaczego jest „zajęte” | Pokazywanie zajętości kabli i słupów, czytelny komunikat przy nieudanej rozmowie |
| Redundancja ukrywa awarie tak dobrze, że sprzątanie nie ma sensu | Balans: awarie zmniejszają przepustowość i wydłużają trasy, co obniża zadowolenie |
| Domy zastępują słupy | Dom przenosi naraz tylko jedną rozmowę i blokuje wtedy sam siebie. W razie potrzeby dodatkowe ograniczenia (brak magistral, krótszy zasięg) |
| Usuwanie kabli za tanie (zero chaosu) albo za drogie (słupy zawsze zapchane) | Balans kosztu czasu i pieniędzy usuwania, testy od pierwszego dnia |
| Magistrala zawsze lepsza, zwykły kabel zbędny | Magistrala zajmuje √żył miejsc na słupie. W razie potrzeby: cena, zasięg, brak podłączenia do domu |
| Za dużo systemów na 72 h | Najpierw MVP, rozszerzenia dopiero potem |
| Prowadzenie kabli w 3D jest niewygodne | Zaczepianie do najbliższego węzła, czytelny podgląd przed puszczeniem |

---

## 9. Decyzje
| Pytanie | Decyzja |
|---|---|
| Gra bez końca czy etapy z celem? | **Bez końca.** Łatwiej zrobić. Etapy być może później. |
| Czy budynki można łączyć łańcuchem? | **Tak.** |
| Jak działa sieć? | **Każdy słup sam przełącza rozmowy.** Rozmowa szuka najkrótszej drogi z wolnymi żyłami i zajmuje je na czas trwania. Bez projektowania wnętrza słupów. |
| Czy dom przekazuje rozmowy innych? | **Tak**, ale blokuje przy tym sam siebie: dom przenosi naraz jedną rozmowę, swoją albo cudzą. |
| Rola centrali | **Super-słup** bez limitu kabli. Słup nigdy nie staje się centralą. |
| Czy centrala stoi na mapie od początku? | **Nie.** Gracz buduje ją sam w miarę rozwoju sieci. |
| Limit kabli na słupie | **Tak**, zależny od klasy słupa. Kabel zajmuje √(liczba żył) miejsc. Rozmiary magistral to kwadraty: 4, 9, 16. |
| Ograniczenie długości trasy rozmowy | **Brak.** Długa trasa obniża jakość rozmowy i zadowolenie. |
| Czy awarie mają być widoczne? | **Nie muszą.** Redundancja ukrywa awarie, co jest zaletą i źródłem chaosu. Miernik pozwala je znaleźć. |
| Czy kabel można naprawić? | **Nie.** Naprawa to usunięcie kabla. Na kablu są tylko dwie akcje: sprawdź stan i usuń. Kable dodaje się od węzła do węzła, bez automatycznej wymiany. |
| Czy magistrala może być częściowo uszkodzona? | **Tak.** Stan kabla to liczba uszkodzonych żył. Gdy sprawnych żył jest mniej niż rozmów, zrywają się najnowsze. Uszkodzonych żył nie da się naprawić, więc gracz decyduje, czy zostawić magistralę, czy ją usunąć. |
| Koszty i czasy akcji? | Ustalimy w testach, gdy będą gotowe mechaniki. |
| Compo czy Jam? | **Jam** (72 h). Wolno używać gotowych modeli. |
| Zasady LD dotyczące AI? | Nie ograniczają korzystania z AI. |
