import { AfterViewInit, Component, DestroyRef, EventEmitter, Input, Output } from '@angular/core';
import { ItemReorderedEvent } from 'devextreme/ui/list';
import { debounceTime, firstValueFrom } from 'rxjs';
import {
  DeployQueue,
  DeployQueueUpdateGQL,
  InputMaybe,
  OpenPullRequestsGQL,
  PullRequest
} from 'src/app/shared/graphql';
import { MtxPopoverModule } from '@ng-matero/extensions/popover';
import { DatePipe } from '@angular/common';
import { RepoManager } from 'src/app/shared/managers';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MarkdownComponent } from 'ngx-markdown';
import { CleanMarkdownPipe } from 'src/app/shared/pipes';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import _ from 'lodash';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CdkDragDrop, CdkDropList, CdkDrag, moveItemInArray } from '@angular/cdk/drag-drop';

@Component({
  selector: 'app-queue-list',
  templateUrl: './queue-list.component.html',
  styleUrls: ['./queue-list.component.scss'],
  standalone: true,
  imports: [
    MatAutocompleteModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatListModule,
    MatIconModule,
    MtxPopoverModule,
    MarkdownComponent,
    ReactiveFormsModule,
    MatProgressSpinnerModule,
    CdkDropList,
    CdkDrag,
    DatePipe,
    CleanMarkdownPipe
  ]
})
export class QueueListComponent implements AfterViewInit {
  @Input() set queue(value: DeployQueue) {
    this._queue = value;
    if (value) {
      // Make not read only.
      this.pullData = [...value.pullRequests];
    } else {
      this.pullData = [];
    }
  }

  get queue(): DeployQueue | undefined {
    return this._queue;
  }

  pullData: PullRequest[];

  pullRequestControl = new FormControl('', [Validators.required]);

  private _queue?: DeployQueue;

  @Input() loading = true;
  @Output() queueUpdateStarted: EventEmitter<number[]> = new EventEmitter<number[]>();
  @Output() queueUpdateComplete: EventEmitter<number[]> = new EventEmitter<number[]>();
  openPullRequests: PullRequest[];
  pullRequestToAdd: PullRequest;

  constructor(
    private _openPullRequestsGQL: OpenPullRequestsGQL,
    private _deployQueueUpdateGQL: DeployQueueUpdateGQL,
    private _repoManager: RepoManager,
    private _destroyRef: DestroyRef
  ) {
    this.filterPullRequests();
  }

  clearPullRequest(event: MouseEvent) {
    event.stopPropagation();
    this.pullRequestToAdd = null;
    this.pullRequestControl.reset();
  }

  async drop(event: CdkDragDrop<string[]>) {
    moveItemInArray(this.pullData, event.previousIndex, event.currentIndex);
    const pullNumbers = this.pullData.map(i => i.number);
    await this.updatePullNumbers(pullNumbers);
  }

  ngAfterViewInit(): void {
    this.pullRequestControl.valueChanges
      .pipe(takeUntilDestroyed(this._destroyRef), debounceTime(300))
      .subscribe(value => {
        if (!_.isObject(value)) {
          this.filterPullRequests(value);
        }
      });
  }

  async filterPullRequests(search: string = '') {
    const result = await firstValueFrom(
      this._openPullRequestsGQL.fetch({
        input: {
          owner: this._repoManager.owner,
          repo: this._repoManager.repo,
          search
        }
      })
    );

    this.openPullRequests = result.data.openPullRequests;
  }

  selectPullRequest(event: MatAutocompleteSelectedEvent) {
    this.pullRequestToAdd = event.option.value;
  }

  formatPullRequest(item: PullRequest) {
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
    this.pullRequestControl.reset();
    this.pullRequestToAdd = null;
  }

  private async updatePullNumbers(
    pullNumbers: (InputMaybe<number | number[]> | undefined)[] | undefined
  ): Promise<void> {
    const pullRequestNumbers = pullNumbers as number[];
    this.queueUpdateStarted.emit(pullRequestNumbers);
    await firstValueFrom(
      this._deployQueueUpdateGQL.mutate({
        input: {
          owner: this._repoManager.owner,
          repo: this._repoManager.repo,
          environment: this.queue?.environment as string,
          pullNumbers: pullRequestNumbers
        }
      })
    );

    this.queueUpdateComplete.emit(pullRequestNumbers);
  }
}
