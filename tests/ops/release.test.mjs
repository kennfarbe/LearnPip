import assert from 'node:assert/strict';
import { readFile, mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { execFileSync } from 'node:child_process';
import { Writable } from 'node:stream';
import test from 'node:test';
import release, { getLogger } from '@semantic-release/core';
import { analyzeCommits } from '@semantic-release/commit-analyzer';
import { generateNotes } from '@semantic-release/release-notes-generator';
import { prepare as prepareChangelog } from '@semantic-release/changelog';
import { prepare as prepareGit } from '@semantic-release/git';
import { releasePolicy } from '../../scripts/release.mjs';

test('publishing requires a push to main; checks and explicit dry runs are permitted', () => {
  const env = { GITHUB_ACTIONS: 'true', GITHUB_EVENT_NAME: 'push', GITHUB_REF: 'refs/heads/main' };
  assert.equal(releasePolicy([], env).dryRun, false);
  for (const other of [{}, { ...env, GITHUB_EVENT_NAME: 'pull_request' },
    { ...env, GITHUB_REF: 'refs/heads/feature' }, { ...env, GITHUB_EVENT_NAME: 'workflow_dispatch' }]) {
    assert.throws(() => releasePolicy([], other));
    assert.equal(releasePolicy(['--dry-run'], other).dryRun, true);
    assert.equal(releasePolicy(['--check-config'], other).checkConfig, true);
  }
  assert.throws(() => releasePolicy(['--unknown'], env));
});

test('release dependency graph excludes the unused npm publishing chain', async () => {
  const lock = JSON.parse(await readFile(new URL('../../package-lock.json', import.meta.url)));
  for (const path of Object.keys(lock.packages)) {
    assert.ok(!/node_modules\/(semantic-release|@semantic-release\/npm|npm|postcss-selector-parser)$/.test(path), path);
  }
});

test('core verifies dry runs and publishes changelog, commit and tag to a local fixture', async () => {
  const dir = await mkdtemp(join(tmpdir(), 'learnpip-release-'));
  const remote = join(dir, 'remote.git');
  const cwd = join(dir, 'work');
  const git = (...args) => execFileSync('git', args, { cwd, stdio: 'pipe' }).toString().trim();
  try {
    execFileSync('git', ['init', '--bare', remote], { stdio: 'pipe' });
    execFileSync('git', ['clone', remote, cwd], { stdio: 'pipe' });
    git('checkout', '-b', 'main');
    git('config', 'user.name', 'Release test');
    git('config', 'user.email', 'release-test@example.invalid');
    await writeFile(join(cwd, 'README.md'), 'Fixture\n');
    git('add', '.');
    git('commit', '-m', 'feat: initial fixture');
    git('tag', 'v1.0.0');
    git('commit', '--allow-empty', '-m', 'fix: fixture correction');
    git('push', 'origin', 'main', '--tags');
    execFileSync('git', ['--git-dir', remote, 'symbolic-ref', 'HEAD', 'refs/heads/main']);
    const stream = new Writable({ write(chunk, encoding, callback) { callback(); } });
    let published = false;
    const input = {
      context: {
        cwd, env: { ...process.env, GH_TOKEN: '', GITHUB_TOKEN: '' },
        envCi: { isCi: false, isPr: false, branch: 'main' },
        stdout: stream, stderr: stream, logger: getLogger({ stdout: stream, stderr: stream }),
        options: { branches: ['main'], repositoryUrl: remote, tagFormat: 'v${version}', dryRun: true }
      },
      plugins: [{ analyzeCommits: (pluginOptions, context) => analyzeCommits({}, context),
        generateNotes: (pluginOptions, context) => generateNotes({}, { ...context,
          options: { ...context.options, repositoryUrl: 'https://github.com/example/fixture.git' } }),
        publish: () => { published = true; } }]
    };
    const result = await release(input);
    assert.equal(result.nextRelease.version, '1.0.1');
    assert.equal(result.nextRelease.gitTag, 'v1.0.1');
    assert.equal(published, false);
    assert.equal(git('tag'), 'v1.0.0');
    // Publish only to the disposable local bare repository; no GitHub API calls.
    input.context.options.dryRun = false;
    input.plugins.push({ prepare: (pluginOptions, context) =>
      prepareChangelog({ changelogFile: 'CHANGELOG.md' }, context) });
    input.plugins.push({ prepare: (pluginOptions, context) =>
      prepareGit({ assets: ['CHANGELOG.md'], message: 'chore(release): ${nextRelease.version} [skip ci]' }, context) });
    const publishedResult = await release(input);
    assert.equal(publishedResult.nextRelease.version, '1.0.1');
    assert.equal(published, true);
    assert.match(await readFile(join(cwd, 'CHANGELOG.md'), 'utf8'), /1\.0\.1/);
    assert.equal(git('log', '-1', '--format=%s'), 'chore(release): 1.0.1 [skip ci]');
    assert.equal(git('rev-parse', 'v1.0.1'), git('rev-parse', 'HEAD'));
    assert.match(git('ls-remote', '--tags', 'origin'), /refs\/tags\/v1\.0\.1/);
  } finally {
    await rm(dir, { recursive: true, force: true });
  }
});
