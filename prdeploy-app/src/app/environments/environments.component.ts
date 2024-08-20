import { Component, OnInit, ViewChild } from '@angular/core';
import { DxFormModule, DxSelectBoxModule } from 'devextreme-angular';
import { Environment, EnvironmentsGQL } from '../shared/graphql';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';
import { SelectionChangedEvent } from 'devextreme/ui/select_box';
import { ActivatedRoute } from '@angular/router';
import { RepoManager } from '../shared/managers';
import { EnvironmentsGridComponent } from './environments-grid/environments-grid.component';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-environments',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, DxFormModule, DxSelectBoxModule, EnvironmentsGridComponent],
  templateUrl: './environments.component.html',
  styleUrl: './environments.component.scss'
})
export class EnvironmentsComponent implements OnInit {
  private static readonly StableEnvironment: Environment = {
    name: 'stable'
  };

  @ViewChild(EnvironmentsGridComponent) environtmentsGrid: EnvironmentsGridComponent;

  constructor(
    private _environmentsGQL: EnvironmentsGQL,
    private _repoManager: RepoManager,
    private _route: ActivatedRoute
  ) {}

  environments: Environment[];
  sourceEnvironment: Environment;
  targetEnvironment: Environment;

  private _initialSourceEnvironment = '';

  ngOnInit(): void {
    firstValueFrom(this._route.queryParamMap).then(param => {
      this._initialSourceEnvironment = param.get('sourceEnvironment');
    });
    this._repoManager.valueChanged$.pipe(takeUntilDestroyed()).subscribe(() => this.loadEnvironments());
  }

  sourceEnvironmentChanged(event: SelectionChangedEvent): void {
    if (event.selectedItem.name !== this.sourceEnvironment.name) {
      this.sourceEnvironment = event.selectedItem;
      this.updateCompareEnvironments(true);
    }
  }

  targetEnvironmentChanged(event: SelectionChangedEvent): void {
    if (event.selectedItem.name !== this.targetEnvironment.name) {
      this.targetEnvironment = event.selectedItem;
      this.updateCompareEnvironments(false);
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

    this.environments = [...(environmentsResponse.data.environments || []), EnvironmentsComponent.StableEnvironment];
    this.updateCompareEnvironments(true);
  }

  updateCompareEnvironments(sourceChanged: boolean): void {
    if (!this.environments || this.environments.length === 0) {
      return;
    }

    // Get first environment if none is selected.
    if (!this.sourceEnvironment) {
      this.sourceEnvironment = this._initialSourceEnvironment
        ? this.environments.find(e => e.name === this._initialSourceEnvironment)
        : this.environments.find(e => e.name !== EnvironmentsComponent.StableEnvironment.name);

      if (!this.sourceEnvironment) {
        this.sourceEnvironment = this.environments.find(e => e.name !== EnvironmentsComponent.StableEnvironment.name);
      }
    }

    // Get stable environment if none is selected.
    if (!this.targetEnvironment) {
      // Default to stable comparison as this is the most common.
      // Stable is a special environment for the last version release to production.
      this.targetEnvironment = EnvironmentsComponent.StableEnvironment;
    }

    if (this.sourceEnvironment?.name !== this.targetEnvironment?.name) {
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
