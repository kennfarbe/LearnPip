# [1.12.0](https://github.com/kennfarbe/LearnPip/compare/v1.11.0...v1.12.0) (2026-10-03)


### Bug Fixes

* **identity:** complete OIDC provider configuration ([d52f785](https://github.com/kennfarbe/LearnPip/commit/d52f785acd3d15e83236df81700c25de6979d011))
* **identity:** correct escaped newline character literals ([67a4db4](https://github.com/kennfarbe/LearnPip/commit/67a4db473d66c23cc459ed106055a590e6ffba79))
* **identity:** pin Apple issuer and require explicit Microsoft tenant ([0ea5ced](https://github.com/kennfarbe/LearnPip/commit/0ea5ced7bd6ea2365f5399a1b588936bb2127109))
* **identity:** satisfy Facebook OAuth analyzer checks ([f23265d](https://github.com/kennfarbe/LearnPip/commit/f23265dd9585974f0818b514aba0f5e43e360c56))
* **identity:** satisfy GitHub OAuth analyzer checks ([a965af1](https://github.com/kennfarbe/LearnPip/commit/a965af14e6cef45d880e3861c19ac8542acbf68b))
* **identity:** use single escape in C# character literals ([42b2fba](https://github.com/kennfarbe/LearnPip/commit/42b2fba04a3efeaa722a782a5ec017f0ccdd0302))
* **identity:** validate Microsoft issuer against tenant strategy ([1a74bc2](https://github.com/kennfarbe/LearnPip/commit/1a74bc21fb88bb64035bbf6ad65bfff9f021a556))


### Features

* **identity:** add authenticated provider unlink endpoint ([4b66336](https://github.com/kennfarbe/LearnPip/commit/4b66336f633c40b469b2429c12e597c1c8b74fa1))
* **identity:** centralize safe external account resolution and session issuance ([b43cbd7](https://github.com/kennfarbe/LearnPip/commit/b43cbd7c0f5cfeebc553d1c30df35f48f1d66113))
* **identity:** configure Facebook OAuth secret in production Compose ([4758c68](https://github.com/kennfarbe/LearnPip/commit/4758c68ea094bc14437e4a1501e1fe6d4a754847))
* **identity:** create optional GitHub OAuth secret on legacy upgrades ([991afe1](https://github.com/kennfarbe/LearnPip/commit/991afe19b10d28f5cf4c5fbb4fd163100b9d3c2f))
* **identity:** explicitly link GitHub OAuth identity to current account ([86a0560](https://github.com/kennfarbe/LearnPip/commit/86a0560e23315d5d38b1122bb85217f0d4d8dc0c))
* **identity:** expose optional Facebook sign-in ([5f219f6](https://github.com/kennfarbe/LearnPip/commit/5f219f6a0b933b9387d015d3578e1748ce971f5d))
* **identity:** handle Apple form_post with cross-site correlation and nonce cookies ([bea1b04](https://github.com/kennfarbe/LearnPip/commit/bea1b042a16063ea55e4fb62c7bbf5cd41a50537))
* **identity:** implement GitHub OAuth with verified stable user ID and no email merge ([787a6c1](https://github.com/kennfarbe/LearnPip/commit/787a6c15bea583da9b50ab6a2b946bde272aee1b))
* **identity:** implement separate Facebook OAuth with app-scoped stable ID ([cc79a01](https://github.com/kennfarbe/LearnPip/commit/cc79a01141beae2fb32486eb487ea08181f270c1))
* **identity:** offer configured GitHub OAuth sign-in ([e728c7d](https://github.com/kennfarbe/LearnPip/commit/e728c7d704807e8a06d06d996ced770b6554158a))
* **identity:** preserve optional Facebook OAuth secrets on upgrades ([0b49cc8](https://github.com/kennfarbe/LearnPip/commit/0b49cc8962c39034d60bcfb8c14d2a3c6566bb2a))
* **identity:** protect production GitHub OAuth client secret ([01ab23e](https://github.com/kennfarbe/LearnPip/commit/01ab23ec7383916bc90db8563b0acd32891634d0))
* **identity:** register optional Facebook OAuth ([07aaa95](https://github.com/kennfarbe/LearnPip/commit/07aaa951e976c47b433e16a813573b69992c617b))
* **identity:** register optional GitHub OAuth ([6c55ed5](https://github.com/kennfarbe/LearnPip/commit/6c55ed59f0fd0c88a65ac01f5431742309dc7773))
* **identity:** require active session for explicit Facebook account link ([1e9b0fd](https://github.com/kennfarbe/LearnPip/commit/1e9b0fdc5b2372f4d4c63563b273507772446833))
* **identity:** safely unlink external sign-in providers ([c75369c](https://github.com/kennfarbe/LearnPip/commit/c75369cdec56bb8b1e3fa3710d517cc9cb99a080))

# [1.11.0](https://github.com/kennfarbe/LearnPip/compare/v1.10.0...v1.11.0) (2026-10-03)


### Bug Fixes

* **identity:** initialize newly introduced optional provider secrets on upgrades ([d009ae0](https://github.com/kennfarbe/LearnPip/commit/d009ae01933a18e23766f977416d11088428c836))
* **identity:** mount persistent keys for migration startup as well ([610cad5](https://github.com/kennfarbe/LearnPip/commit/610cad51b528db93a958ca4e135922fb12840393))
* **identity:** require valid HTTPS issuer before advertising OIDC provider ([48f1ea5](https://github.com/kennfarbe/LearnPip/commit/48f1ea5d4fc5cbac3d6c489e218bc5119e4aec68))
* **identity:** reserve generic OIDC registration for supported protocol providers ([2d85a7e](https://github.com/kennfarbe/LearnPip/commit/2d85a7ea39b702720d74972dcdf9c6627f42e3e1))


### Features

* **identity:** add per-provider login and explicit account linking routes ([0900c10](https://github.com/kennfarbe/LearnPip/commit/0900c100285ec7a50b5cb67b51905116378cd7ad))
* **identity:** initialize empty provider secret files without rotating existing secrets ([00498a0](https://github.com/kennfarbe/LearnPip/commit/00498a025ab12bde6d9492c6d15c10bd3cca139c))
* **identity:** mount persistent Data Protection volume in production ([b87b32c](https://github.com/kennfarbe/LearnPip/commit/b87b32c697547dd93d2c0077ebad53f9ef298a5f))
* **identity:** persist production Data Protection keys across API restarts ([e8d8eed](https://github.com/kennfarbe/LearnPip/commit/e8d8eed2e519d1a6aceb82112fcbcc37bf2ed0e7))
* **identity:** register separately configured OIDC authentication schemes ([129fad8](https://github.com/kennfarbe/LearnPip/commit/129fad853cef0618ccc11a176dd51e01268dd2f0))
* **identity:** wire optional Apple and Microsoft OIDC configuration into production Compose ([54c82f0](https://github.com/kennfarbe/LearnPip/commit/54c82f00cbb696f0d4de89219e4f2807db8a61b9))

# [1.10.0](https://github.com/kennfarbe/LearnPip/compare/v1.9.2...v1.10.0) (2026-10-03)


### Features

* **catalog:** select validated offline questions with referenced media ([a67a397](https://github.com/kennfarbe/LearnPip/commit/a67a3976497db4efd492710b3bdc1fd0aa521900))

## [1.9.2](https://github.com/kennfarbe/LearnPip/compare/v1.9.1...v1.9.2) (2026-10-03)


### Bug Fixes

* **catalog:** reject undeclared question document fields ([bd7c774](https://github.com/kennfarbe/LearnPip/commit/bd7c77467bfdd4c3baddf38a6dc0219e66b20ece))

## [1.9.1](https://github.com/kennfarbe/LearnPip/compare/v1.9.0...v1.9.1) (2026-10-03)


### Bug Fixes

* **catalog:** reject unknown fields in offline draft contract ([d76ec4b](https://github.com/kennfarbe/LearnPip/commit/d76ec4b4c16b5205066218736094bd36283a3214))

# [1.9.0](https://github.com/kennfarbe/LearnPip/compare/v1.8.0...v1.9.0) (2026-10-03)


### Features

* **catalog:** write validated offline draft snapshot atomically ([0e74e30](https://github.com/kennfarbe/LearnPip/commit/0e74e303ff31284d6c0a0f6b50f004d196a83b1b))

# [1.8.0](https://github.com/kennfarbe/LearnPip/compare/v1.7.1...v1.8.0) (2026-10-03)


### Bug Fixes

* **catalog:** assert reader validator exception from its loaded module ([6f3942b](https://github.com/kennfarbe/LearnPip/commit/6f3942b21ee1afa384296e9cfaca37b9c59b95f0))
* **catalog:** define reader exception used by negative contract tests ([065d7b4](https://github.com/kennfarbe/LearnPip/commit/065d7b4e21da473c469d970ae964fc4890185e5e))
* **catalog:** use immutable NamedTuple snapshot compatible with script loading ([ad256d6](https://github.com/kennfarbe/LearnPip/commit/ad256d6a1209c5b74a809254313b63a4064a527d))


### Features

* **catalog:** add validated read-only offline ZIP reader ([83912fb](https://github.com/kennfarbe/LearnPip/commit/83912fb156621753691b9a6dea0c819476a87e79))

## [1.7.1](https://github.com/kennfarbe/LearnPip/compare/v1.7.0...v1.7.1) (2026-10-03)


### Bug Fixes

* **catalog:** require leading manifest and non-executable media types ([2d3c1bc](https://github.com/kennfarbe/LearnPip/commit/2d3c1bcba6b0217a3f4254b95422578e057ac767))

# [1.7.0](https://github.com/kennfarbe/LearnPip/compare/v1.6.0...v1.7.0) (2026-10-03)


### Bug Fixes

* **security:** accept valid NuGet projects without reported frameworks ([1ede4ba](https://github.com/kennfarbe/LearnPip/commit/1ede4bafb97ae1f365f93d8b793855e05ab2c0ab))
* **security:** never waive invalid dev audit reports ([561572a](https://github.com/kennfarbe/LearnPip/commit/561572a103248dab3d57d697ee2c7d9d9ce3d48c))


### Features

* **moderation:** add purpose-bound audited private question inspection ([202fdd9](https://github.com/kennfarbe/LearnPip/commit/202fdd918465e97621b5c91aebbee164a4f78c9e))
* **moderation:** define audited inspection requests ([29a393b](https://github.com/kennfarbe/LearnPip/commit/29a393b4e379e1af0c7f8f973cfcb999f3a9b1e6))
* **moderation:** define audited inspection requests ([475d80b](https://github.com/kennfarbe/LearnPip/commit/475d80bca99ba1db2c91208ec2ae96e107ca415a))
* **release:** add dependency-free conventional commit release planner ([491d33c](https://github.com/kennfarbe/LearnPip/commit/491d33c42eb32b1ffd6ead0577e694e93cff1e91))
* **release:** replace semantic-release job with dependency-free release publishing ([c777a14](https://github.com/kennfarbe/LearnPip/commit/c777a14708abf32ad5efa9cf6ad53b14018ca164))
* **security:** classify scan reports without exposing advisory details ([57b0719](https://github.com/kennfarbe/LearnPip/commit/57b07198a004fc92c5b234a9d798bc4bd9b167a4))
* **security:** separate informational dev audit from blocking runtime dependency audit ([fdbff28](https://github.com/kennfarbe/LearnPip/commit/fdbff280e7827ed971a9d403ec62a79d115671f0))


### Reverts

* **release:** remove replacement release planner ([3afd09f](https://github.com/kennfarbe/LearnPip/commit/3afd09f35083ef57c15e3942b6bb6b6845ef92df))
* **release:** restore existing semantic-release configuration ([0e1c62d](https://github.com/kennfarbe/LearnPip/commit/0e1c62d2c64997865c0f2c649759ec8ab6c00397))
* **release:** restore existing semantic-release configuration ([0d6e29b](https://github.com/kennfarbe/LearnPip/commit/0d6e29b6b7226c79cbc7b92da78f6ebeb2b7a340))
* **release:** restore existing semantic-release configuration ([71616c1](https://github.com/kennfarbe/LearnPip/commit/71616c1752d3bd8d57b754e56075ba4e4b29c634))
* **release:** retain semantic-release workflow as requested ([1cfe992](https://github.com/kennfarbe/LearnPip/commit/1cfe992c8cd8035858e02ab47a33f57ad0268940))

# [1.6.0](https://github.com/kennfarbe/LearnPip/compare/v1.5.1...v1.6.0) (2026-10-03)


### Features

* **catalog:** add offline ZIP integrity and semantic validator ([8956ef0](https://github.com/kennfarbe/LearnPip/commit/8956ef07f29b1323d8f280c32f413dfbd5ffca58))

## [1.5.1](https://github.com/kennfarbe/LearnPip/compare/v1.5.0...v1.5.1) (2026-10-03)


### Bug Fixes

* **ci:** classify Markdown in code directories as documentation only ([ac23a51](https://github.com/kennfarbe/LearnPip/commit/ac23a51dc6507eeae4f792d3a982a6688039518a))
* **ci:** run classifier tests directly and validate step action pins ([8144936](https://github.com/kennfarbe/LearnPip/commit/8144936d670ca64658795838447e1593801c5431))
* **ci:** show failed stack diagnostics and set test password once ([2f883e3](https://github.com/kennfarbe/LearnPip/commit/2f883e35a99ca5ead7503c54a9ac31284ac844c6))
* **dev:** use supported PostgreSQL 18 volume mount ([5eda940](https://github.com/kennfarbe/LearnPip/commit/5eda9409ecdef3ac5d3c94cd7248deb10e2928d3))

# [1.5.0](https://github.com/kennfarbe/LearnPip/compare/v1.4.0...v1.5.0) (2026-10-03)


### Bug Fixes

* **web:** initialize system theme across workspaces ([e48eae0](https://github.com/kennfarbe/LearnPip/commit/e48eae0095b14b8b85f2fc2706fa1ec8961c206f))


### Features

* **api:** expose current workspace capabilities ([7dacae3](https://github.com/kennfarbe/LearnPip/commit/7dacae319ee768923c47a75a6c97a0f3bcb6db79))
* **web:** introduce focused application workspaces ([6c86f61](https://github.com/kennfarbe/LearnPip/commit/6c86f61b67dadc4c3b6b7ab5d27c948422c0986e))

# [1.4.0](https://github.com/kennfarbe/LearnPip/compare/v1.3.1...v1.4.0) (2026-10-03)


### Bug Fixes

* **deploy:** mount update operator directories from shared absolute path ([15176c3](https://github.com/kennfarbe/LearnPip/commit/15176c34c42c5785e5a7e5336ec1a5a10aa76d2e))
* **deploy:** pass update operator queue, status and version into API ([53ab634](https://github.com/kennfarbe/LearnPip/commit/53ab6346e526223eaf1a39c5d45aab2eea676d99))
* **domain:** restore LearningContent after splitting its collection file ([b266037](https://github.com/kennfarbe/LearnPip/commit/b266037594e7457015c2531520fb439be900b69a))
* **installer:** persist shared path for mounted operator directories ([24d1d77](https://github.com/kennfarbe/LearnPip/commit/24d1d77bd67422cb0f3f0f864bf13819e19b4cf1))
* **tests:** avoid hiding inherited authentication Scheme property ([0127226](https://github.com/kennfarbe/LearnPip/commit/012722667acdac90f9a69a2bb51a959db5b24ab5))
* **tests:** use named authentication test scheme in ticket ([c31e2e7](https://github.com/kennfarbe/LearnPip/commit/c31e2e7aabe4a2543f6726ac39be42b24bcbbea5))
* **updates:** serialize queue writes and fail safely if operator is unavailable ([701765d](https://github.com/kennfarbe/LearnPip/commit/701765d1d9c04b9d86c92855697d67bf2f276309))
* **updates:** use consistent camel-case JSON across API and operator ([ea66ffa](https://github.com/kennfarbe/LearnPip/commit/ea66ffaa7ea07caf94675d394308a05bf4d7b73d))
* **web:** align ESLint dependency versions ([669f8ee](https://github.com/kennfarbe/LearnPip/commit/669f8eee999eacd7dc06f940749dc0d34c000ce4))
* **web:** associate each translation input with its label ([71efbc9](https://github.com/kennfarbe/LearnPip/commit/71efbc97899ca8ac1a6bbec9accd511ecb2d3ed2))
* **web:** restore Angular dependency pins and nested ESLint locks ([1ea0081](https://github.com/kennfarbe/LearnPip/commit/1ea008144decee4c15c70f23b1845cc2cebe865d))
* **web:** synchronize ESLint lockfile dependency graph ([caea7d2](https://github.com/kennfarbe/LearnPip/commit/caea7d2b25e3f766eeb9b27fd4f17be04139d9ef))
* **web:** use native accessible dialog close control ([ae1541f](https://github.com/kennfarbe/LearnPip/commit/ae1541fa81f4e505e7a52625eb8d33b18f5ecd5e))


### Features

* add admin release and update service ([10a82d6](https://github.com/kennfarbe/LearnPip/commit/10a82d69eba9b15f61db109f78b33407a0f7ac6d))
* add responsive admin update UI ([bc9b3c4](https://github.com/kennfarbe/LearnPip/commit/bc9b3c4317f0341922a0447c5e23d70db30d1260))
* add rootless restricted update operator ([15b5443](https://github.com/kennfarbe/LearnPip/commit/15b5443c618248848e56a8a529b7f0e111db3cfb))
* add rootless update operator service ([605f9d2](https://github.com/kennfarbe/LearnPip/commit/605f9d22f4ed682f87aaeb5296c2fc19b07b6fcf))
* enable scheduled stable release checks ([e3161d2](https://github.com/kennfarbe/LearnPip/commit/e3161d2e84068a9e1eb159c08c5b2a5052a2d4c1))
* expose admin update endpoints ([7bcee12](https://github.com/kennfarbe/LearnPip/commit/7bcee12a3a6c7e9214fa90d4599316f785c2f3c5))
* expose release version and private update queue ([d667411](https://github.com/kennfarbe/LearnPip/commit/d667411a1958006e5b791d2bfeb774054bc0be84))
* prepare private web update state directories ([5348efd](https://github.com/kennfarbe/LearnPip/commit/5348efd742df8eeb2d08937ee6583cfcf90670ca))
* register admin update UI ([b900412](https://github.com/kennfarbe/LearnPip/commit/b9004122b80e4aef10a957bd906f0fc00af2ebc6))
* register secure update services and rate limit ([b964979](https://github.com/kennfarbe/LearnPip/commit/b9649795f3e29b5c9df1c332c95dac58972609da))
* run update checks without an open browser ([d098c18](https://github.com/kennfarbe/LearnPip/commit/d098c1839b0c78293b5970695fdc6e0450a00bce))
* show update status to administrators ([8febcb1](https://github.com/kennfarbe/LearnPip/commit/8febcb1cd785d53d3121fbfc02125966370429d8))
* support server-side scheduled update checks ([9349c52](https://github.com/kennfarbe/LearnPip/commit/9349c52e0292b315f7daec3b88e182139c2da6c3))

## [1.3.1](https://github.com/kennfarbe/LearnPip/compare/v1.3.0...v1.3.1) (2026-10-01)


### Bug Fixes

* **api:** include PostgreSQL GSSAPI runtime library ([9f372d4](https://github.com/kennfarbe/LearnPip/commit/9f372d40da22d4b82b05748ae100b0fa33251187))
* **ops:** find and bootstrap system tools ([c3ce10e](https://github.com/kennfarbe/LearnPip/commit/c3ce10eda8d8ec4e5ff55d1afc5226da0c87d07c))
* **worker:** include PostgreSQL GSSAPI runtime library ([1f7cb8c](https://github.com/kennfarbe/LearnPip/commit/1f7cb8c23a075118c076a500ef87430751fa15a1))

# [1.3.0](https://github.com/kennfarbe/LearnPip/compare/v1.2.1...v1.3.0) (2026-10-01)


### Features

* **ops:** configure rootless standard ports ([5b62737](https://github.com/kennfarbe/LearnPip/commit/5b6273783bbf3a8da2821563c286df1b38c6683f))
* **ops:** make rootless proxy ports configurable ([dac87f5](https://github.com/kennfarbe/LearnPip/commit/dac87f5278ee1b9111af1b96d5fda112fe8981dd))

## [1.2.1](https://github.com/kennfarbe/LearnPip/compare/v1.2.0...v1.2.1) (2026-10-01)


### Bug Fixes

* **ops:** bootstrap installer dependencies ([06d3749](https://github.com/kennfarbe/LearnPip/commit/06d37491c7206c07d348a05d6831f075b3dc3625))

# [1.2.0](https://github.com/kennfarbe/LearnPip/compare/v1.1.1...v1.2.0) (2026-10-01)


### Features

* **ops:** add internal deployment override ([68645a3](https://github.com/kennfarbe/LearnPip/commit/68645a3f2c960d3afa965438f634bf80f0393b6f))
* **ops:** add internal TLS configuration ([1fe1f28](https://github.com/kennfarbe/LearnPip/commit/1fe1f285254270dad845b0de075a3a73057415a9))
* **ops:** support internal LAN installs ([a88a037](https://github.com/kennfarbe/LearnPip/commit/a88a037a0477818f193721faa59ad39216f59194))

## [1.1.1](https://github.com/kennfarbe/LearnPip/compare/v1.1.0...v1.1.1) (2026-10-01)


### Bug Fixes

* **release:** trigger installer release after merge ([a5ec1f0](https://github.com/kennfarbe/LearnPip/commit/a5ec1f0fa366001494c1671a64f7f53dcb2fd185))

# [1.1.0](https://github.com/kennfarbe/LearnPip/compare/v1.0.0...v1.1.0) (2026-10-01)


### Bug Fixes

* **ci:** repair Docker publish workflow ([89995a0](https://github.com/kennfarbe/LearnPip/commit/89995a0bdeb4cf10b8e2ad0bc07c87d2d7c66cb4))


### Features

* **deploy:** configure application image version ([0b07949](https://github.com/kennfarbe/LearnPip/commit/0b07949aadbf5842d54f93a74ddcf049e9b84e5a))
* **deploy:** use published Docker Hub images ([b0b7aed](https://github.com/kennfarbe/LearnPip/commit/b0b7aed8c4b400a81225c3db577055d02ee19210))

# 1.0.0 (2026-10-01)


### Bug Fixes

* **ai:** compare URI schemes without constant pattern ([8d4101b](https://github.com/kennfarbe/LearnPip/commit/8d4101b1eff9d01c09ad5400957903b375b59f8a))
* **ai:** parse spaced arithmetic question suffixes ([a3cf2a5](https://github.com/kennfarbe/LearnPip/commit/a3cf2a55db4077082193078a69368fd01a208ef9))
* **ai:** show key settings only when mode is enabled ([4c41123](https://github.com/kennfarbe/LearnPip/commit/4c411235718778aff4735f4d35df11015bb6d338))
* align translation model snapshot and web build ([03a8a49](https://github.com/kennfarbe/LearnPip/commit/03a8a49bc05f3f96468ab919613d7b4ff2638b5c))
* **auth:** resolve cookie SameSite type ([cb9e2c3](https://github.com/kennfarbe/LearnPip/commit/cb9e2c31b5a6c6ee218ba0eed6d3f4438e1a574f))
* **data:** restore complete EF model snapshot ([1d03251](https://github.com/kennfarbe/LearnPip/commit/1d03251e2f925fed321e5b3f93e4eb4ffb5d2aae))
* **data:** update EF snapshot for community feedback ([a839c3b](https://github.com/kennfarbe/LearnPip/commit/a839c3bab773e5b8286742f5f5f21dd012466a6f))
* **exams:** disambiguate assertion and final stage count ([0cc9c9c](https://github.com/kennfarbe/LearnPip/commit/0cc9c9c893957d477000ba6fbd7e1f0c80d24964))
* **exams:** require measured simulation parts for forecast ([d8964a4](https://github.com/kennfarbe/LearnPip/commit/d8964a4b8760e0b6dd5d660ed0338f275442cd8f))
* **exams:** type nullable planning alternatives ([702ae61](https://github.com/kennfarbe/LearnPip/commit/702ae610f8440f3139ca0aa5cfdf429b4b453789))
* **exams:** use snapshot list count in simulation ([3dc557a](https://github.com/kennfarbe/LearnPip/commit/3dc557a8d256cc0a4e990fe040836760e27e6e07))
* **family:** reference account key in guardian migration ([694f534](https://github.com/kennfarbe/LearnPip/commit/694f534b1fb1a50e99c1ab29202121d8ce27f225))
* **media:** accept valid still images with minimal EXIF ([1cae208](https://github.com/kennfarbe/LearnPip/commit/1cae20850ce4cd7b2939f392b979adae4b9bf394))
* **moderation:** load locked version associations separately ([9532040](https://github.com/kennfarbe/LearnPip/commit/9532040893463f0a55cb50af969f6a8320ae3d9a))
* **ops:** wait for restored database creation before smoke test ([ff3499a](https://github.com/kennfarbe/LearnPip/commit/ff3499ac12dd8217f4999aac6b59a37249ae3efc))
* **test:** close authentication registration correctly ([f5d89f8](https://github.com/kennfarbe/LearnPip/commit/f5d89f889ebf32293b4aa462de1ef838d4148186))
* **test:** use distinct database scope name ([72df1e6](https://github.com/kennfarbe/LearnPip/commit/72df1e6ff96cfcd2a235f58060528b8741b55b2c))


### Features

* add bilingual UI and versioned question translations ([830d8fe](https://github.com/kennfarbe/LearnPip/commit/830d8fe875a1551c394f84da9b97f75a1af5fe0f))
* **ai:** add explicit provider modes and guarded request gateway ([99acbe4](https://github.com/kennfarbe/LearnPip/commit/99acbe4021625e0f4477a2241dd0ed0ef7d9a92d))
* **api:** add versioned read contract and authorization policies ([e1a5b73](https://github.com/kennfarbe/LearnPip/commit/e1a5b73b36ebdffd56a9e75b852ac88821ac0513))
* **auth:** add pseudonymous email and OIDC identity paths ([3350274](https://github.com/kennfarbe/LearnPip/commit/33502741c83734fd8ebf856638c35d1865800956))
* **auth:** add scoped roles and secure admin bootstrap ([09528aa](https://github.com/kennfarbe/LearnPip/commit/09528aa37b00f77f607f2c57f25fcdc536cc276b))
* **content:** moderate public question submissions ([618edcc](https://github.com/kennfarbe/LearnPip/commit/618edcccd24a47fbf6706141482c56e85b531da6))
* create private question drafts from reviewed photos ([9e003e7](https://github.com/kennfarbe/LearnPip/commit/9e003e783f4b5a5fd7c31c6ce5714db28d66e455))
* **data:** add PostgreSQL schema and migrations ([ecb5666](https://github.com/kennfarbe/LearnPip/commit/ecb56661f0830850ef4ccb457c2efd2ece153367))
* **exams:** add catalog editions and versioned simulations ([5a33176](https://github.com/kennfarbe/LearnPip/commit/5a3317653a0cdc5df22790ba1593daf3df601e10))
* **exams:** add staged power test and resumable simulation modes ([e46aceb](https://github.com/kennfarbe/LearnPip/commit/e46acebf4cb03e66aba825264de214440cfde1d9))
* **exams:** estimate goals from scope, time and spaced mastery ([3af9617](https://github.com/kennfarbe/LearnPip/commit/3af961715f65b2f6c80e3ea83d72c61705e32b2a))
* **exams:** forecast readiness with sourced schedules ([daca711](https://github.com/kennfarbe/LearnPip/commit/daca71182c23624fd3ef89238761cd6b32c0ae26))
* **family:** verify and revoke parent links with limited progress views ([1680a65](https://github.com/kennfarbe/LearnPip/commit/1680a6559c73016394844ae6e41dc934e5403347))
* **groups:** add expiring invitations and catalog sharing ([62261a0](https://github.com/kennfarbe/LearnPip/commit/62261a028a2d34ea5b0e657d7d6f8f366b8985c5))
* **identity:** manage inactive account lifecycle ([d74a6ae](https://github.com/kennfarbe/LearnPip/commit/d74a6aea66390c71798ca29c00a148b150722f3e))
* **learning:** add opt-in email reminders and quiet hours ([f2645a1](https://github.com/kennfarbe/LearnPip/commit/f2645a1dc3463490210e90ccf216e021c3c5775c))
* **learning:** add short randomized study sessions ([5422d47](https://github.com/kennfarbe/LearnPip/commit/5422d47c8ee8b55ffed85b0baa4f0dfa4190a71c))
* **learning:** schedule adaptive reviews by content ([84ea9d3](https://github.com/kennfarbe/LearnPip/commit/84ea9d328e00b311d38be35585cf0273ee9451f7))
* **learning:** show encouraging progress by topic ([144939f](https://github.com/kennfarbe/LearnPip/commit/144939f04d41b8476124090c08c9b3b93f466e10))
* **media:** store and serve sanitized private images ([a67e92d](https://github.com/kennfarbe/LearnPip/commit/a67e92d7c5641944c6779e8c0a1e59c82ad68bcd))
* **moderation:** add versioned feedback and review actions ([c5b3d19](https://github.com/kennfarbe/LearnPip/commit/c5b3d19072102af12f34b077ca8faba2346909a2))
* **ops:** verify production backups with isolated restore ([928c7f0](https://github.com/kennfarbe/LearnPip/commit/928c7f03e64e8e86c029ec360a0aa12c8622789d))
* **privacy:** export account data and support verified self-deletion ([3e7242f](https://github.com/kennfarbe/LearnPip/commit/3e7242fbfcfc70770dd2c3f78e8edf2e04f1a134))
* **privacy:** require explicit version visibility grants ([062cb50](https://github.com/kennfarbe/LearnPip/commit/062cb506594ffe9375569ab4afe75bb44193f9c6))
* **questions:** publish immutable rich versions and trace selections ([5cfa6f8](https://github.com/kennfarbe/LearnPip/commit/5cfa6f851ce2c9d738271a96263e6facfe1c7d85))
* **release:** publish install bundle and versioned images ([5e203e8](https://github.com/kennfarbe/LearnPip/commit/5e203e8c0b117e9b293bff00dd125e2411f7a755))
* verify photo solutions and guide learning variants ([e590e63](https://github.com/kennfarbe/LearnPip/commit/e590e63bd322932a99f691f83fd2a2dd91cda952))
* **web:** adapt landing page to phone widths ([286883b](https://github.com/kennfarbe/LearnPip/commit/286883bbd03f699268628541106517792430bbb5))
* **web:** add compact raster PWA icons ([e0c3ec9](https://github.com/kennfarbe/LearnPip/commit/e0c3ec95cce87e5f92fd60054810668582869200))
* **web:** add installable app metadata ([4e23409](https://github.com/kennfarbe/LearnPip/commit/4e23409accf88a23ba3516ccde1a8c57e9d8aee2))
* **web:** add LearnPip install manifest ([050507e](https://github.com/kennfarbe/LearnPip/commit/050507ef01fd5d4bb62f6298f8fd0129e62bd1ef))
* **web:** add persistent light dark and system appearance ([f692eec](https://github.com/kennfarbe/LearnPip/commit/f692eec015798f82d03c94dedbf825c48bb46a54))
* **web:** add private mobile question editor and catalogs ([2323f36](https://github.com/kennfarbe/LearnPip/commit/2323f36c228e7eba028fe23b844ebe54fe453542))
* **web:** cache only static PWA shell for offline use ([a768d85](https://github.com/kennfarbe/LearnPip/commit/a768d852bb5d6c9f696ac5071599b0c71305b484))
* **web:** establish responsive mobile defaults ([147b0c4](https://github.com/kennfarbe/LearnPip/commit/147b0c47c2f73ad305de5fea4ada8fd18d540c6a))
* **web:** register versioned production service worker ([18b428d](https://github.com/kennfarbe/LearnPip/commit/18b428d1bc08556a52a3a1578da458455083f2fa))

# Changelog

Dieser Verlauf wird bei Veröffentlichungen auf `main` automatisch durch semantic-release ergänzt.
