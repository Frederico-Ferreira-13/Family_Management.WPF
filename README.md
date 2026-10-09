**FamilyManagement**

A desktop application for personal and family financial management, built with C# and WPF.
Status: Work in progress. The core architecture and several modules are implemented, but integration, functional validation, testing, security improvements, and dependency-injection refinements are ongoing. This is not yet a production-ready release.

About the Project
FamilyManagement started with the goal of bringing personal and household finances together in one application: accounts, transactions, budgets, financial goals, and an overview of financial assets.

It is a learning-driven project that has evolved over approximately one year of development, with a strong focus on separation of concerns, business rules, and data persistence. Over time, the work has grown from implementing individual features into designing a layered solution, reviewing contracts, 
improving dependency management, and addressing sensitive financial workflows such as the impact of transactions on account balances.

**Goals:**

- Centralize the management of financial accounts and transactions.
- Organize financial information by user and family.
- Support financial planning through budgets, categories, and goals.
- Track investments and recurring transactions.
- Preserve financial consistency through domain rules and transactional operations.
- Build a modular, testable codebase that can evolve over time.

Technology Stack
Area	Technology / Approach
Language	C#
Platform	.NET 10, Windows
User interface	WPF and XAML
Presentation pattern	MVVM (Model–View–ViewModel)
Data access	Entity Framework Core
Database	SQLite
Database evolution	EF Core Migrations
Application composition	Microsoft.Extensions.Hosting and Dependency Injection
Configuration	appsettings.json, environment variables, and .NET User Secrets
Authentication	Authentication services and JWT configuration
Version control	Git and GitHub


**Architecture**

The solution is organized into four main projects:
Family_Management.WPF/
├── FamilyManagement.Domain/
├── FamilyManagement.Application/
├── FamilyManagement.Infrastructure/
├── Family_Management.WPF/
├── Family_Management.WPF.slnx
└── .gitignore

**Domain**
Contains the core business concepts and rules without depending on the graphical interface or database implementation details.
It includes entities such as Account, Transaction, Budget, Category, Goal, Investment, Family, and User; value objects such as Money; enumerations; domain errors; and repository and IUnitOfWork contracts.
For example, account operations validate amounts, currencies, account status, and available funds before changing balances.

**Application**
Coordinates use cases through commands, handlers, DTOs, and application services.
It includes workflows for creating, confirming, updating, and reversing transactions. ITransactionImpactService centralizes the application and reversal of financial effects, while ISetupService supports initial setup checks.
Consistency across transactions, accounts, budgets, and goals remains an active area of review and testing.

**Infrastructure**

Implements data-access contracts and technical services.
It includes ApplicationDbContext, EF Core configurations, repositories, UnitOfWork, SQLite migrations, and authentication services. Database connection settings are supplied through configuration, and the local SQLite database file is not tracked in version control.

**WPF — Presentation**

Provides the desktop experience through XAML Views, ViewModels, navigation, user-session management, commands, and UI services.
The application uses the .NET Generic Host to configure dependencies and launch the main window. Service registration and dependency lifetimes are still being stabilized.

**Project Modules**

- Authentication and users: sign-in, registration, password changes, and user management.
- Families: family management and initial setup.
- Accounts: account creation, management, and balances.
- Transactions: income, expenses, transfers, and transaction confirmation.
- Categories: classification of financial transactions.
- Budgets: expense planning and tracking.
- Financial goals: goals and associated contributions.
- Investments: investment recording and tracking.
- Recurring transactions: defining and processing recurring financial activity.
- Dashboard: financial information display and CSV export integration.
- Currencies: currency identification and management.

The presence of modules, entities, and screens does not mean every feature is complete or fully validated end to end.

**Technical Decisions and Challenges**

**Separation of Concerns**

Moving toward a layered architecture keeps financial rules in the Domain layer, coordinates operations in Application, isolates EF Core in Infrastructure, and leaves presentation responsibilities to WPF.
Financial Transactions and Consistency

Income, expenses, and transfers may affect multiple entities. The project uses domain operations and database transactions to coordinate those effects. Intermediate failures, reversals, insufficient funds, and changes tracked by DbContext are still under review.
Persistence and Migrations

The relational model is managed with EF Core and SQLite. Development work includes entity configuration, relationship mapping, migrations, and investigating mapping issues.
Dependency Injection and WPF Startup

Integrating the .NET Generic Host with MVVM navigation requires careful management of Singleton, Scoped, and Transient lifetimes. This is one of the areas currently being refined.
Configuration Security

The JWT signing key must not be stored in the repository. During development, it should be provided through .NET User Secrets. Local database files, build artifacts, and private secrets are excluded from Git.

**Development Requirements**
- Windows with WPF support.
- .NET 10 SDK.
- Visual Studio or VS Code with C#/.NET tooling.
- Git.
  
**Getting Started**
1. Clone the repository and open the solution directory.
2. Restore dependencies and build:
   dotnet restore
   dotnet build
3. Ensure Family_Management.WPF/appsettings.json contains a valid ConnectionStrings:DefaultConnection value and the non-secret JwtSettings parameters, without an embedded signing key.
4. Configure JwtSettings:SecurityKey using .NET User Secrets for the WPF project (generate your own strong private key; do not reuse someone else's):
   dotnet user-secrets init --project .\Family_Management.WPF\Family_Management.WPF.csproj
   # Set JwtSettings:SecurityKey to a strong, private value.
5. Set the development environment in your PowerShell session:
   $env:DOTNET_ENVIRONMENT = "Development"
6. Run the application:
   dotnet run --project .\Family_Management.WPF\Family_Management.WPF.csproj
   
**Note:** Startup may still expose unresolved navigation dependencies. These steps describe environment setup; they do not guarantee that the current version works in every scenario.

**Current Status and Roadmap**
- [x] Initial layered solution structure.
- [x] Core domain entities, contracts, and business rules.
- [x] EF Core infrastructure with SQLite and migrations.
- [x] Structure for Views, ViewModels, and application use cases.
- [x] Git repository preparation and exclusion of local data.
- [ ] Resolve navigation lifetimes and WPF startup issues.
- [ ] Validate the consistency of financial operations and reversals.
- [ ] Add automated tests for domain logic, application workflows, and persistence.
- [ ] Review authentication, permissions, and isolation of family data.
- [ ] Complete end-to-end functional testing and prepare a first stable release.

**Development Journey**
This repository marks a milestone in approximately one year of development. Beyond its financial features, the project has been an opportunity to deepen practical knowledge of C#, WPF, MVVM, layered architecture, Entity Framework Core, dependency injection, domain modeling, and version-control practices.
The goal is to keep improving code quality and turn this foundation into a reliable, understandable, and maintainable family financial management application.

**License**
No distribution license has been selected yet. Until one is added, do not assume the code is released under an open-source license.
