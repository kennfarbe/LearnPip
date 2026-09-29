import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PrivateMedia } from './private-media';
import { QuestionEditor } from './question-editor';
import { LearningSession } from './learning-session';
import { LearningProgress } from './learning-progress';
import { AccountActivity } from './account-activity';
import { GroupSpace } from './group-space';
import { ModerationQueue } from './moderation-queue';
import { CommunityFeedback } from './community-feedback';
import { ExamPlan } from './exam-plan';

@Component({
  imports: [
    RouterOutlet,
    PrivateMedia,
    QuestionEditor,
    LearningSession,
    LearningProgress,
    AccountActivity,
    GroupSpace,
    ModerationQueue,
    CommunityFeedback,
    ExamPlan,
  ],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = 'LearnPip';
}
