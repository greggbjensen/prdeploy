import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { DxDataGridModule } from 'devextreme-angular';
import { OwnerRepos } from 'src/app/shared/graphql';
import { Repository } from 'src/app/shared/models';

@Component({
  selector: 'app-repositories-grid',
  standalone: true,
  imports: [DxDataGridModule, MatButtonModule, MatIconModule],
  templateUrl: './repositories-grid.component.html',
  styleUrl: './repositories-grid.component.scss'
})
export class RepositoriesGridComponent {
  @Output() showAddRepo = new EventEmitter<string>();
  @Output() onRemoveRepo = new EventEmitter<Repository>();

  repositories: Repository[];

  private _ownerRepos: OwnerRepos[];

  @Input() set ownerRepos(value: OwnerRepos[]) {
    this._ownerRepos = value;

    if (this._ownerRepos) {
      this.repositories = this._ownerRepos.flatMap(o =>
        o.repos.map(repo => ({
          owner: o.owner,
          repo
        }))
      );
    } else {
      this.repositories = [];
    }
  }

  get ownerRepos() {
    return this._ownerRepos;
  }

  removeRepo(repository: Repository) {
    this.onRemoveRepo.emit(repository);
  }

  addRepo(owner: string) {
    this.showAddRepo.emit(owner);
  }
}
