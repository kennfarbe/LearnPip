
# Admin web update architecture

Issues #75, #76 and #77 are implemented as one operational feature.

## Trust boundaries

The public API and web containers never receive a Docker socket, host shell, sudo capability, or
operator credentials. The API only writes validated update requests to a private, shared update
queue. A separate `learnpip-update-operator` process runs as the same unprivileged Linux account
that owns the rootless Docker daemon and consumes that queue.

The operator accepts only JSON jobs containing a stable SemVer tag (`vMAJOR.MINOR.PATCH`) and an
opaque job id. Repository, release API and archive URLs are constants for
`kennfarbe/LearnPip`. It re-fetches release metadata immediately before execution, rejects drafts
and prereleases, and then invokes `scripts/install-release.sh update --version <validated-tag>
--yes`. No URL, path or command supplied by the browser is executed.

## Release integrity

GitHub HTTPS is the distribution channel. The release installer rejects non-stable tags, verifies
the returned release tag, rejects unsafe archive paths, links and special files, and requires the
expected deployment files before activation. The operator revalidates the release immediately
before running the installer. Future signed release/checksum verification can be added without
changing the API contract.

## Update lifecycle

1. Admin checks a concrete stable release and sees release notes.
2. Admin explicitly confirms the installed and target versions. Cookie requests retain the global
   Origin/CSRF check and the update endpoint requires a fresh admin session.
3. API persists/audits one queued job. A database advisory lock prevents concurrent jobs.
4. Operator performs preflight checks, requires rootless Docker, checks free disk space, and runs a
   backup before the installer.
5. Installer performs pull/build-equivalent image acquisition, migration, restart and healthcheck.
   The `current` symlink changes only after the healthcheck succeeds.
6. Operator writes a redacted phase/status file. API imports that state and records the final audit
   result. A migration or healthcheck failure is reported as a partial/failed update, never success.
7. Browser reconnects and reloads the actual `LEARNPIP_VERSION` exposed by the restarted API.

Jobs and operator status live outside API/Web containers, so an API restart does not lose an
in-flight update. The operator uses a filesystem lock and atomic status files for idempotence.

## Backup and recovery

Every web update requires a successful consistent PostgreSQL backup first. The backup manifest
contains a SHA-256 and operational counts, never secrets. Configuration/secrets stay in the shared
installation directory and should additionally be protected by an external VM/host backup.
Database migrations are not assumed reversible: switching the `current` symlink is not a database
rollback. Restore remains an explicit administrator operation using the documented isolated restore
procedure; failed migrations/healthchecks require investigation before retrying.

## Privileges

Normal web updates never ask for sudo. Installation may need one-time privileges for dependencies,
network ports, or rootless Docker setup. Existing rootful installations must migrate to rootless
Docker; membership in the rootful `docker` group and wildcard sudoers rules are explicitly not a
supported web-update design.
