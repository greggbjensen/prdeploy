import { ChangeDetectionStrategy, Component, Inject, OnInit } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogActions,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from '@angular/material/dialog';
import { MatListModule, MatSelectionListChange } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MtxButtonModule } from '@ng-matero/extensions/button';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { PullRequestAddServicesGQL, PullRequestServiceStatus, PullRequestServicesGQL } from 'src/app/shared/graphql';
import { NotificationManager, RepoManager } from 'src/app/shared/managers';
import { DialogResult } from 'src/app/shared/models';
import { LoggingService } from 'src/app/shared/services';
import { AlertPanelComponent } from 'src/app/shared/components';
import { PrServicesDialogData } from './pr-services-dialog-data';

/**
 * Lists every configured service for a pull request. Services whose build is already triggered by the pull request's
 * checks are shown checked and locked; any extra services that are checked get added through an `/add` command, which
 * writes a `.deploy` file line into each service's configured directory so its build runs.
 */
@Component({
  selector: 'app-pr-services-dialog',
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatButtonModule,
    MatListModule,
    MatProgressSpinnerModule,
    MtxButtonModule,
    AlertPanelComponent,
    RouterModule
  ],
  templateUrl: './pr-services-dialog.component.html',
  styleUrl: './pr-services-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager
})
export class PrServicesDialogComponent implements OnInit {
  services: PullRequestServiceStatus[] = [];
  /** Names of services the user checked that the pull request does not trigger yet. */
  selectedServices: string[] = [];
  loading = true;
  processing = false;

  constructor(
    public repoManager: RepoManager,
    @Inject(MAT_DIALOG_DATA) public data: PrServicesDialogData,
    private _pullRequestServicesGQL: PullRequestServicesGQL,
    private _pullRequestAddServicesGQL: PullRequestAddServicesGQL,
    private _notificationManager: NotificationManager,
    private _dialogRef: MatDialogRef<PrServicesDialogComponent>,
    private _loggingService: LoggingService
  ) {}

  /** Loads the configured services and which ones the pull request already triggers. */
  async ngOnInit(): Promise<void> {
    try {
      const response = await firstValueFrom(
        this._pullRequestServicesGQL.fetch({
          variables: {
            input: { owner: this.repoManager.owner, repo: this.repoManager.repo, pullNumber: this.data.pullNumber }
          },
          fetchPolicy: 'network-only'
        })
      );
      this.services = response.data.pullRequestServices;
    } catch (error) {
      this._loggingService.error(error);
    }

    this.loading = false;
  }

  /** Records the newly checked services; already triggered services are disabled and never included. */
  onSelectionChange(event: MatSelectionListChange): void {
    this.selectedServices = event.source.selectedOptions.selected.filter(o => !o.disabled).map(o => o.value);
  }

  /** Adds the checked services to the pull request, then closes the dialog. */
  async addServices(): Promise<void> {
    this.processing = true;

    try {
      await firstValueFrom(
        this._pullRequestAddServicesGQL.mutate({
          variables: {
            input: {
              owner: this.repoManager.owner,
              repo: this.repoManager.repo,
              pullNumber: this.data.pullNumber,
              services: this.selectedServices
            }
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

  /** Closes the dialog without changes. */
  cancel(): void {
    this._dialogRef.close(DialogResult.Cancel);
  }
}
