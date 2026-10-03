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
