import { Component, Input, ViewChild } from '@angular/core';
import { DxPopoverComponent, DxPopoverModule } from 'devextreme-angular';
import { PullRequest } from 'src/app/shared/graphql';
import { CleanMarkdownPipe } from '../../../shared/pipes/clean-markdown.pipe';
import { MarkdownComponent } from 'ngx-markdown';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-pull-request-popover',
  templateUrl: './pull-request-popover.component.html',
  styleUrls: ['./pull-request-popover.component.scss'],
  standalone: true,
  imports: [DxPopoverModule, MarkdownComponent, DatePipe, CleanMarkdownPipe]
})
export class PullRequestPopoverComponent {
  @Input() pullRequest?: PullRequest;
  @Input() target?: string;
  @ViewChild('detailsPopover') detailsPopover: DxPopoverComponent;
  private popoverElement: Element;
  private mouseoutHandler: any;

  constructor() {
    this.mouseoutHandler = this.mouseout.bind(this);
  }

  onHiding(e: any): void {
    // Allow user to click on popover details.
    // SourceRef: https://supportcenter.devexpress.com/ticket/details/t1114215/popover-hides-after-mouse-move
    if (this.popoverElement) {
      this.popoverElement.removeEventListener('mouseleave', this.mouseoutHandler);
    }

    this.popoverElement = e.component.content();
    this.mouseoutHandler = this.popoverElement.addEventListener('mouseleave', this.mouseoutHandler);
    const isMouseInsidePopover = this.popoverElement.matches(':hover');
    e.cancel = isMouseInsidePopover;
  }

  mouseout(e: MouseEvent): void {
    // Discard event bubbling from children.
    if (e.currentTarget === e.target) {
      this.detailsPopover.instance.hide();
    }
  }
}
