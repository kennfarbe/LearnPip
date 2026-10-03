import assert from 'node:assert/strict';
import { test } from 'node:test';
import { mkdtempSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { evaluate, InvalidScan } from '../../scripts/security-report.mjs';

const npm = (high = 0, critical = 0) => ({
  auditReportVersion: 2, metadata: { vulnerabilities: { high, critical } },
});
const nuget = (severity, section = 'transitivePackages') => ({ projects: [{ frameworks: [{
  [section]: [{ vulnerabilities: [{ severity }] }],
}] }] });
const trivy = (Severity) => ({ SchemaVersion: 2, Results: [{
  Vulnerabilities: [{ Severity }],
}] });

for (const [name, kind, report, expected] of [
  ['clean npm', 'npm', npm(), [0, 0]],
  ['npm findings', 'npm', npm(2, 1), [2, 1]],
  ['clean NuGet', 'nuget', { projects: [{ frameworks: [] }] }, [0, 0]],
  ['NuGet omitted frameworks', 'nuget', { projects: [{}] }, [0, 0]],
  ['NuGet direct high', 'nuget', nuget('High', 'topLevelPackages'), [1, 0]],
  ['NuGet transitive critical', 'nuget', nuget('Critical'), [0, 1]],
  ['NuGet moderate', 'nuget', nuget('Moderate'), [0, 0]],
  ['clean Trivy', 'trivy', { SchemaVersion: 2, Results: [{}] }, [0, 0]],
  ['Trivy high', 'trivy', trivy('HIGH'), [1, 0]],
  ['Trivy critical', 'trivy', trivy('CRITICAL'), [0, 1]],
  ['Trivy unknown', 'trivy', trivy('UNKNOWN'), [0, 0]],
]) {
  test(name, () => assert.deepEqual(evaluate(kind, report), expected));
}

for (const [name, kind, report] of [
  ['null root', 'npm', null],
  ['array root', 'npm', []],
  ['npm error', 'npm', { ...npm(), error: { code: 'ENOAUDIT' } }],
  ['npm missing metadata', 'npm', { auditReportVersion: 2 }],
  ['npm missing summary', 'npm', { auditReportVersion: 2, metadata: {} }],
  ['npm invalid version', 'npm', { ...npm(), auditReportVersion: true }],
  ['npm null count', 'npm', npm(null)],
  ['npm negative count', 'npm', npm(-1)],
  ['npm string count', 'npm', npm('2')],
  ['npm fractional count', 'npm', npm(0.5)],
  ['npm unsafe count', 'npm', npm(Number.MAX_SAFE_INTEGER + 1)],
  ['missing NuGet projects', 'nuget', {}],
  ['empty NuGet projects', 'nuget', { projects: [] }],
  ['invalid NuGet framework', 'nuget', { projects: [{ frameworks: [null] }] }],
  ['invalid NuGet packages', 'nuget', { projects: [{ frameworks: [{ topLevelPackages: {} }] }] }],
  ['unknown NuGet severity', 'nuget', nuget('unexpected')],
  ['missing Trivy results', 'trivy', { SchemaVersion: 2 }],
  ['invalid Trivy findings', 'trivy', { SchemaVersion: 2, Results: [{ Vulnerabilities: {} }] }],
  ['unknown Trivy severity', 'trivy', trivy('unexpected')],
  ['unknown scanner', 'other', {}],
]) {
  test(name, () => assert.throws(() => evaluate(kind, report), InvalidScan));
}

test('CLI exit codes and aggregate-only output', () => {
  const directory = mkdtempSync(join(tmpdir(), 'learnpip-report-test-'));
  const script = fileURLToPath(new URL('../../scripts/security-report.mjs', import.meta.url));
  try {
    const path = join(directory, 'report.json');
    for (const [content, status] of [
      [JSON.stringify(npm()), 0],
      [JSON.stringify({ ...trivy('HIGH'), secret: 'PRIVATE-FINDING-123' }), 1],
      ['{"secret":"PRIVATE-FINDING-123",', 2],
    ]) {
      writeFileSync(path, content);
      const result = spawnSync(process.execPath, [script, status === 0 ? 'npm' : 'trivy', path], { encoding: 'utf8' });
      assert.equal(result.status, status);
      assert.doesNotMatch(result.stdout + result.stderr, /PRIVATE-FINDING-123/);
    }
    assert.equal(spawnSync(process.execPath, [script, 'npm', join(directory, 'missing')]).status, 2);
    assert.equal(spawnSync(process.execPath, [script]).status, 2);
  } finally {
    rmSync(directory, { recursive: true, force: true });
  }
});
