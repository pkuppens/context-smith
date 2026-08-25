import { Component, Input } from '@angular/core';
import { DocumentOutlineNode } from './models';

@Component({
  selector: 'app-document-outline',
  imports: [],
  template: `
    @if (node.title) {
      <li>
        {{ node.title }}
        @if (node.children.length) {
          <ul>
            @for (child of node.children; track child) {
              <app-document-outline [node]="child" />
            }
          </ul>
        }
      </li>
    } @else {
      @for (child of node.children; track child) {
        <app-document-outline [node]="child" />
      }
    }
  `,
})
export class DocumentOutline {
  @Input({ required: true }) node!: DocumentOutlineNode;
}
