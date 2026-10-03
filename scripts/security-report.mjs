#!/usr/bin/env node
// Evaluate scanner JSON without exposing private vulnerability identifiers.
import { readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

export class InvalidScan extends Error {}

function check(condition, message) {
  if (!condition) throw new InvalidScan(message);
}

function object(value) {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function count(value) {
  check(Number.isSafeInteger(value) && value >= 0, 'Invalid vulnerability count');
  return value;
}

export function evaluate(kind, document) {
  check(object(document), 'Expected JSON object');
  if (kind === 'npm') {
    check(Number.isSafeInteger(document.auditReportVersion), 'Not an npm audit report');
    check(!document.error, 'npm scanner returned an error');
    check(object(document.metadata), 'npm audit metadata missing');
    const severities = document.metadata.vulnerabilities;
    check(object(severities), 'npm vulnerability summary missing');
    return [count(severities.high === undefined ? 0 : severities.high),
      count(severities.critical === undefined ? 0 : severities.critical)];
  }

  let high = 0;
  let critical = 0;
  function finding(value, key, allowed) {
    check(object(value), 'Invalid vulnerability record');
    const severity = String(value[key] ?? '').toLowerCase();
    check(allowed.includes(severity), 'Unknown vulnerability severity');
    if (severity === 'high') high += 1;
    if (severity === 'critical') critical += 1;
  }

  if (kind === 'nuget') {
    check(Array.isArray(document.projects) && document.projects.length > 0,
      'NuGet projects missing');
    for (const project of document.projects) {
      check(object(project), 'Invalid NuGet project');
      const frameworks = project.frameworks === undefined ? [] : project.frameworks;
      check(Array.isArray(frameworks), 'Invalid NuGet framework list');
      for (const framework of frameworks) {
        check(object(framework), 'Invalid NuGet framework');
        for (const section of ['topLevelPackages', 'transitivePackages']) {
          const packages = framework[section] === undefined ? [] : framework[section];
          check(Array.isArray(packages), 'Invalid package list');
          for (const pkg of packages) {
            check(object(pkg), 'Invalid package');
            const vulnerabilities = pkg.vulnerabilities === undefined ? [] : pkg.vulnerabilities;
            check(Array.isArray(vulnerabilities), 'Invalid vulnerability list');
            for (const value of vulnerabilities) {
              finding(value, 'severity', ['low', 'moderate', 'medium', 'high', 'critical']);
            }
          }
        }
      }
    }
    // Projects without vulnerable packages can legitimately omit frameworks.
    return [high, critical];
  }

  if (kind === 'trivy') {
    check(Number.isSafeInteger(document.SchemaVersion), 'Invalid Trivy scan report');
    check(Array.isArray(document.Results), 'Trivy result list missing');
    for (const result of document.Results) {
      check(object(result), 'Invalid Trivy result');
      const vulnerabilities = result.Vulnerabilities ?? [];
      check(Array.isArray(vulnerabilities), 'Invalid Trivy vulnerability list');
      for (const value of vulnerabilities) {
        finding(value, 'Severity', ['unknown', 'low', 'medium', 'high', 'critical']);
      }
    }
    return [high, critical];
  }
  throw new InvalidScan('Unknown scanner type');
}

export function main(args = process.argv.slice(2)) {
  let high;
  let critical;
  try {
    check(args.length === 2 && ['npm', 'nuget', 'trivy'].includes(args[0]),
      'Invalid arguments');
    [high, critical] = evaluate(args[0], JSON.parse(readFileSync(args[1], 'utf8')));
  } catch {
    console.error('Security scan incomplete or invalid; investigation required.');
    return 2;
  }
  if (high || critical) {
    console.error('Security scan detected high/critical findings. Investigate privately.');
    return 1;
  }
  console.log('Security scan completed with no high/critical findings.');
  return 0;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  process.exitCode = main();
}
