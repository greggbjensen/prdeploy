import { ChangeDetectorRef, Component, EventEmitter, Input, Output } from '@angular/core';
import { DxButtonModule, DxPopupModule, DxTextBoxModule } from 'devextreme-angular';
import { firstValueFrom } from 'rxjs';
import { OwnerRepoAddEnabledGQL } from 'src/app/shared/graphql';
import { NotificationManager } from 'src/app/shared/managers';
import { Repository } from 'src/app/shared/models';
import { LoggingService } from 'src/app/shared/services';

@Component({
  selector: 'app-add-repo-dialog',
  standalone: true,
  imports: [DxPopupModule, DxTextBoxModule, DxButtonModule],
  templateUrl: './add-repo-dialog.component.html',
  styleUrl: './add-repo-dialog.component.scss'
})
export class AddRepoDialogComponent {
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() repoAdded = new EventEmitter<Repository>();
  @Input() owner = '';
  repo: string;
  processing = false;

  private _visible = false;

  get visible() {
    return this._visible;
  }

  @Input()
  set visible(value: boolean) {
    this._visible = value;
    this.clearFields();
  }

  constructor(
    private _ownerRepoAddEnabledGQL: OwnerRepoAddEnabledGQL,
    private _notificationManager: NotificationManager,
    private _loggingService: LoggingService,
    private _changeDetectorRef: ChangeDetectorRef
  ) {}

  clearFields() {
    this.repo = '';
  }

  onVisibleChange(): void {
    this.visibleChange.emit(this.visible);
  }

  async add(): Promise<void> {
    if (!this.owner && this.repo) {
      return;
    }

    this.processing = true;

    try {
      const repository: Repository = {
        owner: this.owner,
        repo: this.repo
      };
      await firstValueFrom(
        this._ownerRepoAddEnabledGQL.mutate({
          input: repository
        })
      );

      this.repoAdded.emit(repository);
      this._notificationManager.show(`Repository ${this.owner}/${this.repo} added.`);
      this.visible = false;
      this._changeDetectorRef.detectChanges();
    } catch (error) {
      this._loggingService.error(error, `Error adding repository.`);
      this._notificationManager.show('Error adding repository.', 'error');
    }

    this.processing = false;
  }

  cancel(): void {
    this.visible = false;
    this._changeDetectorRef.detectChanges();
  }
}
