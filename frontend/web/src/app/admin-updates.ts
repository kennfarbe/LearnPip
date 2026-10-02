import { Component, inject, OnInit, signal } from '@angular/core';
import { LanguageService } from './language';

interface ReleaseInfo {
  version: string;
  name: string;
  notes: string;
  url: string;
  publishedAtUtc: string | null;
}
interface UpdateJob {
  id: string;
  fromVersion: string;
  targetVersion: string;
  state: string;
  phase: string;
  message: string | null;
}
interface UpdateStatus {
  installedVersion: string;
  latestVersion: string | null;
  state: string;
  interval: string;
  lastCheckedAtUtc: string | null;
  nextCheckAtUtc: string | null;
  error: string | null;
  release: ReleaseInfo | null;
  job: UpdateJob | null;
}

@Component({
  selector: 'app-admin-updates',
  template: `
    @if (status(); as s) {
      <section class="updates" aria-labelledby="updates-title">
        <div class="heading">
          <div><p class="eyebrow">System</p><h2 id="updates-title">{{ de ? 'LearnPip-Updates' : 'LearnPip updates' }}</h2></div>
          <span class="badge">{{ stateLabel(s.state) }}</span>
        </div>
        <div class="versions">
          <div><small>{{ de ? 'Installiert' : 'Installed' }}</small><strong>{{ s.installedVersion }}</strong></div>
          <div><small>{{ de ? 'Neuestes Stable' : 'Latest stable' }}</small><strong>{{ s.latestVersion ?? '–' }}</strong></div>
        </div>
        <p class="meta">
          {{ de ? 'Letzte Prüfung:' : 'Last check:' }} {{ date(s.lastCheckedAtUtc) }} ·
          {{ de ? 'Nächste Prüfung:' : 'Next check:' }} {{ date(s.nextCheckAtUtc) }}
        </p>
        <div class="controls">
          <label>{{ de ? 'Automatisch prüfen' : 'Check automatically' }}
            <select [value]="s.interval" (change)="setInterval($any($event.target).value)">
              <option value="daily">{{ de ? 'Täglich' : 'Daily' }}</option>
              <option value="weekly">{{ de ? 'Wöchentlich' : 'Weekly' }}</option>
              <option value="monthly">{{ de ? 'Monatlich' : 'Monthly' }}</option>
              <option value="never">{{ de ? 'Nie' : 'Never' }}</option>
            </select>
          </label>
          <button type="button" (click)="check()" [disabled]="busy()">{{ de ? 'Jetzt nach Updates suchen' : 'Check for updates now' }}</button>
        </div>
        @if (s.error) { <p class="error" role="alert">{{ s.error }}</p> }
        @if (s.release && s.state === 'update_available') {
          <div class="available">
            <h3>{{ de ? 'Update auf ' + s.release.version + ' verfügbar' : 'Update to ' + s.release.version + ' available' }}</h3>
            <p>{{ s.release.name }}</p>
            <div class="actions">
              <a [href]="s.release.url" target="_blank" rel="noopener">{{ de ? 'Release Notes anzeigen' : 'View release notes' }}</a>
              <button type="button" (click)="install(s)" [disabled]="busy()">{{ de ? 'Update auf ' + s.release.version + ' installieren' : 'Install ' + s.release.version }}</button>
            </div>
          </div>
        }
        @if (s.job && (s.job.state === 'queued' || s.job.state === 'running')) {
          <div class="progress" role="status"><strong>{{ de ? 'Update läuft:' : 'Update running:' }} {{ s.job.targetVersion }}</strong><p>{{ s.job.message ?? s.job.phase }}</p></div>
        }
        @if (s.job?.state === 'failed') { <p class="error" role="alert">{{ de ? 'Update fehlgeschlagen:' : 'Update failed:' }} {{ s.job?.message }}</p> }
        <p class="warning"><strong>{{ de ? 'Vor Updates:' : 'Before updating:' }}</strong>
          {{ de ? 'Externes VM-/Host-Backup prüfen. Datenbankmigrationen sind nicht automatisch rückgängig zu machen.' : 'Verify an external VM/host backup. Database migrations are not automatically reversible.' }}
        </p>
      </section>
    }
  `,
  styles: `
    .updates{margin-top:3rem;padding:clamp(1rem,3vw,2rem);border:1px solid var(--border);border-radius:1rem;background:var(--panel);color:var(--text)}
    .heading,.versions,.controls,.actions{display:flex;gap:1rem;align-items:center;justify-content:space-between;flex-wrap:wrap}
    h2{margin:.15rem 0 0;font-size:clamp(1.5rem,4vw,1.9rem)} .eyebrow{margin:0;color:var(--accent-text);font-size:.75rem;font-weight:700;text-transform:uppercase}
    .badge{padding:.35rem .65rem;border-radius:99px;background:var(--soft);font-weight:700}.versions{margin:1.25rem 0}.versions div{flex:1;min-width:10rem;padding:1rem;border:1px solid var(--border);border-radius:.75rem;background:var(--surface)}
    small,strong{display:block}.versions strong{font-size:1.35rem;margin-top:.25rem}.meta{color:var(--muted)}label{display:flex;gap:.5rem;align-items:center;flex-wrap:wrap}
    select,button,a{min-height:44px;padding:.65rem .8rem;border:1px solid var(--accent);border-radius:.55rem;font:inherit}button{background:var(--accent);color:var(--on-accent);font-weight:700;cursor:pointer}button:disabled{opacity:.55;cursor:wait}
    a{display:inline-flex;align-items:center;color:var(--accent-text);text-decoration:none;background:var(--surface)}.available,.progress,.warning,.error{margin-top:1rem;padding:1rem;border-radius:.75rem;background:var(--surface);border:1px solid var(--border)}
    .error{border-color:var(--danger);color:var(--danger)}.warning{border-color:var(--warning)}h3{margin-top:0}
    @media(max-width:42rem){.controls,.actions{align-items:stretch;flex-direction:column}.controls button,.actions button,.actions a{width:100%;justify-content:center;text-align:center}}
  `
})
export class AdminUpdates implements OnInit {
  readonly language = inject(LanguageService);
  readonly status = signal<UpdateStatus | null>(null);
  readonly busy = signal(false);

  get de() {
    return this.language.current() !== 'en';
  }

  ngOnInit() {
    void this.load();
  }

  async load() {
    try {
      const response = await fetch('/api/v1/admin/updates', { credentials: 'same-origin' });
      if (response.ok) this.status.set(await response.json());
      else this.status.set(null);
    } catch {
      this.status.set(null);
    }
  }

  async check() {
    this.busy.set(true);
    try {
      const response = await fetch('/api/v1/admin/updates/check', {
        method: 'POST',
        credentials: 'same-origin',
      });
      if (response.ok) this.status.set(await response.json());
    } finally {
      this.busy.set(false);
    }
  }

  async setInterval(interval: string) {
    const response = await fetch('/api/v1/admin/updates/interval', {
      method: 'PUT',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ interval }),
    });
    if (response.ok) await this.load();
  }

  async install(s: UpdateStatus) {
    if (!s.release) return;
    const text = this.de
      ? `Installiert: ${s.installedVersion}\nZiel: ${s.release.version}\n\nVorher externes Backup prüfen. Update jetzt starten?`
      : `Installed: ${s.installedVersion}\nTarget: ${s.release.version}\n\nVerify an external backup first. Start update now?`;
    if (!window.confirm(text)) return;
    this.busy.set(true);
    try {
      const response = await fetch('/api/v1/admin/updates/install', {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ version: s.release.version }),
      });
      if (response.ok) {
        await this.load();
        this.poll();
      }
    } finally {
      this.busy.set(false);
    }
  }

  poll() {
    const timer = window.setInterval(async () => {
      await this.load();
      const state = this.status()?.job?.state;
      if (state !== 'queued' && state !== 'running') window.clearInterval(timer);
    }, 3000);
  }

  date(value: string | null) {
    return value ? new Date(value).toLocaleString(this.de ? 'de-DE' : 'en-US') : '–';
  }

  stateLabel(s: string) {
    const de: { [key: string]: string } = {
      current: 'Aktuell',
      update_available: 'Update verfügbar',
      updating: 'Update läuft',
      check_failed: 'Prüfung fehlgeschlagen',
      unknown: 'Noch nicht geprüft',
    };
    const en: { [key: string]: string } = {
      current: 'Current',
      update_available: 'Update available',
      updating: 'Updating',
      check_failed: 'Check failed',
      unknown: 'Not checked',
    };
    return (this.de ? de : en)[s] ?? s;
  }
}
