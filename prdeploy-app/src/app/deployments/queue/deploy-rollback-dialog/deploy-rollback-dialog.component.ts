import { ChangeDetectorRef, Component, EventEmitter, Input, Output } from '@angular/core';
import { DxButtonModule, DxNumberBoxModule, DxPopupModule } from 'devextreme-angular';
import { firstValueFrom } from 'rxjs';
import { DeployEnvironmentRollbackGQL, Repository } from 'src/app/shared/graphql';
import { DialogButton, DialogService, LoggingService, StatusDialogType } from 'src/app/shared/services';

@Component({
  selector: 'app-deploy-rollback-dialog',
  standalone: true,
  imports: [DxPopupModule, DxButtonModule, DxNumberBoxModule],
  templateUrl: './deploy-rollback-dialog.component.html',
  styleUrl: './deploy-rollback-dialog.component.scss'
})
export class DeployRollbackDialogComponent {
  @Input() repository: Repository;
  @Input() environment: string;
  @Input() pullNumber: string;

  processing = false;
  rollbackCount = 1;

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

  constructor(
    private _deployEnvironmentDeployGQL: DeployEnvironmentRollbackGQL,
    private _dialogService: DialogService,
    private _loggingService: LoggingService,
    private _changeDetectorRef: ChangeDetectorRef
  ) {}

  async rollback(): Promise<void> {
    this.processing = true;

    try {
      await firstValueFrom(
        this._deployEnvironmentDeployGQL.mutate({
          owner: this.repository.owner,
          repo: this.repository.repo,
          environment: this.environment,
          pullRequestNumber: this.pullNumber,
          count: this.rollbackCount > 1 ? this.rollbackCount : undefined
        })
      );

      await firstValueFrom(
        this._dialogService.showStatusDialog(
          StatusDialogType.Success,
          `Rollback ${this.environment} Started`,
          [
            `The comment to rollback from the ${this.environment} environment has been added.`,
            'It may take a minute to update.'
          ],
          [new DialogButton('OK', 'primary')]
        )
      );

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
    this.rollbackCount = 1;
    this.processing = false;
  }
}
