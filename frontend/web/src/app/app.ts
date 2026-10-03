import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PrivateMedia } from './private-media';
import { QuestionEditor } from './question-editor';
import { LearningSession } from './learning-session';
import { LearningProgress } from './learning-progress';
import { AccountActivity } from './account-activity';
import { AdminUpdates } from './admin-updates';
import { GroupSpace } from './group-space';
import { ModerationQueue } from './moderation-queue';
import { CommunityFeedback } from './community-feedback';
import { ExamPlan } from './exam-plan';
import { ExamProfiles } from './exam-profiles';
import { AiAssistant } from './ai-assistant';
import { PhotoDraft } from './photo-draft';
import { Translations } from './translations';
import { FamilySpace } from './family-space';
import { ThemeService } from './theme';
import { LanguageService } from './language';

@Component({
  imports: [
    RouterOutlet,
    PrivateMedia,
    QuestionEditor,
    LearningSession,
    LearningProgress,
    AccountActivity,
    AdminUpdates,
    GroupSpace,
    ModerationQueue,
    CommunityFeedback,
    ExamPlan,
    ExamProfiles,
    AiAssistant,
    PhotoDraft,
    Translations,
    FamilySpace,
  ],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly theme = inject(ThemeService);
  protected readonly title = 'LearnPip';
  protected readonly language = inject(LanguageService);
  constructor() {
    this.language.set(this.language.current());
  }
}
