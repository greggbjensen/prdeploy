import { LiveAnnouncer } from '@angular/cdk/a11y';
import { Component, Input, ViewChild } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSort, MatSortModule, Sort } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { firstValueFrom } from 'rxjs';
import {
  DeployStateComparison,
  DeployStateComparisonGQL,
  Environment,
  ServiceComparison
} from 'src/app/shared/graphql';
import { RepoManager } from 'src/app/shared/managers';

@Component({
  selector: 'app-environments-grid',
  standalone: true,
  imports: [MatTableModule, MatSortModule, MatProgressSpinnerModule],
  templateUrl: './environments-grid.component.html',
  styleUrl: './environments-grid.component.scss'
})
export class EnvironmentsGridComponent {
  private _sourceEnvironment: Environment;
  private _targetEnvironment: Environment;

  stateComparison: DeployStateComparison;
  serviceComparisons: MatTableDataSource<ServiceComparison>;
  displayedColumns: string[] = ['service', 'source-run-id', 'source-version', 'target-run-id', 'target-version'];

  @ViewChild(MatSort) sort: MatSort;

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
    private _liveAnnouncer: LiveAnnouncer,
    private _repoManager: RepoManager
  ) {}

  getRunUrl(runId: string) {
    return `https://github.com/${this._repoManager.owner}/${this._repoManager.repo}/actions/runs/${runId}`;
  }

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

    this.serviceComparisons = new MatTableDataSource(stateResponse.data.deployStateComparison.serviceComparisons);
    this.serviceComparisons.sort = this.sort;
    this.stateComparison = stateResponse.data.deployStateComparison;
  }

  announceSortChange(sortState: Sort) {
    // TODO: Finalize sorting.
    if (sortState.direction) {
      this._liveAnnouncer.announce(`Sorted ${sortState.direction}ending`);
    } else {
      this._liveAnnouncer.announce('Sorting cleared');
    }
  }
}
