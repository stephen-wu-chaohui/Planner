# Planner — Fleet Optimization and Visualization Platform

**Planner** is a modular .NET 8 and Blazor-based system that demonstrates how **optimization algorithms** (Google OR-Tools) can be integrated into a **clean, message-driven architecture** for intelligent **fleet management**.  
It combines optimization science with modern .NET engineering to create a purpose-driven, scalable, and interactive platform.

---

## 🗺️ PlannerView — The Operational Core

`PlannerView.razor` is the centerpiece of the Planner ecosystem — a **map-driven orchestration layer** that connects the Blazor UI, data services, and backend optimization pipelines.  
It showcases .NET 8 full-stack capability with live data, dependency injection, and asynchronous optimization updates.

### 🔹 Architectural Role
PlannerView unifies workflow between **UI**, **state management**, and **optimization**:

| Layer | Description |
|-------|--------------|
| **Blazor Presentation Layer** | Built with Blazor Server (.NET 8), Bootstrap 5, and Google Maps JS interop (`PlannerMap.razor`, `googleMap.js`). |
| **Data Coordination Layer** | `DataCenterService` loads and synchronizes JSON domain data (customers, vehicles, jobs, routes). |
| **Real-Time Update Layer** | `IMessageHubReceiver` (SignalR client) subscribes to optimization results and updates the map dynamically. |
| **Optimization Interface** | User actions like *Solve VRP* or *Run Solver* post structured requests to Planner.API. |

---

### 🔹 Developer-Focused Features

#### 🧭 Map-Centric UX
- Dynamic markers and route overlays  
- Custom icons (vehicles, depots, customers)  
- Context menus and Ctrl-click coordinate picking  
- Dark-mode map style for visual clarity  

#### 🧠 Reactive State Management
- Dependency-injected `DataCenterService` (singleton scope)  
- Async initialization via `OnInitializedAsync()`  
- Centralized subscription to SignalR for all components  

#### ⚙️ Event-Driven Workflow

    Right-click → Open Modal → Edit Entity → Update DataCenterService
                 ↓
              PlannerMap rerenders
                 ↓
          User triggers “Solve VRP”
                 ↓
       HTTP → API → RabbitMQ → Worker
                 ↓
        SignalR pushes result → Map updates

#### 💾 Lightweight Persistence
All domain data stored in `/wwwroot/data/*.json`, enabling simulation, debugging, and offline sharing — easily replaceable with EF Core or Dapper later.

#### 🧱 Modular Components
- `CustomersModal.razor`
- `VehiclesModal.razor`
- `JobsModal.razor`
- `RoutesModal.razor`

Bootstrap modals are toggled through Blazor event binding; each supports vertical or full-screen layouts.

#### 🧩 Extensibility
PlannerView is engineered for growth:
- Plug-in solvers (VRP, Scheduling, Load Balancing)  
- Additional visualization (heatmaps, clusters)  
- REST / gRPC APIs  
- Cloud-ready CI/CD pipelines  

---

### 🔹 Technology Stack

| Area | Implementation |
|-------|----------------|
| **Frontend** | Blazor Server (.NET 8), Bootstrap 5, Google Maps JS API |
| **Backend** | Planner.API + RabbitMQ + Planner.Optimization.Worker |
| **Communication** | SignalR (real-time) + HTTP JSON |
| **State/Data** | Scoped `DataCenterService`, DI, JSON persistence |
| **Optimization** | Google OR-Tools (Linear & VRP) |
| **Pattern** | Clean Architecture + CQRS-style message routing |

---

### 🧭 Summary
PlannerView is more than a dashboard — it’s a **.NET 8 orchestration layer** uniting spatial data visualization, asynchronous optimization, and clean architecture.  
It demonstrates proficiency in **Blazor**, **SignalR**, **RabbitMQ**, and **OR-Tools** — transforming complex fleet data into intuitive, interactive, and optimized decisions.

---

## 🚚 Vehicle Routing Problem (VRP) Solver

The **VRP Solver** is the purpose-driven optimization engine of Planner.  
It converts fleet data into **optimized multi-vehicle routes** under real-world constraints using **Google OR-Tools**.

### 🔸 Input Example
```json
{
  "vehicles": [
    { "id": "Truck_1", "capacity": 40, "start": "Depot_A", "end": "Depot_A", "maxDistance": 180 },
    { "id": "Truck_2", "capacity": 40, "start": "Depot_A", "end": "Depot_A", "maxDistance": 180 }
  ],
  "customers": [
    { "id": "C1", "demand": 10, "serviceTime": 8, "timeWindow": [480, 600], "location": [-32.02, 116.90] },
    { "id": "C2", "demand": 20, "serviceTime": 10, "timeWindow": [540, 660], "location": [-32.33, 117.31] },
    { "id": "C3", "demand": 15, "serviceTime": 5, "timeWindow": [600, 720], "location": [-33.05, 116.89] },
    { "id": "C4", "demand": 5,  "serviceTime": 6, "timeWindow": [480, 720], "location": [-33.86, 116.33] }
  ],
  "depot": { "id": "Depot_A", "location": [-32.05, 116.93] },
  "distanceMatrix": [
    [0, 14, 23, 32, 45],
    [14, 0, 15, 24, 36],
    [23, 15, 0, 20, 28],
    [32, 24, 20, 0, 18],
    [45, 36, 28, 18, 0]
  ],
  "objectives": {
    "minimizeDistance": true,
    "balanceWorkload": true,
    "respectTimeWindows": true
  },
  "options": {
    "solver": "OR-Tools",
    "searchStrategy": "GUIDED_LOCAL_SEARCH",
    "timeLimitSeconds": 5
  }
}
```

### 🔸 Output Example
```json
{
  "routes": [
    {
      "vehicleId": "Truck_1",
      "stops": [
        { "id": "Depot_A", "arrival": 480 },
        { "id": "C1", "arrival": 495, "departure": 503 },
        { "id": "C2", "arrival": 540, "departure": 550 },
        { "id": "Depot_A", "arrival": 605 }
      ],
      "totalDistance": 96.4,
      "totalLoad": 30
    },
    {
      "vehicleId": "Truck_2",
      "stops": [
        { "id": "Depot_A", "arrival": 480 },
        { "id": "C3", "arrival": 600, "departure": 605 },
        { "id": "C4", "arrival": 660, "departure": 666 },
        { "id": "Depot_A", "arrival": 720 }
      ],
      "totalDistance": 101.8,
      "totalLoad": 20
    }
  ],
  "totalDistance": 198.2,
  "totalVehiclesUsed": 2,
  "computationTime": 0.472
}
```

---

### 🔸 Algorithmic Components (OR-Tools)
| Component | Function |
|------------|-----------|
| **RoutingIndexManager** | Maps indices to actual nodes |
| **RoutingModel** | Defines vehicles, nodes, and depots |
| **Cost Evaluator** | Computes travel cost/time |
| **Constraints** | Applies capacity, distance, and time windows |
| **Search Parameters** | Configures metaheuristics (Tabu, GLS, CP-SAT) |
| **Solution Formatter** | Converts solver output to `VrpResultMessage` |

---

### 🔸 Data Flow
```
PlannerView
   ↓ (HTTP POST)
Planner.API
   ↓ (publish)
RabbitMQ
   ↓ (consume)
Planner.Optimization.Worker (OR-Tools)
   ↓ (result)
SignalR → PlannerView
```

### 🔸 Visualization
![VRP schematic](docs/1.png)

*Figure: Example of two optimized routes from a central depot, generated by OR-Tools and visualized in PlannerView.*

![VRP schematic](docs/2.png)

*Figure: Example of two optimized routes from a central depot, generated by OR-Tools and visualized in PlannerView.*

---

### 🔸 Why OR-Tools Matters
- Proven optimization engine from **Google Research**  
- Supports **Constraint Programming**, **Linear Programming**, and **Metaheuristic Search**  
- Scalable, extensible, and high-performance  
- Ideal foundation for .NET 8 microservice optimization platforms  

**Planner + OR-Tools = Real-world optimization engineering**

---

## 🧩 LinearSolver — Foundational Introduction

The **LinearSolver** module is the educational base that validates Planner’s message flow:

```
Blazor Client → API → RabbitMQ → Optimization.Worker → SignalR → Blazor Client
```

The **LinearPoster** component allows posting raw JSON to `/api/Optimization/linearsolve`, verifying end-to-end communication before extending to VRP or scheduling modules.

---

## ⚙️ System Architecture

| Project | Role |
|----------|------|
| **Planner.BlazorApp** | UI & visualization (PlannerView, LinearSolver) |
| **Planner.API** | REST / SignalR communication hub |
| **Planner.Optimization.Worker** | Executes OR-Tools optimization |
| **Planner.Infrastructure** | Messaging, configuration, DI |
| **Planner.Contracts** | Shared DTOs & message definitions |

---

## 🚀 Summary

Planner demonstrates how a **.NET 8 developer** can integrate **Google OR-Tools** into a **clean, message-driven, real-time optimization platform** — uniting algorithmic logic, modern web architecture, and interactive visualization.

---

✅ *Built with .NET 8 · Blazor Server · SignalR · RabbitMQ · Google OR-Tools*
