import { Component, Input } from '@angular/core';
import { DxDataGridModule } from 'devextreme-angular';
import { firstValueFrom } from 'rxjs';
import { DeployStateComparison, DeployStateComparisonGQL, Environment } from 'src/app/shared/graphql';
import { RepoManager } from 'src/app/shared/managers';

@Component({
  selector: 'app-environments-grid',
  standalone: true,
  imports: [DxDataGridModule],
  templateUrl: './environments-grid.component.html',
  styleUrl: './environments-grid.component.scss'
})
export class EnvironmentsGridComponent {
  private _sourceEnvironment: Environment;
  private _targetEnvironment: Environment;

  stateComparison: DeployStateComparison;

  @Input() set sourceEnvironment(value: Environment) {
    if (value != this._sourceEnvironment) {
      this._sourceEnvironment = value;
      this.updateStateComparison();
    }
  }

  get sourceEnvironment() {
    return this._sourceEnvironment;
  }

  @Input() set targetEnvironment(value: Environment) {
    if (value != this._sourceEnvironment) {
      this._targetEnvironment = value;
      this.updateStateComparison();
    }
  }

  get targetEnvironment() {
    return this._targetEnvironment;
  }

  constructor(
    private _deployStateComparisonGQL: DeployStateComparisonGQL,
    private _repoManager: RepoManager
  ) {}

  async updateStateComparison(): Promise<void> {
    if (!this.sourceEnvironment || !this.targetEnvironment) {
      return;
    }

    const stateResponse = await firstValueFrom(
      this._deployStateComparisonGQL.fetch({
        input: {
          owner: this._repoManager.owner,
          repo: this._repoManager.repo,
          sourceEnvironment: this.sourceEnvironment.name,
          targetEnvironment: this.targetEnvironment.name
        }
      })
    );

    this.stateComparison = stateResponse.data.deployStateComparison;
  }
}
