import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class QuestionRights {
  readonly values = signal<Record<string, boolean>>({});

  allows(action: string): boolean {
    return this.values()[action] === true;
  }

  async refresh(): Promise<void> {
    try {
      const response = await fetch('/api/v1/questions/permissions', {
        credentials: 'same-origin',
        cache: 'no-store',
      });
      if (!response.ok) throw new Error();
      const body = (await response.json()) as { data: Record<string, boolean> };
      this.values.set(body.data);
    } catch {
      this.values.set({});
    }
  }
}
