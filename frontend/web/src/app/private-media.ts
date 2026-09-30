import { Component, inject, ElementRef, ViewChild, signal } from '@angular/core';
import { LanguageService } from './language';

interface MediaResponse {
  data: { id: string; altText: string };
}

@Component({
  selector: 'app-private-media',
  template: `
    <section class="media-panel" aria-labelledby="media-title">
      <h2 id="media-title">{{ language.t('Private Bilder') }}</h2>
      <p>
        {{
          language.t(
            'Fotos und Zeichnungen bleiben in deinem Konto. Melde dich an, bevor du ein Bild hochlädst.'
          )
        }}
      </p>
      <form (submit)="upload($event)">
        <label for="media-file">{{ language.t('Bild (JPEG oder PNG, maximal 5 MiB)') }}</label>
        <input
          id="media-file"
          type="file"
          accept="image/jpeg,image/png"
          (change)="selectFile($event)"
          required
        />
        <label for="media-description">{{ language.t('Bildbeschreibung') }}</label>
        <input
          id="media-description"
          type="text"
          maxlength="300"
          required
          [value]="description()"
          (input)="description.set(asInput($event).value)"
        />
        <button type="submit" [disabled]="uploading()">
          {{ uploading() ? 'Wird hochgeladen …' : 'Privat hochladen' }}
        </button>
      </form>
      @if (message()) {
        <p role="status">{{ message() }}</p>
      }
      @if (imageUrl()) {
        <figure>
          <button
            type="button"
            class="image-button"
            (click)="openViewer()"
            aria-label="Bild vergrößern"
          >
            <img [src]="imageUrl()" [alt]="imageAlt()" />
          </button>
          <figcaption>{{ imageAlt() }}</figcaption>
        </figure>
        <button type="button" (click)="remove()">{{ language.t('Bild löschen') }}</button>
      }
      <dialog #viewer aria-label="Bild vergrößert anzeigen" (click)="closeViewer()">
        @if (imageUrl()) {
          <img [src]="imageUrl()" [alt]="imageAlt()" />
        }
        <button type="button" (click)="closeViewer()">{{ language.t('Schließen') }}</button>
      </dialog>
    </section>
  `,
  styles: `
    .media-panel {
      margin-top: 2.5rem;
      padding: 1.5rem;
      border: 1px solid var(--border);
      border-radius: 0.9rem;
      background: var(--surface);
    }
    form {
      display: grid;
      gap: 0.7rem;
      max-width: 32rem;
    }
    input {
      padding: 0.65rem;
    }
    button {
      width: fit-content;
      padding: 0.65rem 1rem;
      cursor: pointer;
    }
    .image-button {
      display: block;
      padding: 0;
      border: 0;
      background: none;
    }
    figure {
      margin: 1.5rem 0;
    }
    figure img {
      max-width: min(100%, 25rem);
      max-height: 16rem;
      object-fit: contain;
    }
    dialog {
      max-width: 95vw;
      max-height: 95vh;
      border: 0;
      border-radius: 0.75rem;
    }
    dialog img {
      display: block;
      max-width: 85vw;
      max-height: 75vh;
      object-fit: contain;
    }
  `,
})
export class PrivateMedia {
  readonly language = inject(LanguageService);
  @ViewChild('viewer') private viewer?: ElementRef<HTMLDialogElement>;
  readonly description = signal('');
  readonly uploading = signal(false);
  readonly message = signal('');
  readonly imageUrl = signal('');
  readonly imageAlt = signal('');
  private file: File | null = null;
  private id = '';

  asInput(event: Event): HTMLInputElement {
    return event.target as HTMLInputElement;
  }

  selectFile(event: Event): void {
    this.file = this.asInput(event).files?.item(0) ?? null;
  }

  async upload(event: Event): Promise<void> {
    event.preventDefault();
    if (!this.file || !this.description().trim() || this.uploading()) return;
    this.uploading.set(true);
    this.message.set('');
    try {
      const form = new FormData();
      form.append('file', this.file);
      form.append('altText', this.description().trim());
      const response = await fetch('/api/v1/media/', {
        method: 'POST',
        body: form,
        credentials: 'same-origin',
      });
      if (!response.ok) {
        this.message.set(
          response.status === 401
            ? 'Bitte zuerst anmelden.'
            : 'Das Bild konnte nicht hochgeladen werden.',
        );
        return;
      }
      const media = (await response.json()) as MediaResponse;
      this.id = media.data.id;
      this.imageAlt.set(media.data.altText);
      this.imageUrl.set(`/api/v1/media/${this.id}/content`);
      this.message.set('Bild privat gespeichert.');
    } catch {
      this.message.set('Die Verbindung ist fehlgeschlagen.');
    } finally {
      this.uploading.set(false);
    }
  }

  openViewer(): void {
    this.viewer?.nativeElement.showModal();
  }

  closeViewer(): void {
    this.viewer?.nativeElement.close();
  }

  async remove(): Promise<void> {
    if (!this.id) return;
    const response = await fetch(`/api/v1/media/${this.id}`, {
      method: 'DELETE',
      credentials: 'same-origin',
    });
    if (!response.ok) {
      this.message.set('Das Bild konnte nicht gelöscht werden.');
      return;
    }
    this.closeViewer();
    this.id = '';
    this.imageUrl.set('');
    this.message.set('Bild gelöscht.');
  }
}
