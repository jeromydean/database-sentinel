<div align="center">
  <img src="src/images/logo.svg" alt="Database Sentinel" width="120" />
</div>

# BlueFence Database Sentinel

A database monitoring application. Planned capabilities include real-time health and performance monitoring, alerting, dashboards, historical metrics and trending, and visibility into blocking, deadlocks, and query performance. This project is **work in progress**; features and architecture are being refined as we go.

### Tech stack

[![Microsoft SQL Server](https://img.shields.io/badge/Microsoft%20SQL%20Server-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?style=flat-square&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Keycloak](https://img.shields.io/badge/Keycloak-FF6900?style=flat-square&logo=keycloak&logoColor=white)](https://www.keycloak.org/)
[![.NET](https://img.shields.io/badge/.NET-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-8322F6?style=flat-square)](https://avaloniaui.net/)

## 🎯 Goals

- Monitor database servers (health, performance, availability).
- Store and query monitoring data for dashboards, alerts, and historical analysis.
- Provide a secure, multi-tenant–friendly experience with centralized identity.

## 🧩 Planned Stack & Integrations

- **Target databases**: [Microsoft SQL Server](https://www.microsoft.com/sql-server) first; support for additional database platforms may be added later.
- **Monitoring storage**: [PostgreSQL](https://www.postgresql.org/) for storing metrics, events, and related monitoring data.
- **Identity**: [Keycloak](https://www.keycloak.org/) as the identity provider (IDP) for authentication and single sign-on.

## 📁 Repository Structure

- `src/BlueFence.DatabaseSentinel.Api` – ASP.NET Core API (current entry point).
- `src/BlueFence.DatabaseSentinel` – Avalonia-based user interface.

## 🚀 Getting Started

Requirements and run instructions will be added as the solution stabilizes.

## 📄 License

MIT.
