import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { Environment, EnvironmentsGQL } from '../shared/graphql';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { RepoManager, RouteManager } from '../shared/managers';
import { EnvironmentsGridComponent } from './environments-grid/environments-grid.component';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectChange, MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';

@Component({
  selector: 'app-environments',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, EnvironmentsGridComponent, MatSelectModule, MatFormFieldModule],
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
    private _routeManager: RouteManager,
    private _route: ActivatedRoute,
    private _changeDetectorRef: ChangeDetectorRef
  ) {
    this._repoManager.valueChanged$.pipe(takeUntilDestroyed()).subscribe(() => this.loadEnvironments());
  }

  environments: Environment[];
  sourceEnvironment: Environment;
  targetEnvironment: Environment;

  private _initialSourceEnvironment = '';

  ngOnInit(): void {
    firstValueFrom(this._route.queryParamMap).then(param => {
      this._initialSourceEnvironment = param.get('sourceEnvironment');
    });
  }

  sourceEnvironmentChanged(event: MatSelectChange): void {
    if (event.value.name !== this.sourceEnvironment.name) {
      this.sourceEnvironment = event.value;
      this.updateCompareEnvironments(true);
    }
  }

  targetEnvironmentChanged(event: MatSelectChange): void {
    if (event.value.name !== this.targetEnvironment.name) {
      this.targetEnvironment = event.value;
      this.updateCompareEnvironments(false);
    }
  }

  environmentDisplayExpr(item: Environment): string {
    return item ? item.name : '';
  }

  private async loadEnvironments(): Promise<void> {
    if (!this._repoManager.isValid) {
      return;
    }

    // Make sure to reset for route changes.
    this.sourceEnvironment = null;
    this.targetEnvironment = null;

    const owner = this._repoManager.owner;
    const repo = this._repoManager.repo;
    const environmentsResponse = await firstValueFrom(
      this._environmentsGQL.fetch({
        input: {
          owner,
          repo
        }
      })
    );

    if (!environmentsResponse.data.environments || environmentsResponse.data.environments.length === 0) {
      await this._routeManager.navigate(['/', owner, repo, 'settings']);
      return;
    }

    this.environments = [...environmentsResponse.data.environments, EnvironmentsComponent.StableEnvironment];
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
      this._changeDetectorRef.detectChanges();
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

    this._changeDetectorRef.detectChanges();
  }
}
