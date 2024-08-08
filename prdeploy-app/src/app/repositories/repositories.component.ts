import { Component, OnInit } from '@angular/core';
import { RepositoriesGridComponent } from './repositories-grid/repositories-grid.component';
import { EnabledOwnerReposGQL, OwnerRepos } from '../shared/graphql';
import { firstValueFrom } from 'rxjs';
import { DxButtonModule } from 'devextreme-angular';

@Component({
  selector: 'app-repositories',
  standalone: true,
  imports: [DxButtonModule, RepositoriesGridComponent],
  templateUrl: './repositories.component.html',
  styleUrl: './repositories.component.scss'
})
export class RepositoriesComponent implements OnInit {
  constructor(private _enabledOwnerReposGQL: EnabledOwnerReposGQL) {}

  ownerRepos: OwnerRepos[];

  ngOnInit(): void {
    this.updateOwnerRepos();
  }

  async updateOwnerRepos(): Promise<void> {
    const response = await firstValueFrom(this._enabledOwnerReposGQL.fetch());
    this.ownerRepos = response.data.enabledOwnerRepos;
  }
}
