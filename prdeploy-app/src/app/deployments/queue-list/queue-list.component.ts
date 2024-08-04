import { Component, EventEmitter, Input, Output } from '@angular/core';
import CustomStore from 'devextreme/data/custom_store';
import { ItemReorderedEvent } from 'devextreme/ui/list';
import { firstValueFrom } from 'rxjs';
import {
  DeployQueue,
  DeployQueueUpdateGQL,
  InputMaybe,
  OpenPullRequestsGQL,
  PullRequest
} from 'src/app/shared/graphql';
import { DxSelectBoxModule, DxLoadPanelModule } from 'devextreme-angular';
import { PullRequestPopoverComponent } from '../pull-request-popover/pull-request-popover.component';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DxTemplateModule } from 'devextreme-angular/core';
import { DxoItemDraggingModule, DxoLoadPanelModule } from 'devextreme-angular/ui/nested';
import { DxListModule } from 'devextreme-angular/ui/list';
import { DatePipe } from '@angular/common';
import { RepoManager } from 'src/app/shared/managers';

@Component({
  selector: 'app-queue-list',
  templateUrl: './queue-list.component.html',
  styleUrls: ['./queue-list.component.scss'],
  standalone: true,
  imports: [
    DxListModule,
    DxoItemDraggingModule,
    DxoLoadPanelModule,
    DxTemplateModule,
    DxButtonModule,
    PullRequestPopoverComponent,
    DxSelectBoxModule,
    DxLoadPanelModule,
    DatePipe
  ]
})
export class QueueListComponent {
  @Input() set queue(value: DeployQueue) {
    this._queue = value;
    if (value) {
      this.pullData = value.pullRequests;
    } else {
      this.pullData = [];
    }
  }

  get queue(): DeployQueue | undefined {
    return this._queue;
  }

  pullData: any;

  private _queue?: DeployQueue;

  @Input() loading = true;
  @Output() queueUpdateStarted: EventEmitter<string[]> = new EventEmitter<string[]>();
  @Output() queueUpdateComplete: EventEmitter<string[]> = new EventEmitter<string[]>();
  openPullRequests: CustomStore<PullRequest, number>;

  constructor(
    private _openPullRequestsGQL: OpenPullRequestsGQL,
    private _deployQueueUpdateGQL: DeployQueueUpdateGQL,
    private _repoManager: RepoManager
  ) {
    this.openPullRequests = new CustomStore<PullRequest, number>({
      key: 'number',
      load: async options => {
        const result = await firstValueFrom(
          this._openPullRequestsGQL.fetch({
            owner: this._repoManager.owner,
            repo: this._repoManager.repo,
            search: options.searchValue
          })
        );
        return result.data.openPullRequests;
      }
    });
  }

  pullRequestDisplayExpr(item: PullRequest) {
    return item ? `#${item.number}  ${item.title}  (${item.user?.name})` : '';
  }

  async onItemReordered(e: ItemReorderedEvent) {
    // The syntax above retains this scope.
    const items = e.component.getDataSource().items() as PullRequest[];
    const swapItem = items[e.toIndex];
    items[e.toIndex] = items[e.fromIndex];
    items[e.fromIndex] = swapItem;
    const pullNumbers = items.map(i => i.number);
    await this.updatePullNumbers(pullNumbers);
  }

  dataChange() {
    console.log('data change');
  }

  async remove(pullRequest: PullRequest) {
    const pullNumbers = this.queue?.pullRequests.filter(p => p.number !== pullRequest.number).map(p => p.number);
    await this.updatePullNumbers(pullNumbers);
  }

  async add(pullRequest: PullRequest) {
    const pullNumbers = this.queue?.pullRequests.map(p => p.number);
    pullNumbers?.push(pullRequest.number);
    await this.updatePullNumbers(pullNumbers);
  }

  private async updatePullNumbers(
    pullNumbers: (InputMaybe<string | string[]> | undefined)[] | undefined
  ): Promise<void> {
    const pullRequestNumbers = pullNumbers as string[];
    this.queueUpdateStarted.emit(pullRequestNumbers);
    await firstValueFrom(
      this._deployQueueUpdateGQL.mutate({
        owner: this._repoManager.owner,
        repo: this._repoManager.repo,
        environment: this.queue?.environment as string,
        pullRequestNumbers
      })
    );

    this.queueUpdateComplete.emit(pullRequestNumbers);
  }
}
