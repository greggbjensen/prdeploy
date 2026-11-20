import { Component, Input, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { DeploySettingsCompare, RepositoryWorkflowsGQL } from 'src/app/shared/graphql';
import { RepoManager } from 'src/app/shared/managers';
import { LoggingService } from 'src/app/shared/services';
import { firstValueFrom } from 'rxjs';
import { SettingsLevel } from '../models';
import { BadgeFormComponent } from './badge-form/badge-form.component';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { FormsModule } from '@angular/forms';
import { MatSelectModule } from '@angular/material/select';

/** Deployment settings form, including the GitHub workflow dropdown fed by the repository's workflow files. */
@Component({
  selector: 'app-deploy-form',
  imports: [BadgeFormComponent, MatInputModule, MatButtonModule, FormsModule, MatSelectModule],
  templateUrl: './deploy-form.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './deploy-form.component.scss'
})
export class DeployFormComponent implements OnInit {
  environments: string[];

  /** Workflow file names (without folder) available in the repository. */
  workflows: string[] = [];
  loadingWorkflows = false;

  constructor(
    private _repoManager: RepoManager,
    private _repositoryWorkflowsGQL: RepositoryWorkflowsGQL,
    private _loggingService: LoggingService
  ) {}

  /** Loads the repository workflow files; a failure leaves the list empty. */
  async ngOnInit(): Promise<void> {
    this.loadingWorkflows = true;
    try {
      const response = await firstValueFrom(
        this._repositoryWorkflowsGQL.fetch({
          variables: { input: { owner: this._repoManager.owner, repo: this._repoManager.repo } }
        })
      );
      this.workflows = response.data.repositoryWorkflows.map(w => w.split('/').pop() ?? w);
    } catch (error) {
      this._loggingService.error(error);
    }
    this.loadingWorkflows = false;
  }

  /** Dropdown options: the repository workflows plus the currently configured value if it is not among them. */
  get workflowOptions(): string[] {
    const current = this.settingsCompare?.deployWorkflow?.[this.level];
    return current && !this.workflows.includes(current) ? [current, ...this.workflows] : this.workflows;
  }

  showOwner = true;

  private _settingsCompare: DeploySettingsCompare;
  @Input() set settingsCompare(value: DeploySettingsCompare) {
    this._settingsCompare = value;
    this.showOwner = this.level == 'repo';
    this.updateEnvironmentSelection();
    this.defaultPortalUrl();
  }

  get settingsCompare() {
    return this._settingsCompare;
  }

  private _level: SettingsLevel;
  @Input() set level(value: SettingsLevel) {
    this._level = value;
    this.showOwner = this.level == 'repo';
    this.updateEnvironmentSelection();
    this.defaultPortalUrl();
  }

  get level() {
    return this._level;
  }

  /** Defaults the portal URL to the current base URL when neither this level nor the owner level sets one. */
  defaultPortalUrl(): void {
    const urls = this._settingsCompare?.prdeployPortalUrl;
    if (!urls || !this._level) {
      return;
    }

    const inherited = this.showOwner ? urls['owner'] : null;
    if (!urls[this._level] && !inherited) {
      urls[this._level] = window.location.origin;
    }
  }

  updateEnvironmentSelection() {
    if (this.settingsCompare && this.settingsCompare.environments[this._level]) {
      this.environments = this.settingsCompare.environments[this._level].map(e => e.name);
    } else {
      this.environments = [];
    }
  }
}
