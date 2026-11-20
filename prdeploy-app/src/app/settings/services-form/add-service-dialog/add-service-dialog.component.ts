import { Component, ChangeDetectionStrategy, Inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogActions,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MtxButtonModule } from '@ng-matero/extensions/button';
import { firstValueFrom } from 'rxjs';
import { RepositoryWorkflowsGQL } from 'src/app/shared/graphql';
import { RepoManager } from 'src/app/shared/managers';
import { LoggingService } from 'src/app/shared/services';
import { workflowServiceName } from './workflow-service-name';

/** Data passed to the add service dialog. */
export interface AddServiceDialogData {
  /** Builds workflow pattern used to derive the service name from a workflow file. */
  workflowPattern?: string | null;
}

/** Service chosen in the add service dialog. */
export interface AddServiceDialogResult {
  /** Name of the service. */
  name: string;
  /** Path of the service within the repository. */
  path: string;
  /** Workflow file name the service was created from, such as `api-build.yaml`, if one was selected. */
  workflow?: string;
}

/** Dialog for adding a service, optionally starting from one of the repository's workflow files. */
@Component({
  selector: 'app-add-service-dialog',
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatButtonModule,
    MtxButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule
  ],
  templateUrl: './add-service-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './add-service-dialog.component.scss'
})
export class AddServiceDialogComponent implements OnInit {
  form = new FormGroup({
    workflow: new FormControl<string | null>(null),
    name: new FormControl('', Validators.required),
    path: new FormControl('', Validators.required)
  });

  /** Workflow file paths available in the repository. */
  workflows: string[] = [];
  loadingWorkflows = false;

  constructor(
    private _dialogRef: MatDialogRef<AddServiceDialogComponent>,
    private _repoManager: RepoManager,
    private _repositoryWorkflowsGQL: RepositoryWorkflowsGQL,
    private _loggingService: LoggingService,
    @Inject(MAT_DIALOG_DATA) private _data: AddServiceDialogData | null
  ) {
    this._dialogRef
      .afterOpened()
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        this.clearFields();
      });

    // Selecting a workflow fills in the service name and path, which can still be edited afterward.
    this.form.controls.workflow.valueChanges.pipe(takeUntilDestroyed()).subscribe(workflow => {
      if (workflow) {
        const name = workflowServiceName(workflow, this._data?.workflowPattern);
        this.form.controls.name.setValue(name);
        this.form.controls.path.setValue(name);
      }
    });
  }

  /** Loads the repository workflow files; a failure leaves the list empty so the name can still be typed. */
  async ngOnInit(): Promise<void> {
    this.loadingWorkflows = true;
    try {
      const response = await firstValueFrom(
        this._repositoryWorkflowsGQL.fetch({
          variables: { input: { owner: this._repoManager.owner, repo: this._repoManager.repo } }
        })
      );
      this.workflows = response.data.repositoryWorkflows;
    } catch (error) {
      this._loggingService.error(error);
    }
    this.loadingWorkflows = false;
  }

  /** Returns the workflow file name without its folder, for display in the dropdown. */
  workflowFileName(path: string): string {
    return path.split('/').pop() ?? path;
  }

  clearFields() {
    this.form.reset();
  }

  add(): void {
    const workflow = this.form.value.workflow;
    this._dialogRef.close({
      name: this.form.value.name,
      path: this.form.value.path,
      workflow: workflow ? this.workflowFileName(workflow) : undefined
    });
  }

  cancel(): void {
    this._dialogRef.close(null);
  }
}
