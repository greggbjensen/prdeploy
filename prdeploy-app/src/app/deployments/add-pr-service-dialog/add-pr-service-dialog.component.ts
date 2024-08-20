import { Component, ViewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import {
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from '@angular/material/dialog';
import {
  DxDropDownBoxModule,
  DxListComponent,
  DxListModule,
  DxSelectBoxComponent,
  DxSelectBoxModule
} from 'devextreme-angular';
import CustomStore from 'devextreme/data/custom_store';
import { firstValueFrom } from 'rxjs';
import {
  OpenPullRequestsGQL,
  PullRequest,
  PullRequestAddServicesGQL,
  RepositoryServicesGQL
} from 'src/app/shared/graphql';
import { NotificationManager, RepoManager } from 'src/app/shared/managers';
import { DialogResult } from 'src/app/shared/models';
import { LoggingService } from 'src/app/shared/services';

@Component({
  selector: 'app-add-pr-service-dialog',
  standalone: true,
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    DxSelectBoxModule,
    DxListModule,
    DxDropDownBoxModule,
    MatButtonModule
  ],
  templateUrl: './add-pr-service-dialog.component.html',
  styleUrl: './add-pr-service-dialog.component.scss'
})
export class AddPrServiceDialogComponent {
  @ViewChild('selectPullRequest') selectPullRequestComponent: DxSelectBoxComponent;
  @ViewChild(DxListComponent, { static: false }) listView: DxListComponent;

  selectedPullRequest: PullRequest;
  openPullRequests: CustomStore<PullRequest, number>;
  repositoryServices: string[];
  selectedServices: string[] = [];
  processing = false;

  constructor(
    private _openPullRequestsGQL: OpenPullRequestsGQL,
    private _pullRequestAddServicesGQL: PullRequestAddServicesGQL,
    private _repositoryServicesGQL: RepositoryServicesGQL,
    private _notificationManager: NotificationManager,
    private _repoManager: RepoManager,
    private _dialogRef: MatDialogRef<AddPrServiceDialogComponent>,
    private _loggingService: LoggingService
  ) {
    this._dialogRef
      .afterOpened()
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        this.clearFields();

        firstValueFrom(
          this._repositoryServicesGQL.fetch({ input: { owner: this._repoManager.owner, repo: this._repoManager.repo } })
        ).then(response => {
          this.repositoryServices = response.data.repositoryServices;
        });
      });

    this.openPullRequests = new CustomStore<PullRequest, number>({
      key: 'number',
      load: async options => {
        const result = await firstValueFrom(
          this._openPullRequestsGQL.fetch({
            input: {
              owner: this._repoManager.owner,
              repo: this._repoManager.repo,
              search: options.searchValue
            }
          })
        );
        return result.data.openPullRequests;
      }
    });
  }

  pullRequestDisplayExpr(item: PullRequest): string {
    return item ? `#${item.number}  ${item.title}  (${item.user?.name})` : '';
  }

  async addServicesToPr(): Promise<void> {
    this.processing = true;

    try {
      await firstValueFrom(
        this._pullRequestAddServicesGQL.mutate({
          input: {
            owner: this._repoManager.owner,
            repo: this._repoManager.repo,
            pullNumber: this.selectedPullRequest.number,
            services: this.selectedServices
          }
        })
      );

      this._notificationManager.show(`Add services comment added, it may take a minute to update.`);
      this._dialogRef.close(DialogResult.Save);
    } catch (error) {
      this._loggingService.error(error);
    }

    this.processing = false;
  }

  onDropDownBoxValueChanged(): void {
    this.updateListSelection();
  }

  onSelectedServicesChange(): void {
    this.selectedServices = this.listView.selectedItems;
  }

  updateListSelection() {
    if (!this.listView) {
      return;
    }

    if (!this.selectedServices || this.selectedServices.length === 0) {
      this.listView.selectedItems = [];
    }
  }

  cancel(): void {
    this._dialogRef.close(DialogResult.Cancel);
  }

  private clearFields() {
    this.selectedPullRequest = null;
    this.processing = false;
    this.selectedServices = [];
    this.updateListSelection();
    if (this.selectPullRequestComponent) {
      this.selectPullRequestComponent.value = null;
    }
  }
}
