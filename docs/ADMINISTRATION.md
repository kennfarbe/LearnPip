# Roles and administration (LP-10)

System roles are `user` (the default for every active account), `moderator`, and `admin`.
A moderator may use explicit moderation operations but does not gain access to private
questions, media, or unrelated groups. Group membership is scoped to one group:
`member` or `leader`. The group owner may manage that group's memberships. A leader
may assign `member` or `leader` within their group, but cannot grant system roles.

## First administrator

Create the intended account via the normal sign-in flow, then run the following
command in a trusted deployment environment with database access and the current
application image. Do not expose the bootstrap flag on the web process or put an
account ID into the web server's permanent environment:

```sh
Authentication__BootstrapAdminAccountId=<existing-account-uuid> dotnet LearnPip.Api.dll --bootstrap-admin
```

Run migrations first (`dotnet LearnPip.Api.dll --migrate`). The bootstrap is
transactional, takes a database lock, records an audit event, and refuses another
attempt even if all administrators are later removed outside the application.
The application forbids removing the last active administrator through its API.

## Administration API

Only an administrator with a valid session created in the last 15 minutes may call
`/api/v1/admin/*`. Sign in again to obtain a fresh session. Browser session cookies
require the configured same-origin request origin on writes; bearer tokens also work.

- `PUT` / `DELETE /api/v1/admin/accounts/{accountId}/roles/{moderator|admin}` grant or revoke a system role.
- `PUT /api/v1/groups/{groupId}/members/{accountId}/role` with `{"code":"member"}` or `{"code":"leader"}` manages a group-scoped role as the owner or leader.
- `PUT /api/v1/admin/settings/maintenance-notice` with `{"value":"..."}` updates the administrative notice (maximum 1000 characters).
- `GET /api/v1/admin/audit` returns the 100 newest changes. Audit entries include actor, target, old and new value, and UTC time. The bootstrap actor is null because it runs locally.

The notice is an administrative value only; consumers may choose whether to display it.
No arbitrary configuration keys or secret values can be written through the API.
