import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PrivateMedia } from './private-media';

@Component({
  imports: [RouterOutlet, PrivateMedia],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = 'LearnPip';
}
