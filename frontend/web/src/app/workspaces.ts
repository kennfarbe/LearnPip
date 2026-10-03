import { Component, inject, signal, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LanguageService } from './language';
import { ThemeService } from './theme';
import { ApplicationAccess } from './application-access';
import { LearningProgress } from './learning-progress';
import { LearningSession } from './learning-session';
import { QuestionEditor } from './question-editor';
import { PhotoDraft } from './photo-draft';
import { Translations } from './translations';
import { AiAssistant } from './ai-assistant';
import { ExamPlan } from './exam-plan';
import { ExamProfiles } from './exam-profiles';
import { PrivateMedia } from './private-media';
import { GroupSpace } from './group-space';
import { CommunityFeedback } from './community-feedback';
import { FamilySpace } from './family-space';
import { PasswordAccess } from './password-access';
import { AccountActivity } from './account-activity';
import { AdminUpdates } from './admin-updates';
import { ModerationQueue } from './moderation-queue';
import { CatalogLibrary } from './catalog-library';
import { ExamAdministration } from './exam-administration';

@Component({
  selector: 'app-overview-workspace',
  imports: [RouterLink, LearningProgress],
  template: `
    @if (denied()) {
      <p class="notice" role="alert">
        {{ language.t('Dieser Bereich ist für dein Konto nicht verfügbar.') }}
      </p>
    }
    <section class="action-grid" [attr.aria-label]="language.t('Dein nächster Schritt')">
      <article class="workspace-card primary-card">
        <p class="eyebrow">{{ language.t('Dein nächster Schritt') }}</p>
        <h2>{{ language.t('In kleinen Schritten üben') }}</h2>
        <p>{{ language.t('Kurze Lerneinheiten, die in den Alltag passen.') }}</p>
        <a class="primary-action" routerLink="/learn">{{ language.t('Jetzt lernen') }}</a>
      </article>
      <article class="workspace-card">
        <h2>{{ language.t('Fragen sammeln') }}</h2>
        <p>{{ language.t('Eigene Fragen, Antworten und Erklärungen an einem privaten Ort.') }}</p>
        <a class="secondary-action" routerLink="/questions" [queryParams]="{ create: 'true' }">{{
          language.t('Neue Frage erstellen')
        }}</a>
      </article>
      <article class="workspace-card">
        <h2>{{ language.t('Lernstoff organisieren') }}</h2>
        <p>{{ language.t('Kataloge und Lerninhalte an einem Ort.') }}</p>
        <a class="secondary-action" routerLink="/catalogs">{{ language.t('Kataloge öffnen') }}</a>
      </article>
    </section>
    <app-learning-progress [compact]="true" />
  `,
})
export class OverviewWorkspace {
  readonly language = inject(LanguageService);
  readonly denied = toSignal(
    inject(ActivatedRoute).queryParamMap.pipe(map((params) => params.get('access') === 'denied')),
    { initialValue: false },
  );
}

@Component({
  selector: 'app-questions-workspace',
  imports: [QuestionEditor, PhotoDraft, Translations, AiAssistant],
  template: `
    <app-question-editor />
    <details
      class="workspace-disclosure"
      (toggle)="photoOpened.set(photoOpened() || $any($event.target).open)"
    >
      <summary>{{ language.t('Frage aus einem Foto erstellen') }}</summary>
      @if (photoOpened()) {
        <app-photo-draft />
      }
    </details>
    <details
      class="workspace-disclosure"
      (toggle)="aiOpened.set(aiOpened() || $any($event.target).open)"
    >
      <summary>{{ language.t('KI-Unterstützung für Fragen') }}</summary>
      @if (aiOpened()) {
        <app-ai-assistant />
      }
    </details>
    <details
      class="workspace-disclosure"
      (toggle)="translationsOpened.set(translationsOpened() || $any($event.target).open)"
    >
      <summary>{{ language.t('Übersetzungen bearbeiten') }}</summary>
      @if (translationsOpened()) {
        <app-translations />
      }
    </details>
  `,
})
export class QuestionsWorkspace {
  readonly language = inject(LanguageService);
  readonly photoOpened = signal(false);
  readonly aiOpened = signal(false);
  readonly translationsOpened = signal(false);
  readonly editor = viewChild(QuestionEditor);

  canLeave(): boolean {
    return this.editor()?.canLeave() ?? true;
  }
}

@Component({
  selector: 'app-learning-workspace',
  imports: [LearningSession, ExamPlan, ExamProfiles],
  template: `
    <app-learning-session />
    @if (!practice()?.session() || practice()?.session()?.completed) {
      <details
        class="workspace-disclosure"
        (toggle)="planningOpened.set(planningOpened() || $any($event.target).open)"
      >
        <summary>{{ language.t('Eine Prüfung vorbereiten') }}</summary>
        @if (planningOpened()) {
          <app-exam-plan />
        }
      </details>
      <details
        class="workspace-disclosure"
        (toggle)="examsOpened.set(examsOpened() || $any($event.target).open)"
      >
        <summary>{{ language.t('Prüfungssimulation öffnen') }}</summary>
        @if (examsOpened()) {
          <app-exam-profiles />
        }
      </details>
    }
  `,
})
export class LearningWorkspace {
  readonly practice = viewChild(LearningSession);
  readonly language = inject(LanguageService);
  readonly planningOpened = signal(false);
  readonly examsOpened = signal(false);
}

@Component({
  selector: 'app-catalogs-workspace',
  imports: [CatalogLibrary, LearningSession, PrivateMedia, GroupSpace, CommunityFeedback],
  template: `
    <app-catalog-library />
    <details
      class="workspace-disclosure"
      (toggle)="contentsOpened.set(contentsOpened() || $any($event.target).open)"
    >
      <summary>{{ language.t('Lerninhalte und Varianten organisieren') }}</summary>
      @if (contentsOpened()) {
        <app-learning-session workspace="contents" />
      }
    </details>
    <details
      class="workspace-disclosure"
      (toggle)="mediaOpened.set(mediaOpened() || $any($event.target).open)"
    >
      <summary>{{ language.t('Private Medien') }}</summary>
      @if (mediaOpened()) {
        <app-private-media />
      }
    </details>
    <details
      class="workspace-disclosure"
      (toggle)="groupsOpened.set(groupsOpened() || $any($event.target).open)"
    >
      <summary>{{ language.t('Lerngruppen') }}</summary>
      @if (groupsOpened()) {
        <app-group-space />
      }
    </details>
    <details
      class="workspace-disclosure"
      (toggle)="feedbackOpened.set(feedbackOpened() || $any($event.target).open)"
    >
      <summary>{{ language.t('Rückmeldungen zu öffentlichen Fragen') }}</summary>
      @if (feedbackOpened()) {
        <app-community-feedback />
      }
    </details>
  `,
})
export class CatalogsWorkspace {
  readonly language = inject(LanguageService);
  readonly contentsOpened = signal(false);
  readonly mediaOpened = signal(false);
  readonly groupsOpened = signal(false);
  readonly feedbackOpened = signal(false);
}

@Component({
  selector: 'app-administration-workspace',
  imports: [AdminUpdates, ModerationQueue, ExamAdministration],
  template: `
    @if (access.capabilities().administration) {
      <app-admin-updates />
      <details class="workspace-disclosure">
        <summary>{{ language.t('Offizielle Kataloge und Prüfungsprofile') }}</summary>
        <app-exam-administration />
      </details>
    }
    @if (access.capabilities().moderation) {
      <app-moderation-queue />
    }
    <button type="button" class="secondary-action" (click)="access.refresh()">
      {{ language.t('Berechtigungen aktualisieren') }}
    </button>
    @if (!access.canManage()) {
      <p role="status">{{ language.t('Dieser Bereich ist für dein Konto nicht verfügbar.') }}</p>
    }
  `,
})
export class AdministrationWorkspace {
  readonly language = inject(LanguageService);
  readonly access = inject(ApplicationAccess);
}

@Component({
  selector: 'app-settings-workspace',
  imports: [AccountActivity, FamilySpace, PasswordAccess],
  template: `
    <section class="workspace-card" aria-labelledby="appearance-title">
      <h2 id="appearance-title">{{ language.t('Sprache und Design') }}</h2>
      <div class="settings-grid">
        <div>
          <label for="setting-language">{{ language.t('Sprache') }}</label>
          <select
            id="setting-language"
            [value]="language.current()"
            (change)="language.set($any($event.target).value)"
          >
            <option value="de">Deutsch</option>
            <option value="en">English</option>
          </select>
        </div>
        <div>
          <label for="setting-theme">{{ language.t('Design') }}</label>
          <select
            id="setting-theme"
            [value]="theme.preference()"
            (change)="theme.set($any($event.target).value)"
          >
            <option value="system">{{ language.t('System') }}</option>
            <option value="light">{{ language.t('Hell') }}</option>
            <option value="dark">{{ language.t('Dunkel') }}</option>
          </select>
        </div>
      </div>
    </section>
    <app-password-access />
    <app-account-activity />
    <details
      class="workspace-disclosure"
      (toggle)="familyOpened.set(familyOpened() || $any($event.target).open)"
    >
      <summary>{{ language.t('Familienverknüpfungen') }}</summary>
      @if (familyOpened()) {
        <app-family-space />
      }
    </details>
  `,
})
export class SettingsWorkspace {
  readonly language = inject(LanguageService);
  readonly theme = inject(ThemeService);
  readonly familyOpened = signal(false);
}
