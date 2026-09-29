# Third-party license review (2026-09-29)

Reviewed direct dependencies, the complete npm lockfile (338 packages), Docker images, and repository assets before LP-22. No component in this inventory requires a paid or special proprietary license for the current use. Preserve third-party notices when distributing built artifacts and repeat the review on upgrades.

| Component | License | Source and conditions |
| --- | --- | --- |
| .NET 10, ASP.NET Core, EF Core, Microsoft Extensions and OpenAPI | MIT | [Official license information](https://github.com/dotnet/core/blob/main/license-information.md); retain copyright and license notices. |
| Npgsql, PostgreSQL 18 | PostgreSQL License | [Npgsql metadata](https://github.com/npgsql/npgsql/blob/main/Directory.Build.props), [PostgreSQL license](https://www.postgresql.org/about/licence/); retain notices. |
| SkiaSharp 3.119.4 and Linux native assets | MIT, bundled native third-party notices | [SkiaSharp license](https://github.com/mono/SkiaSharp/blob/main/LICENSE.md); include applicable native notices if redistributed. |
| Angular 22, Angular build/CLI, Prettier | MIT | [Angular](https://github.com/angular/angular/blob/main/LICENSE); retain notices. |
| RxJS, TypeScript | Apache-2.0 | [RxJS](https://github.com/ReactiveX/rxjs/blob/master/LICENSE.txt), [TypeScript](https://github.com/microsoft/TypeScript/blob/main/LICENSE.txt); retain applicable notices. |
| Caddy 2 | Apache-2.0 | [License](https://github.com/caddyserver/caddy/blob/master/LICENSE); retain notices. |
| nginx (official Alpine image) | BSD-2-Clause | [License](https://github.com/nginx/nginx/blob/master/LICENSE); retain notices. |
| xUnit, coverlet, .NET test SDK | Apache-2.0 / MIT | Test-only tools; retain applicable notices if redistributed. |

The `frontend/web/package-lock.json` identifies 338 transitive packages: 289 MIT, 15 ISC, 8 Apache-2.0, 7 BSD-2-Clause, 4 BSD-3-Clause, 1 0BSD, 1 BlueOak-1.0.0, 1 `(Apache-2.0 AND BSD-3-Clause)`, 12 MPL-2.0 (Lightning CSS platform variants), and 1 CC-BY-4.0 (`caniuse-lite`, browser compatibility data). Lightning CSS is build tooling; preserve its notices and source availability if redistributing modified copies. Credit caniuse-lite data if redistributed. The lockfile records the exact names and versions. No external font, icon library, or stock imagery is referenced by the app source.

LearnPip program code has its own AGPL-3.0-only terms; see [LICENSING.md](LICENSING.md). Its source availability obligations for network deployment are separate. User-uploaded images and public question content require a separate rights review; this dependency inventory does not establish their reuse rights.
