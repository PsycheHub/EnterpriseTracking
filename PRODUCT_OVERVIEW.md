# Employee Activity Intelligence Platform

### Complete Sales & Product Documentation

---

## What Is It?

The **Employee Activity Intelligence Platform** is an all-in-one workforce monitoring and productivity analytics solution built for modern businesses. It gives managers and HR teams full visibility into how employees spend their working hours — which applications they use, how long they are active or idle, whether they attend meetings, complete learning courses, and whether they show up consistently every day.

The platform combines a **cloud-hosted REST API backend**, a **lightweight Windows desktop agent**, a **silent screen capture service**, and an **authentication pipeline service** — all working together seamlessly to deliver real-time and historical insights into employee productivity.

---

## The Problem It Solves

Remote and hybrid work has made it nearly impossible for employers to know:

- Whether employees are actually working during paid hours
- Which tools and applications teams spend the most time in
- Who attends daily and who is chronically late or absent
- Whether company-provided training is actually being completed
- How to prove compliance or activity in regulated industries

Traditional HR tools only tell you who is on the clock. This platform tells you **what they were doing every minute of the day**.

---

## Core Components

### 1. Cloud API Backend

A secure, scalable REST API that is the central brain of the platform. It stores all activity data, manages users, and powers the analytics dashboard. Built on ASP.NET Core with JWT authentication and role-based access control.

### 2. Desktop Activity Agent (Windows)

A silent system-tray application installed on each employee's Windows machine. It runs in the background and continuously monitors:

- Which application is currently active (foreground window)
- The exact window title (document name, browser tab, etc.)
- Whether the user is idle or actively working
- Whether they are in a video meeting (Zoom, Microsoft Teams)
- Whether they are completing a recognized learning course

Every session interval is uploaded to the backend. If the internet drops, the agent queues data locally and syncs automatically when reconnected.

### 3. Screen Capture Service

A companion Windows service that takes random screenshots at unpredictable intervals during the workday and uploads them as proof-of-activity. Screenshots are stored securely and linked to the specific user and application category. This provides visual verification on top of the time-tracking data.

### 4. Auth Pipeline Service

A Windows background service that handles secure authentication handoff between the desktop agent and the backend, using a named-pipe architecture. This ensures credentials are never exposed in plain text on the file system.

---

## Key Features

### Activity Tracking

- Tracks every active application a user opens and how long they spend in it
- Records precise start time, end time, and duration in seconds for every session
- Captures the full window title so you know not just the app, but the exact task (e.g., "Q2 Budget — Excel" vs "YouTube")
- Flags idle sessions separately from active work time
- Detects meetings on Zoom and Microsoft Teams automatically
- Detects completion of recognized learning and digital skills courses

### Attendance Management

- Automatically records the first login time of each employee every day — no manual clock-in required
- Full paginated attendance records per user with date-range filtering
- Attendance trend charts (Last 7 Days, Last 30 Days, Current Year)
- Export attendance records to Excel with a single click

### Productivity Dashboard

- Top metrics card: total active time, idle time, app usage summary
- Activity breakdown chart: see how time is split across categories (e.g., Productivity, Communication, Learning, Meetings)
- Trend analysis: visualize productivity over time for individual users or the whole team
- Filter any dashboard view by user, date range, or trend period

### Activity Categorization

- Group applications and window titles into custom categories (e.g., "Development Tools", "Communication", "Social Media", "Learning")
- Map specific app names to categories so every session is automatically classified
- Add icons and event types to categories for rich visual dashboards
- Full CRUD management: create, update, delete app-to-category mappings via the admin panel

### User Management

- Invite users via email — no self-registration; admins control who gets access
- Role-based access: separate permissions for Admin and User roles
- Suspend or unsuspend employees instantly without deleting their data
- Soft-delete support — user data is retained for compliance even after account removal
- Export user lists to Excel with filtering options
- View user growth metrics over time

### Voucher-Based Licensing

- Each active user must have a valid voucher linked to their account
- Vouchers have configurable expiry dates — you control the license period per user
- Extend a voucher's lifecycle without interrupting the user
- Link/unlink vouchers between users as your team changes
- Full voucher dashboard: track linked, unlinked, active, and expired licenses
- Export voucher data with date-range and status filters

### Screenshot Proof of Activity

- Random, unscheduled screenshots prevent employees from gaming the system
- Each screenshot is tied to a specific user, timestamp, application, and activity category
- Images are stored securely in the database with content type metadata
- Admins can review screenshot evidence through the API

### Offline Resilience

- The desktop agent has a built-in offline queue
- If an employee loses internet connection, all activity data is stored locally
- Data is automatically synced to the server once the connection is restored — no data is ever lost

### Security

- All API endpoints are protected with JWT Bearer authentication
- Role-based authorization enforces what each user can see or do
- Passwords are managed through ASP.NET Identity with hashed storage
- Named-pipe authentication prevents credential exposure on the local machine
- Soft-delete preserves audit trails without exposing deleted data

### Reporting & Export

- All major data sets are exportable to `.xlsx` Excel format:
  - User list exports
  - Attendance records exports
  - Voucher exports
- Exports support custom date ranges and status filters
- Files are timestamped automatically for record-keeping

---

## Who Is It For?

| Buyer                         | Use Case                                                                              |
| ----------------------------- | ------------------------------------------------------------------------------------- |
| **Remote-first Companies**    | Verify that remote employees are actively working during business hours               |
| **BPOs & Call Centers**       | Monitor agent productivity and application compliance                                 |
| **IT & Software Teams**       | Understand how developers split time between coding, meetings, and distractions       |
| **HR & Compliance Teams**     | Maintain attendance records, audit trails, and proof of work for regulated industries |
| **Training Departments**      | Track whether employees are completing mandatory digital courses                      |
| **Managed Service Providers** | White-label or resell the platform to multiple client organizations                   |

---

## Deployment Architecture

```
┌─────────────────────────────────────────────┐
│              Admin / Manager Web             │
│           (Dashboard & Reports)              │
└────────────────────┬────────────────────────┘
                     │ HTTPS / JWT
           ┌─────────▼──────────┐
           │   REST API Backend  │
           │   (Cloud / On-Prem) │
           └────┬──────────┬────┘
                │          │
    ┌───────────▼──┐   ┌───▼────────────────┐
    │  Activity    │   │  Screen Capture    │
    │  Agent       │   │  Service           │
    │  (Windows)   │   │  (Windows)         │
    └───────┬──────┘   └────────────────────┘
            │ Named Pipe
    ┌───────▼──────────────┐
    │  Auth Pipeline       │
    │  Service (Windows)   │
    └──────────────────────┘
```

- The **API backend** can be hosted on any cloud provider or on-premise server
- The **desktop components** are installed once per employee Windows machine
- The **admin dashboard** connects to the API over HTTPS from any browser

---

## Technology Stack

| Layer            | Technology                              |
| ---------------- | --------------------------------------- |
| Backend API      | ASP.NET Core, C#, Entity Framework Core |
| Authentication   | JWT Bearer Tokens, ASP.NET Identity     |
| Database         | SQL Server (with EF Core migrations)    |
| Desktop Agent    | .NET 8, Windows Forms (System Tray)     |
| Screen Capture   | .NET 8, Windows GDI+                    |
| IPC / Auth       | Named Pipes (Windows)                   |
| Containerization | Docker (Dockerfile included)            |
| Reporting        | OpenXML / Excel (.xlsx) export          |

---

## AI Intelligence Layer (Planned Roadmap)

The platform is designed to be extended with a suite of AI-powered agents that operate on top of the existing activity, attendance, and task data. Each capability below is a standalone AI Agent module that can be licensed separately or bundled as a complete AI Suite.

> **Status:** Planned — not yet implemented. These agents are the next major product milestone and will be scoped, architected, and built in a dedicated phase.

---

### AI Agent 1 — Daily Task Allocation Agent

Collates all pending and newly created tasks each day and intelligently distributes them among employees based on their roles, current workload, and task priorities. When integrated with an HR system, the agent gains access to employee profiles, leave status, and skill sets to make even smarter allocation decisions. No manual assignment is needed — the agent handles distribution automatically at the start of each business day.

**Value delivered:** Eliminates manual task allocation bottlenecks. Ensures no employee is overloaded while others are underutilized. Directly boosts team throughput.

---

### AI Agent 2 — Task Monitoring & Reminders Agent

Continuously monitors the progress of all assigned tasks in real time. When a task is delayed, inactive for an unusual period, or appears to have been abandoned, the agent automatically sends targeted reminders to the responsible employee and, where appropriate, escalates to their manager. Reminder cadence and escalation thresholds are configurable per organization.

**Value delivered:** Reduces task abandonment and deadline misses. Creates a self-managing accountability loop without requiring managers to manually chase employees.

---

### AI Agent 3 — Performance & KPI Analysis Agent

Analyses employee activity logs, completed tasks, turnaround times, idle ratios, attendance consistency, application usage patterns, and other relevant metrics collected by the platform. From this data, the agent computes individual and team-level KPI scores and generates structured monthly performance reports. Reports highlight high performers, flag employees who are underperforming, and surface trends that would otherwise require hours of manual analysis.

**Value delivered:** Replaces manual KPI spreadsheets. Gives HR and management objective, data-backed performance insights on a monthly cadence with zero manual effort.

---

### AI Agent 4 — Management Reporting Agent

Automatically compiles and delivers comprehensive monthly performance reports directly to designated management stakeholders. Reports cover team achievements, outstanding and overdue tasks, productivity trends, attendance summaries, and areas requiring management attention. Reports are formatted for executive consumption — clear, concise, and actionable — and can be delivered via email, in-app notification, or exported to PDF/Excel.

**Value delivered:** Saves senior management hours of report compilation. Ensures leadership always has an accurate, up-to-date view of workforce performance without relying on line managers to manually produce reports.

---

### AI Agent 5 — Continuous Task Follow-Up Agent

Maintains persistent tracking of every assigned task through its full lifecycle. When tasks remain unresolved past expected completion windows, the agent proactively follows up with the assigned employee, logs the follow-up activity, and where necessary escalates to supervisors or triggers a formal review flag. The agent maintains a full follow-up audit trail so management can see exactly when an issue was flagged and what action was taken.

**Value delivered:** Ensures nothing falls through the cracks. Creates a culture of accountability without requiring managers to micromanage every open item on the board.

---

### AI Suite Pricing

| Module                            | Price per Agent |
| --------------------------------- | --------------- |
| Daily Task Allocation Agent       | 25,000          |
| Task Monitoring & Reminders Agent | 25,000          |
| Performance & KPI Analysis Agent  | 25,000          |
| Management Reporting Agent        | 25,000          |
| Continuous Task Follow-Up Agent   | 25,000          |
| **Full AI Suite (all 5 agents)**  | **125,000**     |

> Agents are licensed on top of the base platform subscription. Each agent module is activatable independently, so clients can start with one agent and expand over time. Volume discounts available for enterprise contracts.

---

## Pricing Model Options

The platform supports several commercial pricing structures depending on your go-to-market strategy:

### Per-Seat Subscription

Charge per active user per month. The built-in **voucher system** maps perfectly to this model — each voucher represents one paid seat with a configurable expiry date. You can issue, extend, or revoke seats from the admin panel without touching code.

### Team Tiers

Bundle seats into fixed tiers (e.g., Starter: up to 10 users, Business: up to 50 users, Enterprise: unlimited). Manage tier limits through voucher quotas in the admin dashboard.

### Annual Contracts

Use the voucher expiry system to enforce annual licenses. Set all vouchers to expire 365 days after issue and automate renewal reminders.

### White-Label / Reseller

The platform has no embedded branding in the codebase. You can fully white-label it and sell it under your own brand to other businesses, charging a reseller margin on top of your hosting costs.

---

## What Makes It Different

1. **End-to-end system** — Unlike tools that only track time (toggle) or only take screenshots (teramind lite), this platform does both, plus attendance, plus learning detection, plus meeting tracking — all in one system.

2. **Voucher licensing built in** — You do not need a third-party billing system to control who has access. The voucher engine is already in the product.

3. **Offline-first agent** — The desktop agent queues data locally if there is no internet, so you never lose activity records for remote workers with unstable connections.

4. **Self-hostable** — Dockerfile is included. You can deploy the entire backend on your own infrastructure, giving enterprise clients the data sovereignty they need.

5. **Excel exports everywhere** — Every major data set can be exported to Excel — a feature that HR and finance teams demand and many competitors charge extra for.

6. **Role-based access** — Admins see everything; regular users see only their own data. This is ready out of the box.

7. **AI-ready architecture** — The platform's activity logs, attendance records, and task data form the exact dataset AI agents need to operate. The planned AI Intelligence Layer plugs directly into this data to deliver automated task allocation, KPI analysis, and management reporting — no separate data pipeline required.

---

## Competitive Positioning

| Feature                         | This Platform | Basic Time Trackers | Enterprise Suites |
| ------------------------------- | ------------- | ------------------- | ----------------- |
| App-level activity tracking     | Yes           | Yes                 | Yes               |
| Window title tracking           | Yes           | Partial             | Yes               |
| Idle detection                  | Yes           | Partial             | Yes               |
| Meeting detection (Zoom/Teams)  | Yes           | No                  | Yes               |
| Learning course detection       | Yes           | No                  | No                |
| Random screenshot capture       | Yes           | Some                | Yes               |
| Attendance auto-tracking        | Yes           | No                  | Yes               |
| Voucher / seat licensing engine | Yes           | No                  | No                |
| Offline queue                   | Yes           | No                  | Partial           |
| Self-hostable                   | Yes           | No                  | Rarely            |
| Excel export (all modules)      | Yes           | No                  | Yes               |
| Docker deployment               | Yes           | No                  | Sometimes         |
| Price                           | Yours to set  | $5–$12/user/mo      | $15–$30/user/mo   |

---

## Getting Started for Buyers

1. **Deploy the API** — Host the backend on your preferred cloud or on-premise server using the included Dockerfile
2. **Run database migrations** — Entity Framework Core migrations are included; the database is ready in minutes
3. **Create an admin account** — Use the seeder to bootstrap the first admin user
4. **Install the agent** — Deploy the Windows Activity Agent and Screen Capture Service to each employee machine (can be pushed via Group Policy or MDM)
5. **Create vouchers and invite users** — Issue vouchers for each seat, invite employees via email, and link vouchers to their accounts
6. **View the dashboard** — Connect your front-end dashboard to the API endpoints and start seeing data immediately

---

## Support & Customization

Because the full source code is yours, you can:

- Customize the activity categories to match your industry
- Add new learning course detection patterns
- Integrate with your existing HR system via the REST API
- Build a custom front-end dashboard using the API endpoints
- Add new export formats beyond Excel
- Implement multi-tenancy for serving multiple client organizations from a single deployment

---

_This documentation covers all components as implemented in the current codebase and is intended for commercial sales, partnership discussions, and investor presentations._
