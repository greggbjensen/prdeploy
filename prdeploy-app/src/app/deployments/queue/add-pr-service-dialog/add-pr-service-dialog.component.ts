import { ChangeDetectorRef, Component, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import {
  DxButtonModule,
  DxDropDownBoxModule,
  DxListComponent,
  DxListModule,
  DxPopupModule,
  DxSelectBoxComponent,
  DxSelectBoxModule
} from 'devextreme-angular';
import { firstValueFrom } from 'rxjs';
// import {
//   OpenPullRequestsGQL,
//   PullRequest,
//   PullRequestAddServicesGQL,
//   Repository,
//   RepositoryServicesGQL
// } from 'src/app/shared/graphql';
import { DialogButton, DialogService, LoggingService, StatusDialogType } from 'src/app/shared/services';

@Component({
  selector: 'app-add-pr-service-dialog',
  standalone: true,
  imports: [DxPopupModule, DxSelectBoxModule, DxListModule, DxDropDownBoxModule, DxButtonModule],
  templateUrl: './add-pr-service-dialog.component.html',
  styleUrl: './add-pr-service-dialog.component.scss'
})
export class AddPrServiceDialogComponent {
  // @Input() repository: Repository;
  @ViewChild('selectPullRequest') selectPullRequestComponent: DxSelectBoxComponent;
  @ViewChild(DxListComponent, { static: false }) listView: DxListComponent;

  private _visible = false;

  get visible() {
    return this._visible;
  }

  // selectedPullRequest: PullRequest;
  // openPullRequests: CustomStore<PullRequest, number>;
  repositoryServices: string[];
  selectedServices: string[] = [];
  processing = false;

  @Input()
  set visible(value: boolean) {
    this._visible = value;
    this.clearFields();

    if (this.visible) {
      // firstValueFrom(
      //   this._repositoryServicesGQL.fetch({ owner: this.repository.owner, repo: this.repository.repo })
      // ).then(response => {
      //   this.repositoryServices = response.data.repositoryServices;
      // });
    }
  }

  @Output() visibleChange = new EventEmitter<boolean>();

  constructor(
    // private _openPullRequestsGQL: OpenPullRequestsGQL,
    // private _pullRequestAddServicesGQL: PullRequestAddServicesGQL,
    // private _repositoryServicesGQL: RepositoryServicesGQL,
    private _changeDetectorRef: ChangeDetectorRef,
    private _dialogService: DialogService,
    private _loggingService: LoggingService
  ) {
    // this.openPullRequests = new CustomStore<PullRequest, number>({
    //   key: 'number',
    //   load: async options => {
    //     const result = await firstValueFrom(
    //       this._openPullRequestsGQL.fetch({
    //         owner: this.repository.owner,
    //         repo: this.repository.repo,
    //         search: options.searchValue
    //       })
    //     );
    //     return result.data.openPullRequests;
    //   }
    // });
  }

  // pullRequestDisplayExpr(item: PullRequest): string {
  //   return item ? `#${item.number}  ${item.title}  (${item.user?.name})` : '';
  // }

  async addServicesToPr(): Promise<void> {
    this.processing = true;

    try {
      // await firstValueFrom(
      //   this._pullRequestAddServicesGQL.mutate({
      //     owner: this.repository.owner,
      //     repo: this.repository.repo,
      //     pullRequestNumber: this.selectedPullRequest.number,
      //     services: this.selectedServices
      //   })
      // );

      await firstValueFrom(
        this._dialogService.showStatusDialog(
          StatusDialogType.Success,
          `Add Services Started`,
          [`The comment to add services to the pull request has been added.`, 'It may take a minute to update.'],
          [new DialogButton('OK', 'primary')]
        )
      );

      this.visible = false;
    } catch (error) {
      this._loggingService.error(error);
    }

    this.processing = false;
  }

  onVisibleChange(): void {
    this.visibleChange.emit(this.visible);
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
    this.visible = false;
    this._changeDetectorRef.detectChanges();
  }

  private clearFields() {
    // this.selectedPullRequest = null;
    this.processing = false;
    this.selectedServices = [];
    this.updateListSelection();
    if (this.selectPullRequestComponent) {
      this.selectPullRequestComponent.value = null;
    }
    this._changeDetectorRef.detectChanges();
  }
}
