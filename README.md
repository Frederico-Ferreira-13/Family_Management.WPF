# Family Management

A Windows desktop application for personal and household financial management, built with C#, .NET 10 and WPF.

Family Management brings income, expenses, recurring payments, accounts, budgets, goals and investments together in a local application.

**Status:** A locally installable version is available and is undergoing functional testing with users. Development continues through feedback, bug fixes and improvements to usability and financial workflows.

## About the Project

I created Family Management to help organize personal and family finances while developing my practical skills in C# and the .NET ecosystem.

The project has evolved from individual features into a layered application with domain rules, application use cases, persistent storage and a Windows installer.

A family financial plan provides an overview of household finances. Bank accounts are separate concepts and can be associated with financial operations where applicable.

Income and expenses can be associated with family members, supporting both individual attribution and a consolidated household view.

## Features

### Users and Families

- User registration and sign-in.
- Profile management and password changes.
- Family and household member management.
- Family financial plans.
- Association of financial movements with family members.

### Financial Movements

- Income, expenses and account transfers.
- Separate workflows for occasional and recurring transactions.
- Creation and editing of recurring income and expenses.
- Transaction confirmation and management of financial effects.
- Editable transaction dates.
- Category-based classification.
- Monetary values represented with `decimal` through the `Money` value object.

### Dashboard

- Monthly income and expenses.
- Balance overview.
- Top-three monthly expenses chart.
- Monthly financial history.
- Financial goals overview.
- Upcoming quarterly and semiannual payments for the following month.

### Goals and Planning

- Financial goals and contribution tracking.
- A working capital fund ("Fundo de maneio").
- A 10% contribution mechanism for eligible confirmed family income.
- Budget management.
- Investment recording and tracking.

The working capital fund represents an allocation within the application. It does not execute a bank transfer.

### Local Installation

- Windows desktop installation through Inno Setup.
- Self-contained publishing for Windows x64.
- SQLite storage outside the application installation directory.
- Locally generated authentication signing key.
- Database migrations during application startup.
- Backup of an existing database before pending migrations are applied.

## Local-Only Operation

The application runs locally and does not require a hosted API or a central database.

Each Windows user profile has its own application data directory. Independent installations on different computers do not synchronize or share financial data.

People using the same Windows account use the same local database. Application users and family membership organize access within that installation.

Program files and user data are stored separately:

| Purpose | Default location |
| --- | --- |
| Installed application | `%LocalAppData%\Programs\FamilyManagement` |
| SQLite database | `%LocalAppData%\FamilyManagement\Data\FamilyManagement.db` |
| Local authentication key | `%LocalAppData%\FamilyManagement\Security` |
| Backups before database migrations | `%LocalAppData%\FamilyManagement\Backups` |

The local authentication key is not database encryption.

Development builds and the installed application use the same database location when run under the same Windows account. Testing can therefore affect existing local data.

## Technology Stack

| Area | Technology |
| --- | --- |
| Language | C# |
| Framework | .NET 10 |
| Desktop interface | WPF and XAML |
| Presentation pattern | MVVM |
| Data access | Entity Framework Core |
| Database | SQLite |
| Schema evolution | EF Core Migrations |
| Application startup | .NET Generic Host |
| Dependency management | Dependency Injection |
| Packaging | Inno Setup |
| Version control | Git and GitHub |

## Solution Structure

| Project | Responsibility |
| --- | --- |
| `FamilyManagement.Domain` | Entities, value objects, business rules and repository contracts |
| `FamilyManagement.Application` | Use cases, commands, handlers, DTOs and application services |
| `FamilyManagement.Infrastructure` | EF Core, repositories, persistence, migrations and technical services |
| `Family_Management.WPF` | Views, ViewModels, navigation, styles and desktop startup |
| `FamilyManagement.Tests` | Automated tests |
| `SqliteDiagnostic` | SQLite diagnostic utility |

### Domain

Contains the core financial concepts and their rules.

Examples include users, families, accounts, transactions, recurring transactions, budgets, categories, goals and investments.

Value objects such as `Money` and `EmailAddress` centralize validation and representation rules.

### Application

Coordinates operations through use cases, handlers and application services.

Financial impact services coordinate the application and reversal of transaction effects. Dashboard use cases provide information for the household overview.

### Infrastructure

Implements persistence and technical services.

It includes `ApplicationDbContext`, entity configurations, repositories, Unit of Work, SQLite migrations and local database path management.

Migrations are stored in:

`FamilyManagement.Infrastructure/Data/Migrations`

### WPF

Provides the desktop interface through XAML Views and ViewModels.

It also contains application startup configuration, local authentication configuration, database initialization, publishing profiles and the installer script.

## Development Requirements

- Windows capable of running .NET 10 WPF applications.
- .NET 10 SDK.
- Visual Studio with desktop .NET tooling, or VS Code with C# tooling.
- Git.
- Inno Setup, when building the installer.

The self-contained published application does not require the recipient to install the .NET SDK.

## Running from Source

Run these commands from the solution root.

Restore dependencies:

```powershell
dotnet restore
```

Build the solution:

```powershell
dotnet build
```

Run the desktop application:

```powershell
dotnet run --project .\Family_Management.WPF\Family_Management.WPF.csproj
```

The local startup configuration creates the authentication signing key when needed and uses the persistent database directory under `%LocalAppData%`.

Manually configuring a JWT signing key through .NET User Secrets is not required for this local startup workflow.

## Tests

Run the automated test project:

```powershell
dotnet test .\FamilyManagement.Tests\FamilyManagement.Tests.csproj
```

Automated tests complement manual testing. They do not establish that every financial or interface workflow has been validated.

Current manual testing includes:

- Creating and editing recurring income and expenses.
- Assigning movements to family members.
- Correcting transaction dates.
- Checking dashboard totals.
- Reviewing upcoming payment notices.
- Validating goal contributions and reversals.
- Changing passwords.
- Installing updates while preserving local data.

## Publishing for Windows

Create a self-contained Windows x64 build:

```powershell
dotnet publish .\Family_Management.WPF\Family_Management.WPF.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -o .\Family_Management.WPF\bin\Publish\WindowsLocal
```

The published application is generated in:

`Family_Management.WPF/bin/Publish/WindowsLocal`

This is a folder-based deployment. The application executable depends on the other published files.

## Building the Installer

The Inno Setup script is located at:

`Family_Management.WPF/Installer/FamilyManagement.iss`

To generate an installer:

1. Publish the application.
2. Open the `.iss` script in Inno Setup.
3. Check that `PublishSource` points to the published output on your machine.
4. Set the intended application version.
5. Keep the existing `AppId` for updates to the same application.
6. Compile the installer.

Distribute the generated setup executable. Do not include personal databases, authentication keys or backups.

The installer keeps application files separate from persistent user data.

## Data and Repository Hygiene

The repository should contain source code, migrations, shared configuration, publishing profiles and the installer script.

The following should remain outside version control:

- Personal SQLite databases and their auxiliary files.
- Local authentication keys and private configuration.
- Database backups.
- Build and publish output.
- Generated installers.
- Logs and temporary files.

Backups created before schema migrations are not a replacement for a regular backup strategy.

## Planned Improvements

The following items are planned and should not be interpreted as implemented features:

- Review and simplify the default categories.
- Add subcategories.
- Allow users to create categories and subcategories within the application.
- Allow new subcategories under existing default categories.
- Define permissions for category management within a household.
- Preserve historical movements when categories are renamed or deactivated.
- Improve support for variable salary components, bonuses and additional income.
- Support flexible recording of holiday and Christmas allowances.
- Add credit card tracking with configurable conditions for each card.
- Distinguish credit card purchases from later repayments without duplicating expenses.
- Track outstanding card balances, repayments, interest and charges.
- Improve table sizing, date controls and overall interface consistency.
- Expand automated tests and continue collecting user feedback.
- Continue documenting classes and methods, including the reasoning behind design decisions.

Credit card functionality must support different users and contractual conditions rather than assume a fixed rate or a single repayment model.

## Development Approach

This is a learning-driven project with a focus on:

- Separation of concerns.
- Explicit business rules.
- Understandable application workflows.
- Consistent financial effects and reversals.
- Persistent local data.
- Incremental improvements guided by testing and feedback.

The application is being tested by a small group of users before the next feature iteration.

## License

No distribution license has been selected yet. Until a license is added, do not assume the source code is available under an open-source license.