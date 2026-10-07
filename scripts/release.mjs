import { pathToFileURL } from 'node:url';

export function releasePolicy(args, env) {
  if (args.some((arg) => !['--dry-run', '--check-config'].includes(arg))) {
    throw new Error('Supported options: --dry-run, --check-config');
  }
  const checkConfig = args.includes('--check-config');
  const dryRun = args.includes('--dry-run');
  if (!checkConfig && !dryRun &&
      (env.GITHUB_ACTIONS !== 'true' || env.GITHUB_EVENT_NAME !== 'push' ||
       env.GITHUB_REF !== 'refs/heads/main')) {
    throw new Error('Publishing requires a GitHub Actions push to main. Use --dry-run locally.');
  }
  return { checkConfig, dryRun };
}

export async function runRelease(args = process.argv.slice(2), env = process.env) {
  const { checkConfig, dryRun } = releasePolicy(args, env);
  const { default: release, getLogger, resolveConfig, resolveEnvCi } =
    await import('@semantic-release/core');
  const cwd = process.cwd();
  const stdout = process.stdout;
  const stderr = process.stderr;
  const envCi = resolveEnvCi({ cwd, env });
  const logger = getLogger({ stdout, stderr });
  const context = { cwd, env, envCi, logger, stdout, stderr };
  const { options, plugins } = await resolveConfig(context, { dryRun }, { buildPlugins: true });
  if (checkConfig) {
    logger.success('Release configuration and plugins loaded successfully.');
    return false;
  }
  if (envCi.isPr) {
    logger.log('Skipping release from a pull request.');
    return false;
  }
  return release({ context: { ...context, options }, plugins });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  // Core masks secrets in release errors. Do not print raw exceptions here.
  runRelease().catch(() => {
    process.stderr.write('Release failed. Review the masked release log.\n');
    process.exitCode = 1;
  });
}
