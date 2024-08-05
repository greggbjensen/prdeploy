import { Component, DestroyRef, OnInit } from '@angular/core';
import { DxDataGridModule, DxSelectBoxModule } from 'devextreme-angular';
import { DeployStateComparison, DeployStateComparisonGQL, Environment, EnvironmentsGQL } from '../shared/graphql';
import { RepoManager } from '../shared/managers';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';
import { SelectionChangedEvent } from 'devextreme/ui/select_box';

@Component({
  selector: 'app-environments',
  standalone: true,
  imports: [DxSelectBoxModule, DxDataGridModule],
  templateUrl: './environments.component.html',
  styleUrl: './environments.component.scss'
})
export class EnvironmentsComponent implements OnInit {
  private static readonly StableEnvironment: Environment = {
    name: 'stable'
  };

  constructor(
    private _deployStateComparisonGQL: DeployStateComparisonGQL,
    private _environmentsGQL: EnvironmentsGQL,
    private _repoManager: RepoManager,
    private _destroyRef: DestroyRef
  ) {}

  stateComparison: DeployStateComparison;
  environments: Environment[];
  sourceEnvironment: Environment;
  targetEnvironment: Environment;

  ngOnInit(): void {
    this._repoManager.valueChanged$.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(() => this.loadEnvironments());
  }

  sourceEnvironmentChanged(event: SelectionChangedEvent): void {
    if (event.selectedItem.name !== this.sourceEnvironment.name) {
      this.sourceEnvironment = event.selectedItem;
      this.updateStateComparison(true);
    }
  }

  targetEnvironmentChanged(event: SelectionChangedEvent): void {
    if (event.selectedItem.name !== this.targetEnvironment.name) {
      this.targetEnvironment = event.selectedItem;
      this.updateStateComparison(false);
    }
  }

  environmentDisplayExpr(item: Environment): string {
    return item ? item.name : '';
  }

  private async loadEnvironments(): Promise<void> {
    const environmentsResponse = await firstValueFrom(
      this._environmentsGQL.fetch({
        input: {
          owner: this._repoManager.owner,
          repo: this._repoManager.repo
        }
      })
    );

    this.environments = [...environmentsResponse.data.environments, EnvironmentsComponent.StableEnvironment];
    this.updateStateComparison();
  }

  private async updateStateComparison(sourceChanged = true): Promise<void> {
    this.updateCompareEnvironments(sourceChanged);

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

  private updateCompareEnvironments(sourceChanged: boolean): void {
    if (!this.environments || this.environments.length === 0) {
      return;
    }

    // Get first environment if none is selected.
    if (!this.sourceEnvironment) {
      this.sourceEnvironment = this.environments.find(e => e.name !== EnvironmentsComponent.StableEnvironment.name);
    }

    // Get stable environment if none is selected.
    if (!this.targetEnvironment) {
      // Default to stable comparison as this is the most common.
      // Stable is a special environment for the last version release to production.
      this.targetEnvironment = EnvironmentsComponent.StableEnvironment;
    }

    if (this.sourceEnvironment.name !== this.targetEnvironment.name) {
      return;
    }

    if (sourceChanged) {
      // Find last item, more stable.
      const nonSourceEnvironments = this.environments.filter(e => e.name !== this.sourceEnvironment.name);
      this.targetEnvironment = nonSourceEnvironments[nonSourceEnvironments.length - 1];
    } else {
      // Find first item, less stable, because target should be more stable if possible.
      this.sourceEnvironment = this.environments.find(e => e.name !== this.targetEnvironment.name);
    }
  }
}
