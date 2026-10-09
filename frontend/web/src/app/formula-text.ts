import { Component, ElementRef, effect, inject, input } from '@angular/core';
import katex from 'katex';

/** Renders text literally and dollar-delimited formulas without trusting HTML or URLs. */
export function renderFormulaText(host: HTMLElement, text: string): void {
  host.replaceChildren();
  const formula = /\$\$([\s\S]+?)\$\$|\$([^$\n]+?)\$/g;
  let offset = 0;
  for (const match of text.matchAll(formula)) {
    host.append(document.createTextNode(text.slice(offset, match.index)));
    const span = document.createElement('span');
    const source = match[1] ?? match[2];
    // Some machine-readable sources escape TeX commands twice. Only presentation changes.
    const normalized = source.replace(/\\\\(?=[A-Za-z])/g, '\\');
    try {
      katex.render(normalized, span, {
        displayMode: match[1] !== undefined,
        throwOnError: true,
        trust: false,
        strict: 'error',
        maxExpand: 100,
        maxSize: 10,
        output: 'htmlAndMathml',
      });
      host.append(span);
    } catch {
      host.append(document.createTextNode(match[0]));
    }
    offset = match.index + match[0].length;
  }
  host.append(document.createTextNode(text.slice(offset)));
}

@Component({
  selector: 'app-formula-text',
  template: '',
  styles: ':host { overflow-wrap: anywhere; }',
})
export class FormulaText {
  readonly text = input<string | null>('');
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  constructor() {
    effect(() => renderFormulaText(this.host.nativeElement, this.text() ?? ''));
  }
}
