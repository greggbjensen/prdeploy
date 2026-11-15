---
name: azure-container-apps
description: "Azure Container Apps reference: infrastructure-as-code patterns, managed-identity ACR pulls, Key Vault secret references, health probes, scaling rules, revisions and traffic splitting, VNet and ingress, custom domains and managed TLS, and day-2 operations. Use when provisioning or operating an application on Azure Container Apps."
license: MIT
---

# Azure Container Apps

Azure Container Apps is a serverless container hosting platform for web applications, APIs, background workers, and scheduled jobs. The reference material includes Azure resource patterns and operational guidance; use the infrastructure-as-code tool selected by the project.

## References

| Need                                                      | File                                                |
| --------------------------------------------------------- | --------------------------------------------------- |
| Terraform resources, two-phase image rollout, `AcrPull`   | [terraform.md](references/terraform.md)             |
| Env vars and Key Vault secret references                  | [environment.md](references/environment.md)         |
| Liveness, readiness, startup probes                       | [health-probes.md](references/health-probes.md)     |
| HTTP and queue scaling                                    | [scaling.md](references/scaling.md)                 |
| Revisions, blue/green, canary, rollback                   | [revisions.md](references/revisions.md)             |
| VNet, ingress modes, custom domains, TLS, IP restrictions | [networking.md](references/networking.md)           |
| Restart, exec, logs, KQL, secret rotation                 | [day2-operations.md](references/day2-operations.md) |

## Required supporting resources

Container Apps Environment, Azure Container Registry, Log Analytics workspace, Application Insights.

## Common configurations

| Workload         | Ingress  | Min replicas          | Scaling              |
| ---------------- | -------- | --------------------- | -------------------- |
| API or web app   | External | 1 (avoid cold starts) | HTTP                 |
| Internal service | Internal | 0 or 1                | HTTP                 |
| Scheduled work   | None     | Container Apps Job    | Cron or event-driven |

Prefer managed identity for registry and service access, and use Key Vault references for secrets where appropriate. Follow the repository's naming, infrastructure, and deployment policies.
