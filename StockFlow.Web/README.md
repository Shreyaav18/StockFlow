# StockFlow WMS

A warehouse management system built with .NET 8 MVC for tracking bulk shipments through a recursive processing pipeline — from raw intake to final retail units.

---

## What it does

StockFlow lets warehouse teams receive bulk shipments, break them down into smaller output items, and track every step of that breakdown as a parent-child tree. Each output can be processed further or left as final. Managers approve or reject every processing action before it is committed.

**Core features:**

- Receive bulk shipments and log them against a master item catalog
- Process any shipment or processed item into multiple child items with weight tracking
- Weight validation — child weights can never exceed the parent
- Recursive tree — items can be processed to any depth, visualised as a collapsible hierarchy
- Approval workflow — Staff process, Managers approve or reject, Admins oversee everything
- Real-time notifications via SignalR when processing completes or approval is needed
- Nightly Hangfire job flags shipments pending for over 24 hours
- Reports — daily summaries, date range reports, per-item breakdowns
- Export any tree or report to PDF or Excel
- Unified search across items, shipments, and processed records
- Full audit trail — every action logged with user and timestamp

**Role hierarchy:**

| Role | What they can do |
|---|---|
| Staff | Receive shipments, execute processing, view tree |
| Manager | Everything Staff can + approve/reject, create items, view reports, export |
| Admin | Everything + manage users, assign roles, view audit logs |

---

## Tech stack

- .NET 8 MVC — Razor Views, Controllers, Services, Repositories
- SQL Server 2022 — EF Core Code First with migrations
- SignalR — real-time notifications
- Hangfire — background job for stale shipment alerts
- Serilog — structured logging to file and console
- Bootstrap 5 + Vanilla JS + jQuery — frontend
- ApexCharts — dashboard and report charts
- iText7 + ClosedXML — PDF and Excel export
- FluentValidation — server-side input validation
- Docker + Docker Compose — one command local setup

---

## Running locally

**Requirements:**
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running
- That is it — nothing else needed

**Steps:**
```bash
git clone https://github.com/your-username/stockflow-wms.git
cd stockflow-wms
docker-compose up --build
```

Open [http://localhost:8080](http://localhost:8080)

The first run takes 2–3 minutes — SQL Server needs to initialise, then migrations run, then seed data loads automatically. Subsequent runs start in seconds.

---

## Demo credentials

| Role | Email | Password |
|---|---|---|
| Admin | admin@stockflow.com | Admin@1234 |
| Manager | manager@stockflow.com | Manager@1234 |
| Staff | staff@stockflow.com | Staff@1234 |

---

## How to use it

**Receiving a shipment**
Sign in as Staff → Shipments → Receive shipment → select an item, enter weight → submit.

**Processing a shipment**
Open any Pending shipment → Process items → add child items with output weights → submit. Weight is validated automatically. Items go to Pending approval.

**Approving processed items**
Sign in as Manager → Processing → Pending approvals → Approve or Reject with a reason. Staff are notified in real time.

**Viewing the tree**
Open any shipment → View tree. The full parent-child hierarchy is shown as a collapsible tree. Approved nodes can be processed further directly from the tree.

**Exporting**
From any shipment detail or the Reports page → Export PDF or Export Excel.

**Hangfire dashboard**
[http://localhost:8080/jobs](http://localhost:8080/jobs) — Admin only.

---

## Stopping the app
```bash
docker-compose down
```

To also delete the database volume:
```bash
docker-compose down -v
```

---

## Notes

- Logs are written to `logs/stockflow-YYYYMMDD.log` inside the container
- The corn starch shipment is seeded 26 days old — it will appear as Stale immediately
- The salt shipment has a pre-built two-level processing tree ready to explore
- Sidebar collapse state is saved per browser session