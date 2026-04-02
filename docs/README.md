<picture>
  <source media="(prefers-color-scheme: dark)" srcset="../assets/banner-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="../assets/banner-light.svg">
  <img alt="Planner — Clean Architecture • OR-Tools VRP • RabbitMQ • Azure" src="../assets/banner-light.svg">
</picture>

---

# 🧭 Planner  
**Clean Architecture • OR-Tools VRP • RabbitMQ • Azure App Config + Key Vault**

Planner is a modular, cloud-ready logistics optimization platform built with **.NET 10**, demonstrating  
**Clean Architecture**, **asynchronous optimization**, and **real-time visualization** using **Blazor** + **Firestore**.

---

## 🏗️ Infrastructure Overview

Planner applies Clean Architecture with async messaging and Azure integration.

### 🔹 System Architecture
```mermaid
flowchart TD
    subgraph UI["🧑‍💻 Planner.BlazorApp (Blazor Server)"]
        A1[Displays routes & jobs]
        A2[Real-time Firestore updates]
    end
    subgraph API["🌐 Planner.API (ASP.NET 10)"]
        B1[REST endpoints]
        B2[Publishes optimization requests]
        B3[Receives solver results]
        B4[Publishes to Firestore]
    end
    subgraph Worker["⚙️ Planner.Optimization.Worker (.NET Background Service)"]
        C1[Consumes RabbitMQ queue]
        C2[Executes OR-Tools solver]
        C3[Sends results back]
    end
    subgraph MQ["📬 RabbitMQ (Azure Container)"]
        D1[Acts as message broker]
        D2[Decouples API & Worker]
    end
    subgraph Cloud["☁️ Azure Environment"]
        E1[Azure App Configuration]
        E2[Azure Key Vault]
        E3[Azure App Services / Container Apps]
    end
    subgraph Firestore["🔥 Google Firestore"]
        F1[pending_analysis collection]
        F2[route_insights collection]
    end
    UI -->|HTTPS| API
    API -->|AMQP| MQ
    MQ -->|AMQP| Worker
    Worker -->|AMQP| MQ
    MQ -->|Callback| API
    API -->|Write| Firestore
    Firestore -->|Listen| UI
    Cloud --- API
    Cloud --- Worker
```

---

### 🚦 Sequence Flow — “Solve VRP Request”
```mermaid
sequenceDiagram
    participant U as 🧑‍💻 User (Blazor UI)
    participant B as 🌐 Planner.API
    participant Q as 📬 RabbitMQ
    participant W as ⚙️ Optimization.Worker
    participant O as 🧠 OR-Tools Solver
    participant F as 🔥 Firestore
    U->>B: Click “Solve VRP”
    B->>B: Validate job & vehicle data
    B->>Q: Publish <VrpRequestMessage>
    Q-->>W: Deliver message to queue
    W->>O: Run optimization via OR-Tools
    O-->>W: Return optimized routes + cost
    W->>Q: Publish <VrpResultMessage>
    Q-->>B: Deliver result message
    B->>F: Write to pending_analysis collection
    F-->>U: Real-time notification of new result
    U->>U: Render routes on Google Map
```

---

## ⚙️ Technology Stack

| Layer | Technology | Purpose |
|-------|-------------|----------|
| **Frontend** | 🧩 Blazor Server (.NET 10) | Interactive web UI with Firestore listeners |
| **Backend API** | 🌐 ASP.NET Core Web API | Exposes REST endpoints |
| **Messaging** | 📬 RabbitMQ (AMQP) | Decouples API and Worker |
| **Optimization** | 🧠 Google OR-Tools | Solves Linear & VRP models |
| **Background** | ⚙️ Planner.Optimization.Worker | Executes optimization jobs |
| **Real-time DB** | 🔥 Google Firestore | Real-time data sync for routes & insights |
| **Config Mgmt** | 🗂️ Azure App Configuration | Central non-secret settings |
| **Secrets** | 🔒 Azure Key Vault | Secure API keys & credentials |
| **Hosting** | ☁️ Azure App Service / Container Apps | Runs API, UI, Worker, RabbitMQ |
| **Maps** | 🗺️ Google Maps JavaScript API | Visual route display |
| **Build & Deploy** | 🧰 GitHub Actions CI/CD | Build + deploy to Azure |

---

## 🔐 Security & Configuration

### 🧱 Local Development
Sensitive values (e.g., API keys, RabbitMQ credentials) are kept **only in local files**:
```
src/Shared/shared.appsettings.json
```
These are `.gitignore`d.  
Template:  
```
src/Shared/shared.appsettings.template.json
```

### ☁️ Azure Deployment
| Source | Purpose | Example |
|--------|----------|----------|
| **App Configuration** | Shared non-secret settings | `AppConfig__Endpoint=https://planner-appconfig.azconfig.io` |
| **Key Vault** | Secrets and credentials | `@Microsoft.KeyVault(SecretUri=...)` |
| **Environment Vars** | Overrides per env | `RabbitMq__Host`, `Planner__Environment` |

All services authenticate via **Managed Identity**.

### 🧰 Local Fallback
If `shared.appsettings.json` is missing, Planner uses environment variables and logs a friendly notice.

### 🚫 Secret Policy
- No live secrets in repo or history  
- GitHub Secret Scanning enabled  
- Old keys removed via `git filter-repo` and revoked

---

## 🚀 Getting Started

### 🧩 Prerequisites
| Tool | Version | Notes |
|------|----------|-------|
| .NET SDK | 8.0+ | Build/run projects |
| RabbitMQ | 3.12+ | Local broker or Docker |
| Docker Desktop | Latest | Container support |
| Google OR-Tools | 9.x | Optimization engine |
| Google Maps API Key | — | For map rendering |
| Azure Account | — | For deployment |

### ⚙️ Local Setup
```bash
git clone https://github.com/stephen-wu-chaohui/Planner.git
cd Planner
cp src/Shared/shared.appsettings.template.json src/Shared/shared.appsettings.json
```
Edit API keys and RabbitMQ credentials.

Start RabbitMQ:
```bash
docker run -d --hostname planner-rabbit --name planner-rabbit   -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

Run projects:
```bash
dotnet run --project src/Planner.API
dotnet run --project src/Planner.BlazorApp
dotnet run --project src/Planner.Optimization.Worker
```

Open in browser:  
- UI → https://localhost:7014  
- API (Swagger) → https://localhost:5001/swagger  
- RabbitMQ UI → http://localhost:15672 (`planner / planner123`)

---

## 🌟 Project Goals & Future Enhancements

| Area | Planned Feature | Description |
|-------|-----------------|-------------|
| **Optimization** | 🧠 Metaheuristics (ALNS, GA) | Faster large-scale VRP solutions |
| **Dynamic Routing** | ⚡ Live Re-Optimization | React to traffic & order changes |
| **Data Storage** | 💾 EF Core / Dapper | Persist history for analytics |
| **Visualization** | 🗺️ Azure Maps / Leaflet | Advanced multi-layer map |
| **Analytics** | 📊 Power BI integration | Display KPI & cost metrics |
| **Authentication** | 🔐 Azure AD (Entra ID) | Enterprise SSO |
| **Observability** | 📈 App Insights + Logging | Performance & latency tracking |
| **Scalability** | ☁️ Kubernetes / ACI scale-out | Auto-scale workers by queue depth |
| **AI Extensions** | 🤖 ML travel-time prediction | Learn traffic patterns |

> “From route planning to intelligent dispatch.”

---

## 🧰 Contributing
Pull requests and ideas are welcome!  
If you’re passionate about optimization, OR-Tools, or modern .NET architecture,  
Planner is a great sandbox for experimentation.

---

## 🖼️ Logo
<picture>
  <source media="(prefers-color-scheme: dark)" srcset="../assets/logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="../assets/logo-light.svg">
  <img alt="Planner Logo" src="../assets/logo-light.svg" width="96">
</picture>

---

© 2025 Stephen Wu — Planner Project (Frank & Stephen Collaboration)

