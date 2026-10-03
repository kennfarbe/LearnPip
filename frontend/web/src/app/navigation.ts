export const workspaces = [
  {
    path: '/overview',
    label: 'Übersicht',
    description: 'Dein Lernstand und dein nächster Schritt.',
  },
  {
    path: '/questions',
    label: 'Fragen',
    description: 'Eigene Fragen finden, erstellen und bearbeiten.',
  },
  { path: '/learn', label: 'Lernen', description: 'Eine kurze Lerneinheit nach der anderen.' },
  {
    path: '/catalogs',
    label: 'Kataloge und Inhalte',
    description: 'Deinen Lernstoff und deine Gruppen organisieren.',
  },
  {
    path: '/administration',
    label: 'Verwaltung',
    description: 'Einreichungen und die Anwendung verwalten.',
  },
  {
    path: '/settings',
    label: 'Einstellungen',
    description: 'Sprache, Design und dein persönliches Konto.',
  },
] as const;
