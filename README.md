# README – BoardGamesShop (WPF, C#, SQLite)

## Popis projektu
BoardGamesShop je desktopová aplikace vytvořená v technologii WPF (.NET 8).  
Umožňuje správu katalogu deskových her, přihlášení a registraci uživatelů, práci s nákupním košíkem a úpravu cen.  
Aplikace využívá architekturu MVVM a databázi SQLite.

---

## Hlavní funkce aplikace

### 1. Katalog deskových her
- zobrazení všech her uložených v databázi
- filtrování podle žánru
- vyhledávání podle názvu
- řazení podle ceny
- zobrazení detailů hry

### 2. Nákupní košík
- přidání hry do košíku
- odebrání položek
- zobrazení celkové ceny
- vytvoření objednávky

### 3. Přidání nové hry
- formulář pro zadání názvu, ceny, autora, žánrů a popisu
- validace vstupních údajů
- uložení nové hry do SQLite

### 4. Úprava cen
- hromadná nebo individuální změna ceny her
- okamžitá aktualizace v katalogu

### 5. Registrace a přihlášení uživatele
- vytvoření nového uživatele
- zabezpečené uložení hesla (hashování)
- přihlášení a odhlášení
- práce s aktuálně přihlášeným uživatelem

---

## Použité technologie
- C# (.NET 8, WPF)
- SQLite
- MVVM architektura
- LINQ
- ObservableCollection

---
## Architecture Overview

Projekt je rozdělen do logických celků podle architektury MVVM (Model–View–ViewModel).  
Každá část aplikace má jasně definovanou odpovědnost.

---

### 1. Data Layer (`Data/`)

Obsahuje třídy zajišťující komunikaci s databází SQLite.

| Soubor | Popis |
|--------|-------|
| `Db.cs` | Uchovává connection string a správu připojení k SQLite. |
| `DBController.cs` | Kompletní vrstva pro práci s databází (CRUD operace pro hry, žánry, autory, objednávky). |
| `UserRepository.cs` | Práce s uživateli – registrace, načtení, ověřování. |

---

### 2. Model Layer (`DataModels/`)

Reprezentace doménových dat, bez aplikační logiky.

| Soubor | Popis |
|--------|-------|
| `Game.cs` | Model deskové hry (název, cena, autor, žánry, popis). |
| `Author.cs` | Model autora deskových her. |
| `Genre.cs` | Model žánru hry. |
| `Order.cs` | Model uživatelské objednávky. |
| `CartItem.cs` | Položka v nákupním košíku (hra + množství). |

---

### 3. Authentication Layer (`Auth/`)

Komponenty odpovědné za přihlášení a zabezpečení.

| Soubor | Popis |
|--------|-------|
| `AuthService.cs` | Logika přihlášení, odhlášení, práce s aktuálním uživatelem. |
| `PasswordHasher.cs` | Hashování a ověřování hesel, zabezpečené ukládání do databáze. |

---

### 4. ViewModel Layer (`ViewModels/`)

Obsahuje aplikační logiku a datové bindingy pro jednotlivá okna.

| Soubor | Popis |
|--------|-------|
| `MainViewModel.cs` | Hlavní ViewModel: načítání her, filtrování, košík, správa uživatele, otevírání dialogů. |
| `LoginViewModel.cs` | ViewModel pro přihlášení uživatele. |
| `RegisterViewModel.cs` | ViewModel pro registraci nového uživatele. |
| `RelayCommand.cs` | Implementace rozhraní `ICommand` pro binding tlačítek. |
| `BaseViewModel.cs` | Obecný ViewModel s implementací `INotifyPropertyChanged`. |

---

### 5. View Layer (`Views/`)

XAML okna představující uživatelské rozhraní. Každé okno používá svůj ViewModel přes `DataContext`.

| Soubor | Popis |
|--------|-------|
| `MainWindow.xaml` | Hlavní okno aplikace – katalog her, filtrování, košík. |
| `AddGameWindow.xaml` | Dialog pro přidání nové hry. |
| `EditPricesWindow.xaml` | Okno pro úpravu cen her. |
| `CartWindow.xaml` | Nákupní košík a vytvoření objednávky. |
| `LoginWindow.xaml` | Přihlašovací formulář. |
| `RegisterWindow.xaml` | Registrační formulář. |

---

## Databázový model

### Games
| Sloupec | Popis |
|--------|-------|
| Id | Primární klíč |
| Name | Název hry |
| AuthorId | Autor hry |
| Price | Cena |
| Description | Popis |

### Authors
| Sloupec | Popis               |
|---------|---------------------|
| Id      | Primární klíč       |
| Name    | Jméno autora        |

### Genres
| Sloupec | Popis                |
|---------|----------------------|
| Id      | Primární klíč        |
| Name    | Název žánru          |

### GameGenre
| Sloupec  | Popis                              |
|----------|------------------------------------|
| GameId   | Cizí klíč na tabulku Games         |
| GenreId  | Cizí klíč na tabulku Genres        |

### Users
| Sloupec      | Popis                           |
|--------------|---------------------------------|
| Id           | Primární klíč                   |
| Username     | Uživatelské jméno               |
| PasswordHash | Hash hesla                      |

---

## Architektura MVVM

### Model
Reprezentace dat – hry, žánry, uživatelé, objednávky.

### ViewModel
Aplikační logika:
- načítání dat z databáze
- filtrování a vyhledávání
- správa košíku
- autentizace uživatele
- implementace příkazů pomocí RelayCommand

### View
XAML okna propojená s ViewModely pomocí DataContext.

---

## Spuštění aplikace

1. Otevřete projekt ve Visual Studio 2022 nebo novějším.
2. Ujistěte se, že máte nainstalované:
   - .NET 8 SDK
   - SQLite provider
3. Spusťte aplikaci pomocí F5.
4. Pokud databáze neexistuje, aplikace ji vytvoří automaticky.

---

## Zabezpečení
- hesla jsou ukládána v hashované podobě
- AuthService zajišťuje ověření uživatele
- data nejsou ukládána v otevřené podobě

