<div align="center">

#  EstateFlow

### Estate Administration Management System

**C# • .NET 8 • WPF • ASP.NET Core • EF Core • Azure SQL • Azure Functions • SendGrid**

---

> **EstateFlow** is a desktop-based deceased-estate administration system designed to help Estate Controllers manage estates, correspondence, documents, progress, letters, and automated communications from one central workspace.

</div>

---

##  Project Overview

| Area | Description |
|---|---|
| **Frontend** | WPF desktop application using MVVM |
| **Backend** | ASP.NET Core Web API |
| **Database** | Azure SQL Database |
| **Documents** | Azure Blob Storage |
| **Automation** | Azure Functions |
| **Email** | SendGrid |
| **Authentication** | JWT + Two-Factor Authentication support |
| **Framework** | .NET 8 |
| **Primary Language** | C# |

---

##  Main Features

###  Estate Management

> Centralised management of deceased-estate matters.

- Create, view, update and delete estates.
- Each Estate Controller only sees estates assigned to their account.
- Store deceased details, estate file numbers and administration information.
- Display the estate's latest progress directly on the overview screen.

###  Dashboard

- Search by deceased name, file number or status.
- Filter: active estates (default), needs attention, by status, or all.
- Sort by **most urgent** (missed deadlines first), file number, name, least progress or date of death.
- Each row shows progress, the next deadline and overdue diary items; a red dot marks estates needing attention.
- **Overdue / Due this week** panel: every open diary entry due within 7 days. Diary entries set to
  "Dashboard only" (InApp) appear here instead of by email.

###  Master's Process Checklist & Deadlines

The **Checklist** tab follows the Administration of Estates Act process:

```text
Reported to the Master → Letters of Executorship → Advertised for creditors →
L&D account lodged → Inspection period completed → Paid out → Finalised
```

- Record each stage with the date it happened (and an optional note).
- Progress percentage, next step and estate status follow the checklist automatically.
- Deadlines are worked out per stage, e.g. report within 14 days of death (s7/s9), account within
  6 months of Letters (s35(1)), not before the 30-day creditor claim period ends (s29).
- Estates with a missed deadline, or no progress for 90 days, are flagged on the dashboard.

> [!IMPORTANT]
> The statutory periods are taken from the Act; the follow-up periods (e.g. "chase the Master
> after 60 days") are firm practice. **Confirm every period with the firm.** They can be changed
> without code via the `Workflow__*` settings (see `MasterProcessOptions.cs`).

###  Executors & Beneficiaries

- Add Executors and Beneficiaries to an estate.
- Store email addresses for correspondence.
- Use recipient email details for automated reports and manual communication.
- **South African ID numbers are checked** as you type (13 digits, valid date of birth, citizenship
  digit, Luhn check digit), so a typo never reaches a letter. Values with letters are treated as
  passport numbers. The deceased's date of birth must be before the date of death.
- **Invite to portal**: emails an executor or heir a link to the read-only progress portal.

###  Liabilities

- Record the deceased's debts (creditor, description, amount).
- The Liabilities tab shows total assets, total liabilities and the net estate value.

###  Progress Timeline

Alongside the formal checklist, the **Timeline** tab keeps free-text progress updates.

Examples of progress entries:

```text
Report sent to heirs
Spoke to executor regarding outstanding documents
Documents received
Vehicle transfer submitted
Awaiting response from financial institution
```

Progress entries can be **added, edited and deleted**, and the latest update is reflected on the estate overview.

###  Diary & Correspondence

The Diary acts as a working correspondence area for an estate.

```text
Diary Entry
   ↓
Letter-style preview
   ↓
Choose recipient
   ↓
Send email
```

Recipients can include:

- Estate Controller
- Executor
- Beneficiary
- Custom email address

Diary correspondence is sent through the Azure-hosted backend using **Azure Functions + SendGrid**.

###  Documents & Scans

- Upload estate documents.
- Store file metadata in Azure SQL.
- Store the physical files in Azure Blob Storage.
- Open/download documents.
- Delete documents.
- Automatically generate document names.

###  Letters (vehicle transfer, notification of death)

- One click from a vehicle (Movable tab) or a bank/insurance claim (Claims tab).
- Produced as an editable **Word document on the firm's letterhead** (firm name, contact details,
  date, our reference, Master's reference, addressee, signature block).
- A copy is **filed automatically** under the estate's Documents and recorded in Correspondence.
- A Firm Administrator can change the **wording** (Administration → Letter wording) using fields
  such as `{DeceasedName}`, `{FileNumber}`, `{RegistrationNumber}`.

###  Correspondence History

The **Correspondence** tab lists everything sent from EstateFlow for the estate — diary emails,
letters, monthly reports and portal invitations — with who sent it, to whom, when, and whether it
was delivered. Generated letters can be reopened from here.

###  Monthly Estate Reports

- Sent on the 1st of each month to executors and heirs with an email address.
- **HTML email** on the letterhead: progress bar, stage checklist, next step, the last month's
  updates, and the controller's contact details (plain-text version included).
- **Finalised estates are skipped**, and each estate is sent at most once per month (safe re-runs).
- Can be paused per estate; every report is recorded in the correspondence history.

###  Executor / Heir Portal

- A read-only web page at `https://<api-host>/portal/` where executors and heirs see the stage
  checklist, progress and recent updates for their estate(s) — not ID numbers, values or documents.
- Access is by invitation only (Executors / Heirs tab → **Invite to portal**). The emailed link lets
  them choose a password; it expires after 24 hours and works once.
- Aims to cut down "when do I get paid?" calls.

###  Firm Administration (Firm Administrators only)

- **Staff:** create accounts (with a generated temporary password), reset passwords, unlock,
  deactivate (signs them out immediately), reset two-factor authentication for a lost phone.
- **Estates:** reassign one estate, or move all of a departing controller's estates at once.
- **Audit log:** who viewed or changed which estate, and when (including refused attempts).
- **Letter wording** and **Retention** review (see POPIA below).

###  Data Protection (POPIA)

- **Audit trail** of every view and change, kept even if an estate is later deleted.
- **Encryption at rest** of ID numbers and bank/policy references (AES-256-GCM) when
  `DataProtection__FieldKey` is set. Existing rows are encrypted automatically on start-up.
- **Retention review:** estates finalised more than `Retention__FinalisedEstateYears` (default 5)
  years ago are listed for an administrator to purge. Nothing is deleted automatically.
- Email addresses are masked in Azure logs; error details never reach the client.
- Documents opened on a workstation go to `%TEMP%\EstateFlow` and are deleted on sign-out and exit.

### Authentication & Security

- Staff accounts created by a Firm Administrator (no open sign-up).
- Login with account lockout and rate limiting.
- JWT authentication.
- Two-factor authentication support.
- User-specific estate isolation, enforced on every estate endpoint.
- Production secrets are stored in Azure configuration instead of GitHub.

---

#  Solution Structure

```text
EstateFlow
│
├── EstateFlow.Core
│   └── Shared models and DTOs
│
├── EstateFlow.Data
│   └── EF Core, DbContext and repositories
│
├── EstateFlow.Api
│   └── ASP.NET Core Web API
│
├── EstateFlow.Desktop
│   └── WPF desktop application using MVVM
│
└── EstateFlow.Functions
    └── Azure Functions for automation and email tasks
```

---

# ☁️ Architecture

```text
┌──────────────────────────────┐
│     EstateFlow.Desktop       │
│        WPF / MVVM            │
└──────────────┬───────────────┘
               │ HTTPS + JWT
               ▼
┌──────────────────────────────┐
│       EstateFlow.Api         │
│      ASP.NET Core API        │
│        Hosted in Azure       │
└───────┬───────────┬──────────┘
        │           │
        │           └──────────────► Azure Blob Storage
        │                            Estate documents
        ▼
   Azure SQL
   Estate data

        EstateFlow.Api
              │
              ▼
┌──────────────────────────────┐
│       Azure Functions        │
│  MonthlyReportFunction       │
│  DiaryReminderFunction       │
│  SendDiaryEmailFunction      │
└──────────────┬───────────────┘
               │
               ▼
           SendGrid
               │
               ▼
        Email recipients
```

> [!NOTE]
> The WPF Desktop client is configured to communicate with the hosted Azure API. A team member can normally run the Desktop app without having production Azure SQL, Blob Storage or SendGrid secrets on their machine.

---

#  Requirements

| Requirement | Recommended |
|---|---|
| Operating System | Windows 10 / Windows 11 |
| IDE | Visual Studio |
| SDK | .NET 8 SDK |
| Workload | .NET Desktop Development |
| Version Control | Git |
| Internet | Required for hosted backend access |

Optional for backend/cloud development:

- Azure account/access
- Azure Functions tooling
- Entity Framework tooling

---

#  Quick Start

## 1️⃣ Clone the repository

```powershell
git clone https://github.com/moah15/EstateFlowTest.git
cd EstateFlowTest
```

## 2️⃣ Open the solution

Open:

```text
EstateFlow.sln
```

in Visual Studio.

## 3️⃣ Restore NuGet packages

Visual Studio should restore packages automatically.

If needed:

```powershell
dotnet restore
```

## 4️⃣ Build the solution

In Visual Studio:

```text
Build → Rebuild Solution
```

or:

```powershell
dotnet build
```

## 5️⃣ Set the Startup Project

In **Solution Explorer**:

```text
Right-click EstateFlow.Desktop
→ Set as Startup Project
```

## 6️⃣ Run EstateFlow

Press:

```text
F5
```

or click the green **Start** button.

---

#  Demo / Test Login

> [!IMPORTANT]
> Use a dedicated demo account only. Do not put a real employee's password in this README.

```text
Email:    <demo@estateflow.local>
Password: <EstateFlowDemo123!>
```

A new demo account may initially show **no estates** until estates are created for that account.

---

# How to Use EstateFlow

## Login

1. Launch `EstateFlow.Desktop`.
2. Enter the test credentials.
3. Complete 2FA if enabled.
4. The Dashboard loads estates belonging to the logged-in Estate Controller.

## Create an Estate

```text
Dashboard
   ↓
New Estate / Add Estate
   ↓
Capture estate information
   ↓
Save
```

## Estate Detail Areas

| Section | Purpose |
|---|---|
| **General** | Key estate information, current progress, estate actions |
| **Checklist** | Master's-process stages with dates and deadlines |
| **Deceased** | Particulars of the deceased |
| **Executors** | Executor records, email details, portal invitation |
| **Heirs** | Beneficiary/heir records, email details, portal invitation |
| **Movable** | Vehicles and movable assets; vehicle-transfer letters |
| **Immovable** | Property information |
| **Claims** | Banks, policies and institution claims; notification letters |
| **Liabilities** | Debts of the deceased; assets, liabilities and net value |
| **Diary** | Notes and follow-ups with due dates and reminders |
| **Timeline** | Free-text progress updates |
| **Correspondence** | Every email and letter sent for the estate |

Documents are managed from **Documents** in the sidebar (for the estate opened last).
Each tab is its own file in `EstateFlow.Desktop/Views/EstateTabs`, with the matching
view-model code in `Mvvm/EstateDetailViewModel.<Area>.cs`.

---

#  Progress Example

```text
21 Sep 2026
Report sent to heirs
Status: Complete

19 Sep 2026
Spoke to executor regarding outstanding documents
Status: In Progress

17 Sep 2026
Awaiting response from financial institution
Status: Pending
```

---

# ✉️ Diary Email Flow

```text
Select Diary Entry
       ↓
Review Letter Preview
       ↓
Choose Recipient
       ↓
Edit Subject if needed
       ↓
Send Email
```

The message is routed through:

```text
EstateFlow.Desktop
       ↓
EstateFlow.Api
       ↓
SendDiaryEmailFunction
       ↓
SendGrid
       ↓
Recipient Inbox
```

---

# ☁️ Azure Services

| Azure / Cloud Service | Purpose |
|---|---|
| **Azure App Service** | Hosts `EstateFlow.Api` |
| **Azure SQL Database** | Stores estate/application data |
| **Azure Blob Storage** | Stores estate documents |
| **Azure Functions** | Runs automated tasks and email workflows |
| **Application Insights** | Monitoring and telemetry |
| **SendGrid** | Email delivery |

---

#  Azure Functions

### MonthlyReportFunction

Sends scheduled estate updates to Executors and Beneficiaries with valid email addresses.

### DiaryReminderFunction

Processes diary reminders based on due dates and notification settings.

### SendDiaryEmailFunction

Receives diary-email requests from the main API and sends correspondence through SendGrid.

---

#  Configuration & Secrets

> [!CAUTION]
> Never commit production credentials or connection strings to GitHub.

Do **not** commit:

```text
Azure Storage account keys
Azure SQL passwords
SendGrid API keys
Azure Function keys
JWT signing secrets
Production connection strings
```

The hosted application reads production configuration from Azure Environment Variables.

### Deploying this version

1. **Apply the database migration** `AddStageChecklistCorrespondenceAuditTemplates` before or with
   the API deployment (it only adds tables, plus a one-off clean-up of broken status values):
   see *Entity Framework Migrations* below.
2. Set the new API settings below (at least `SendGrid__ApiKey` for portal invitations, and the
   `Firm__*` letterhead details).
3. Optional but recommended: set `DataProtection__FieldKey` on the API **and** the Function App.
4. Publish the API, the Function App and the desktop app together.

### Main API App Service

Required settings. The API refuses to start, with a clear message, if the first two are missing:

```text
ConnectionStrings__EstateFlowDb   Azure SQL connection string
Jwt__Key                          Random signing key, at least 32 bytes
AzureStorage__ConnectionString
AzureStorage__ContainerName
DiaryEmailFunctionUrl
```

Generate a JWT key with, for example:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

> [!WARNING]
> Changing `Jwt__Key` signs everyone out. That is expected: they simply log in again.

Optional settings:

```text
SendGrid__ApiKey                  Needed for portal invitation emails
SendGrid__FromEmail               Verified sender (default noreply@estateflow.stbb.co.za)

Firm__Name                        Letterhead (default "STBB Incorporated")
Firm__Division                    (default "Deceased Estates Division")
Firm__Address                     Separate lines with |   e.g. "1 Main Road|Cape Town|8001"
Firm__Phone, Firm__Email, Firm__Website

DataProtection__FieldKey          Encrypts ID numbers and account references at rest.
                                  32 random bytes, base64 (same command as the JWT key, with 32).
                                  KEEP A SAFE COPY: without it the encrypted values cannot be read.

Workflow__ReportWithinDaysOfDeath, Workflow__AccountWithinMonthsOfLetters, ...
                                  Checklist deadline periods (see MasterProcessOptions.cs)

Retention__FinalisedEstateYears   Retention review period (default 5) — agree with the firm
Portal__BaseUrl                   Public URL used in portal invitation links (default: the API host)
```

### Staff accounts & the first administrator

Registration is restricted. Only a signed-in **Firm Administrator** can create
staff accounts (`POST /api/auth/register`, roles `EstateController` or `FirmAdministrator`).

To create the first administrator, add these App Service settings and restart the API:

```text
Bootstrap__AdminEmail       Promotes an existing account with this email,
                            or creates a new one using the two settings below
Bootstrap__AdminPassword    Only needed when creating a new account
Bootstrap__AdminFullName    Only needed when creating a new account
```

This only runs while no Firm Administrator exists. **Remove the `Bootstrap__*` settings once it has run.**

### Sign-in protection

- 5 wrong passwords or 2FA codes lock an account for 15 minutes.
- Sign-in endpoints are rate-limited per IP address.
- With 2FA enabled, the authenticator code is only accepted together with the
  short-lived token issued after a correct password.
- Each Estate Controller can only reach estates assigned to them. Every
  endpoint with an `{estateId}` is checked centrally (`EstateOwnershipFilter`).

### Function App

Typical settings:

```text
EstateFlowDb
SendGridApiKey
SendGridFromEmail
AzureWebJobsStorage
DataProtection__FieldKey   Same key as the API (if encryption is on)
Firm__Name, Firm__Division  Letterhead on emails (defaults: STBB Incorporated)
Workflow__*                Same deadline periods as the API
```

---

#  Running Desktop Only

This is the recommended setup for most team members.

```text
EstateFlow.Desktop
       │
       ▼
Hosted EstateFlow.Api
       │
       ├────► Azure SQL
       ├────► Azure Blob Storage
       └────► Azure Functions / SendGrid
```

Because the backend is already hosted, a coworker can usually:

```text
Clone → Restore → Build → Run Desktop → Login
```

without configuring production cloud secrets locally.

---

#  Running the API Locally

Only required when actively developing/debugging `EstateFlow.Api`.

Local API development may require local values for:

- Azure SQL connection
- Azure Blob Storage
- JWT/authentication settings
- Function URLs

Use .NET User Secrets (stored outside the repository):

```powershell
cd EstateFlow.Api
dotnet user-secrets set "ConnectionStrings:EstateFlowDb" "Server=tcp:..."
dotnet user-secrets set "Jwt:Key" "<random value, at least 32 bytes>"
dotnet user-secrets set "AzureStorage:ConnectionString" "..."
```

Do not add real credentials to `appsettings.json`.

---

#  Running the Tests

```powershell
dotnet test
```

| Project | What it checks |
|---|---|
| `tests/EstateFlow.Api.Tests` | Starts the real API against an in-memory database: estate ownership, admin-only registration, lockout, two-factor sign-in, liabilities, the stage checklist, SA ID validation, Word letters and filing, correspondence, audit log, staff deactivation, estate reassignment, the portal invitation flow; plus unit tests for deadlines and ID numbers, and (with SQLite) that ID numbers are really encrypted in the database. |
| `tests/EstateFlow.Desktop.Tests` | Loads every desktop screen with sample data and **fails on any broken XAML resource or binding** (e.g. a misspelt property), plus dashboard search/sort, the due panel, checklist rows and ID-number validation. |

No Azure access or secrets are needed. GitHub Actions runs both on every push and
pull request (`.github/workflows/ci.yml`).

The desktop app's API address is in `EstateFlow.Desktop/appsettings.json`. To point a
local build at a local API without editing it:

```powershell
setx ESTATEFLOW_Api__BaseUrl "https://localhost:7234/"
```

---

# ⚙️ Running Azure Functions Locally

Local Function development may require:

```text
local.settings.json
```

Typical values:

```text
AzureWebJobsStorage
EstateFlowDb
SendGridApiKey
SendGridFromEmail
```

`local.settings.json` must remain excluded from Git.

---

#  Entity Framework Migrations

If database models change, open **Package Manager Console** and set:

```text
Default project: EstateFlow.Data
```

The design-time factory reads the connection string from an environment variable. Set it in the same console first:

```powershell
$env:ConnectionStrings__EstateFlowDb = "Server=tcp:..."
```

Create a migration:

```powershell
Add-Migration MigrationName -Project EstateFlow.Data -StartupProject EstateFlow.Api
```

Apply it:

```powershell
Update-Database -Project EstateFlow.Data -StartupProject EstateFlow.Api
```

> [!NOTE]
> Only create a migration when the database model actually changes.

---

#  Git Workflow

Before starting:

```powershell
git pull
```

Create a branch:

```powershell
git checkout -b your-name-feature
```

Example:

```powershell
git checkout -b mohamed-report-ui
```

Commit your changes:

```powershell
git add .
git commit -m "Improve report settings UI"
git push -u origin your-name-feature
```

> [!WARNING]
> Avoid force-pushing the shared `main` branch once multiple developers are actively working on the project.

---

#  Troubleshooting

## App builds but does not launch

Check:

```text
EstateFlow.Desktop is the Startup Project
.NET 8 SDK is installed
NuGet restore completed
```

Then run:

```powershell
dotnet restore
dotnet build
```

## Login works but no estates appear

This is expected for a new account. EstateFlow isolates estates by Estate Controller.

Create a new estate under that account.

## 401 Unauthorized

The JWT may have expired.

```text
Log out → Log back in
```

## Document upload/open fails

The hosted API requires valid Azure Blob Storage configuration:

```text
AzureStorage__ConnectionString
AzureStorage__ContainerName
```

Desktop-only users should not need to configure these locally.

## Diary email says Function is not configured

The **main `estateflow` App Service** requires:

```text
DiaryEmailFunctionUrl
```

This belongs to the API App Service — **not** the Function App.

## Emails do not arrive

Check:

```text
Recipient email address
SendGrid sender verification
Spam / Junk folder
Azure Function invocation logs
SendGrid API configuration
```

## GitHub blocks push with GH013

GitHub Push Protection detected a secret.

```text
Remove the secret from source/history
Keep it in Azure Environment Variables
Push again
```

Do not bypass protection for genuine production credentials.

---

#  Technology Stack

| Technology | Usage |
|---|---|
| **C#** | Main application language |
| **.NET 8** | Application framework |
| **WPF** | Desktop UI |
| **XAML** | WPF layout and styling |
| **ASP.NET Core** | REST API |
| **Entity Framework Core** | ORM/data access |
| **Azure SQL** | Relational database |
| **Azure Blob Storage** | Estate documents |
| **Azure Functions** | Automation |
| **SendGrid** | Email delivery |
| **GitHub** | Version control and collaboration |

---

#  Current Scope

EstateFlow currently focuses on the **internal Estate Controller workflow**.

Primary areas:

```text
Estate administration
Progress tracking
Diary correspondence
Document management
Generated letters
Executor / Beneficiary communication
Monthly reporting
Cloud-backed shared data
```

Executors and heirs have a read-only progress portal (see **Executor / Heir Portal**).
Firm Administrators manage staff, assignments, letter wording, the audit log and retention in the desktop app.

---

#  Team Setup Summary

```text
1. Clone the repository
2. Open EstateFlow.sln
3. Restore NuGet packages
4. Rebuild Solution
5. Set EstateFlow.Desktop as Startup Project
6. Press F5
7. Login using the TEST account
```

> The Desktop client communicates with the shared Azure-hosted backend, allowing multiple team members to work with the same cloud environment without committing production Azure secrets to GitHub.

---

<div align="center">

## EstateFlow

**Estate Administration Management System**

Built with **C# • .NET 8 • WPF • ASP.NET Core • Microsoft Azure**

</div>
