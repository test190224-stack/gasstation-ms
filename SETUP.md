# 🚀 GasStation MS — Տեղադրման ուղեցույց

## Պահանջներ

- **.NET 8 SDK** — https://dotnet.microsoft.com/download/dotnet/8.0
- **SQL Server LocalDB** (Visual Studio-ի հետ ավտոմատ գալիս է) կամ
  **SQL Server Express** — https://www.microsoft.com/sql-server/sql-server-downloads
- **Visual Studio 2022** 17.8+ կամ **VS Code + C# Dev Kit**

## Քայլ առ քայլ ուղեցույց

### 1. Բացել project-ը

Ապածրագրավորել `GasStationMS.zip`-ը և բացել `GasStationMS/GasStationMS.csproj` ֆայլը Visual Studio-ում։

Կամ command-line-ով՝

```bash
cd GasStationMS
dotnet restore
```

### 2. Տեղադրել client-side գրադարանները

Visual Studio-ում ավտոմատ կտեղադրվեն։ Ձեռքով՝ command-line-ով՝

```bash
dotnet tool install -g Microsoft.Web.LibraryManager.Cli
libman restore
```

### 3. Ստեղծել տվյալների բազան (Migration)

```bash
dotnet tool install --global dotnet-ef   # եթե EF CLI-ն տեղադրված չէ
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Visual Studio-ում՝ Package Manager Console-ում՝
```
Add-Migration InitialCreate
Update-Database
```

### 4. Գործարկել

```bash
dotnet run
```

Կամ Visual Studio-ում՝ **F5**։

Բրաուզերում բացվում է՝ `https://localhost:7xxx` (պորտը կարող է տարբեր լինել)։

### 5. Մուտք գործել

**Լռելյայն admin հաշիվ՝**
- Էլ․ փոստ՝ `admin@gasstation.am`
- Գաղտնաբառ՝ `Admin@12345`

## Տեխնիկական ստեկը

| Բաղադրիչ | Տեխնոլոգիա |
|---|---|
| Backend | ASP.NET Core MVC 8.0 |
| ORM | Entity Framework Core 8 |
| Database | SQL Server (LocalDB/Express) |
| Auth | ASP.NET Core Identity |
| Frontend | Razor Views + Bootstrap 5 |
| Charts | Chart.js |
| Excel export | EPPlus 7 |

## Նախապես seed-վող տվյալներ

Առաջին գործարկման ժամանակ SeedData-ն ավտոմատ կստեղծի՝

- **5 դեր՝** Administrator, NetworkManager, Manager, Operator, Accountant
- **1 admin user՝** admin@gasstation.am / Admin@12345
- **5 վառելիքի տեսակ՝** A-92, A-95, A-98, Diesel, LPG
- **3 կայան՝** Երևան Կենտրոն, Աջափնյակ, Գյումրի
- **12 ռեզերվուար** (4 ամեն կայանում)
- **12 dispenser** (1 ամեն ռեզերվուարի համար)
- **1 admin-employee** + **1 բաց հերթափոխ** (որ Sales/Create-ը անմիջապես աշխատի)

## Խնդիրների լուծում

### «SqlException: Cannot open database»
SQL Server-ը չի աշխատում։ Ստուգել SQL Server Configuration Manager-ում
կամ փոխել connection string-ը `appsettings.json`-ում՝ քո DB տարբերակի։

### «No shift is open» Sales/Create-ում
Seed-ը չի գործարկվել։ Ջնջել DB-ն (`GasStationMSDb`) և վերագործարկել app-ը։

### «Unable to track instance» EF-ի սխալ
Migration-ները չեն սինխրոնացված։ Կատարել `dotnet ef database update`։

## Ծրագրի կառուցվածքը

```
GasStationMS/
├── Controllers/        5 controller (Home, Account, Stations, FuelInventory, Sales, Reports)
├── Models/             9 entity + ApplicationUser
├── Data/               ApplicationDbContext + SeedData
├── Services/           3 ծառայություն (FuelInventory, Sales, Reports)
├── ViewModels/         Dashboard + Sales report ViewModels
├── Views/              Razor templates
│   ├── Shared/         _Layout, _LoginLayout, _ValidationScriptsPartial
│   ├── Account/        Login, AccessDenied
│   ├── Home/           Index (landing), Dashboard, Error
│   ├── Stations/       Index, Details, Create, Edit
│   ├── FuelInventory/  Index, LowStock, RegisterDelivery
│   ├── Sales/          Index, Create
│   └── Reports/        Dashboard, Sales
├── wwwroot/            Static files (CSS, JS, libs)
├── Program.cs          DI + Identity setup
└── appsettings.json    Connection strings
```

## Հաջորդ քայլերը (ընդլայնման հնարավորություններ)

- ⚙️ Ռեզերվուարների IoT-ինտեգրում (սենսորների տվյալներ)
- 📱 Մոբայլ հավելված (React Native / Flutter)
- 🤖 ML-հիմնված պահանջարկի կանխատեսում
- 💳 Բանկային վճարային համակարգերի ինտեգրում
- 🌐 Հրապարակային API (REST/GraphQL)

Հաջողություն ✨
