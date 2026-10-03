import { computed, inject, Injectable, signal } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

interface Capabilities {
  administration: boolean;
  moderation: boolean;
}

@Injectable({ providedIn: 'root' })
export class ApplicationAccess {
  readonly capabilities = signal<Capabilities>({ administration: false, moderation: false });
  readonly canManage = computed(() => {
    const permissions = this.capabilities();
    return permissions.administration || permissions.moderation;
  });
  private pending: Promise<void> | null = null;

  refresh(): Promise<void> {
    if (!this.pending) this.pending = this.load().finally(() => (this.pending = null));
    return this.pending;
  }

  private async load(): Promise<void> {
    try {
      const response = await fetch('/api/v1/auth/capabilities', {
        credentials: 'same-origin',
        cache: 'no-store',
      });
      if (!response.ok) throw new Error('Permissions unavailable');
      const { data } = (await response.json()) as { data: Capabilities };
      this.capabilities.set({
        administration: data.administration === true,
        moderation: data.moderation === true,
      });
    } catch {
      this.capabilities.set({ administration: false, moderation: false });
    }
  }
}

export const administrationGuard: CanActivateFn = async () => {
  const access = inject(ApplicationAccess);
  const router = inject(Router);
  await access.refresh();
  return (
    access.canManage() || router.createUrlTree(['/overview'], { queryParams: { access: 'denied' } })
  );
};
