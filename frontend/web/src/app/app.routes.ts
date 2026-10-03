import { Routes } from '@angular/router';
import { administrationGuard } from './application-access';
import type { QuestionsWorkspace } from './workspaces';

export const routes: Routes = [
  { path: '', redirectTo: 'overview', pathMatch: 'full' },
  {
    path: 'overview',
    loadComponent: () => import('./workspaces').then((module) => module.OverviewWorkspace),
  },
  {
    path: 'questions',
    loadComponent: () => import('./workspaces').then((module) => module.QuestionsWorkspace),
    canDeactivate: [(component: QuestionsWorkspace) => component.canLeave()],
  },
  {
    path: 'learn',
    loadComponent: () => import('./workspaces').then((module) => module.LearningWorkspace),
  },
  {
    path: 'catalogs',
    loadComponent: () => import('./workspaces').then((module) => module.CatalogsWorkspace),
  },
  {
    path: 'administration',
    loadComponent: () => import('./workspaces').then((module) => module.AdministrationWorkspace),
    canActivate: [administrationGuard],
  },
  {
    path: 'settings',
    loadComponent: () => import('./workspaces').then((module) => module.SettingsWorkspace),
  },
  { path: '**', redirectTo: 'overview' },
];
