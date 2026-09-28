# Releases and versioning

LearnPip plans to use **Semantic Versioning** for project releases and **semantic-release** to automate version calculation, changelog generation, tags, and GitHub Releases.

Until the release workflow is implemented, changes should use [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) in pull-request titles and commit messages:

| Change | Example | Intended version effect |
| --- | --- | --- |
| New capability | `feat(web): add spaced practice` | Minor |
| Bug fix | `fix(api): validate answers` | Patch |
| Breaking change | `feat(api)!: replace answer endpoint` | Major |
| Documentation, CI, maintenance | `docs: explain setup`, `ci: validate pull requests` | No release by itself |

The CI checks the PR title and each commit subject. For semantic-release to calculate releases reliably, the merge commit history must preserve these messages. When using squash merge, keep the validated PR title as the squash commit title. Do not hand-create version tags or GitHub Releases after automated release publishing is enabled.

## Planned release shape

Initially, one repository-wide version will identify the compatible API, worker, and web release. Container images can use the same version tag. Separate component version streams can be considered later if the components gain independent release cycles.

The semantic-release publishing workflow is intentionally deferred until buildable API and web projects exist. It must run only after trusted changes reach the protected `main` branch, use narrowly scoped release permissions, and never run with publishing credentials for fork pull requests. The starting version and prerelease channel must be decided before the first automated release.
