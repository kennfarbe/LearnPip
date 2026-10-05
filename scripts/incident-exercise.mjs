#!/usr/bin/env node
// Synthetic tabletop exercise: no network, real identities, logs or notifications.
import assert from 'node:assert/strict';

const hours = (date, count) => new Date(date.getTime() + count * 60 * 60 * 1000);
function nextMonth(date) {
  const target = new Date(date);
  const day = target.getUTCDate();
  target.setUTCDate(1);
  target.setUTCMonth(target.getUTCMonth() + 1);
  const last = new Date(Date.UTC(target.getUTCFullYear(), target.getUTCMonth() + 1, 0)).getUTCDate();
  target.setUTCDate(Math.min(day, last));
  return target;
}

const awareness = new Date('2026-10-05T09:00:00Z');
const notification = new Date('2026-10-07T12:00:00Z');
const correctiveMeasure = new Date('2026-10-09T09:00:00Z');
const deadlines = {
  earlyWarning: hours(awareness, 24),
  fullNotification: hours(awareness, 72),
  gdprNotificationIfRequired: hours(awareness, 72),
  craVulnerabilityFinal: hours(correctiveMeasure, 14 * 24),
  craIncidentFinal: nextMonth(notification),
  nis2FinalOrProgress: nextMonth(notification),
};
assert.equal(deadlines.earlyWarning.toISOString(), '2026-10-06T09:00:00.000Z');
assert.equal(deadlines.fullNotification.toISOString(), '2026-10-08T09:00:00.000Z');
assert.equal(deadlines.craVulnerabilityFinal.toISOString(), '2026-10-23T09:00:00.000Z');
assert.equal(deadlines.nis2FinalOrProgress.toISOString(), '2026-11-07T12:00:00.000Z');
assert.equal(nextMonth(new Date('2027-01-31T12:00:00Z')).toISOString(), '2027-02-28T12:00:00.000Z');
assert.equal(nextMonth(new Date('2028-01-31T12:00:00Z')).toISOString(), '2028-02-29T12:00:00.000Z');

const scenarios = [
  { id: 'SYN-DEPENDENCY', activelyExploited: false, severeProductIncident: false, nis2Significant: false, personalDataRisk: false, routes: [] },
  { id: 'SYN-EXPLOIT', activelyExploited: true, severeProductIncident: false, nis2Significant: false, personalDataRisk: false, routes: ['CRA'] },
  { id: 'SYN-COMBINED', activelyExploited: false, severeProductIncident: true, nis2Significant: true, personalDataRisk: true, routes: ['CRA', 'NIS2', 'DSGVO'] },
];
for (const scenario of scenarios) {
  // These booleans are exercise assumptions; real applicability requires human triage.
  const routes = [
    ...(scenario.activelyExploited || scenario.severeProductIncident ? ['CRA'] : []),
    ...(scenario.nis2Significant ? ['NIS2'] : []),
    ...(scenario.personalDataRisk ? ['DSGVO'] : []),
  ];
  assert.deepEqual(routes, scenario.routes);
}
assert.equal(notification <= deadlines.fullNotification, true);
assert.equal(hours(awareness, 25) <= deadlines.earlyWarning, false);
console.log(JSON.stringify({
  synthetic: true,
  exerciseDate: '2026-10-05',
  externalNotifications: 0,
  scenarios: scenarios.map(({ id, routes }) => ({ id, routes })),
  deadlines,
  overdueWarningDetected: true,
  limitation: 'Technical tabletop exercise; real applicability, contacts and representation require operator verification.',
}, null, 2));
