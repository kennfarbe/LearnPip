import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PrivateMedia } from './private-media';
import { QuestionEditor } from './question-editor';
import { LearningSession } from './learning-session';
import { LearningProgress } from './learning-progress';

@Component({
  imports: [RouterOutlet, PrivateMedia, QuestionEditor, LearningSession, LearningProgress],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = 'LearnPip';
}
