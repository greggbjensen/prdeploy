import { ChangeDetectorRef, Component, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { DeployEnvironmentDeployGQL, OpenPullRequestsGQL, PullRequest, Repository } from 'src/app/shared/graphql';
import {
  DialogButton,
  DialogService,
  LoggingService,
  NotificationService,
  StatusDialogType
} from 'src/app/shared/services';
import {
  DxButtonModule,
  DxCheckBoxModule,
  DxPopupModule,
  DxSelectBoxComponent,
  DxSelectBoxModule
} from 'devextreme-angular';
import CustomStore from 'devextreme/data/custom_store';

@Component({
  selector: 'app-deploy-force-dialog',
  templateUrl: './deploy-force-dialog.component.html',
  styleUrls: ['./deploy-force-dialog.component.scss'],
  standalone: true,
  imports: [DxPopupModule, DxSelectBoxModule, DxButtonModule, DxCheckBoxModule]
})
export class DeployForceDialogComponent {
  @Input() repository: Repository;
  @Input() environment: string;

  @ViewChild('selectPullRequest') selectPullRequestComponent: DxSelectBoxComponent;

  processing = false;
  retainLocks = false;
  private _visible = false;

  get visible() {
    return this._visible;
  }

  @Input()
  set visible(value: boolean) {
    this._visible = value;
    this.clearFields();
  }

  @Output() visibleChange = new EventEmitter<boolean>();

  selectedPullRequest: PullRequest;
  openPullRequests: CustomStore<PullRequest, number>;

  constructor(
    private _openPullRequestsGQL: OpenPullRequestsGQL,
    private _deployEnvironmentDeployGQL: DeployEnvironmentDeployGQL,
    private _notificationService: NotificationService,
    private _loggingService: LoggingService,
    private _changeDetectorRef: ChangeDetectorRef
  ) {
    this.openPullRequests = new CustomStore<PullRequest, number>({
      key: 'number',
      load: async options => {
        const result = await firstValueFrom(
          this._openPullRequestsGQL.fetch({
            owner: this.repository.owner,
            repo: this.repository.repo,
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

  async forceDeploy(): Promise<void> {
    this.processing = true;

    try {
      await firstValueFrom(
        this._deployEnvironmentDeployGQL.mutate({
          owner: this.repository.owner,
          repo: this.repository.repo,
          environment: this.environment,
          pullRequestNumber: this.selectedPullRequest.number,
          force: true,
          retain: this.retainLocks
        })
      );

      this._notificationService.show(`Force deploy ${this.environment} comment added, it may take a minute to update.`);
      this.visible = false;
    } catch (error) {
      this._loggingService.error(error);
    }

    this.processing = false;
  }

  cancel(): void {
    this.visible = false;
    this._changeDetectorRef.detectChanges();
  }

  onVisibleChange(): void {
    this.visibleChange.emit(this.visible);
  }

  private clearFields() {
    this.selectedPullRequest = null;
    this.processing = false;
    this.retainLocks = false;
    if (this.selectPullRequestComponent) {
      this.selectPullRequestComponent.value = null;
    }
    this._changeDetectorRef.detectChanges();
  }
}
