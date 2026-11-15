---
name: secure-docker
description: Security checklist for service and app Dockerfiles — non-root user, base image pinning, secrets in layers, .dockerignore, multi-stage builds, runtime hardening, image scanning.
---

# Secure Docker

Use for any `Dockerfile` / `.dockerignore` change. Pair with `secure-github-actions`, `azure-container-apps`.

## Rules

- **Non-root.** Every final stage ends with a non-root `USER` (all service and web images do; keep it that
  way in any new Dockerfile).
- **Pin the base.** Specific tag (`python:3.13-slim`, `node:20-alpine`) — prefer `@sha256:` digest
  for prod and refresh via Dependabot/Renovate. No `latest`. Use slim/distroless final images.
- **Multi-stage.** Build tooling, compilers, dev deps, source and tests stay in the build stage; the run
  stage copies only the venv/`.next/standalone`/artifacts.
- **No secrets in layers.** No `ENV`/`ARG` with credentials; `COPY . .` requires a strict `.dockerignore`
  (`.env*`, `.git`, `*.pem`, `*.key`, `terraform.tfvars`, `node_modules`, `tests`, `.venv`). If a build
  needs a secret, use `RUN --mount=type=secret`. Deleting a file in a later layer does not remove it.
- **Locked installs.** `npm ci --omit=dev`, `pip install --no-cache-dir --require-hashes -r` /
  `uv sync --frozen --no-dev`; no `curl | sh`; checksum verify downloaded binaries.
- **Minimal surface.** Only the app port exposed; use a health check appropriate to the runtime platform; no SSH, package
  managers left with caches; `PYTHONDONTWRITEBYTECODE`, read-only root FS where possible.
- **No privileged runtime.** No `--privileged`, docker.sock mounts, host network, or added capabilities
  (also applies to any local compose files).
- **Reproducible & scanned.** Trivy/Grype or Defender for Containers (ACR) scanning gates publish on HIGH/CRITICAL
  fixable CVEs; SBOM + provenance in CI (`secure-github-actions`).

## Service specifics

- The `agents-video` image adds `ffmpeg`: install from distro packages, pin versions, drop build tools
  afterwards; treat media inputs as untrusted (no `shell=True`, argv lists only).
- Web images: `output: "standalone"`, run as `nextjs` user, no `.env` copied; public env injected at
  deploy, never baked.
- On managed container platforms, ensure the application listens on the configured target port; don't use privileged ports (<1024).

## Review checklist

- [ ] Final-stage `USER` is non-root
- [ ] Base pinned; multi-stage; slim final image
- [ ] `.dockerignore` blocks env/keys/git; no secrets in `ENV`/`ARG`/`RUN`
- [ ] Locked, hash-/lockfile-verified installs
- [ ] Image scan clean or waived with reason

## Provenance

`UnitOneAI/SecuritySkills` container-security (CIS Docker v1.6 / NIST 800-190, MIT),
`igorwarzocha/opencode-workflows` security-docker (no license file — concepts only, wording original),
`agamm/claude-code-owasp` (MIT).
