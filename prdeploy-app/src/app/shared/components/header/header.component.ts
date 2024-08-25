import { Component, Input, Output, EventEmitter } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { UserPanelComponent } from '../user-panel/user-panel.component';
import { RepoManager } from '../../managers';
import { firstValueFrom } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { OwnerRepos } from '../../graphql';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectChange, MatSelectModule } from '@angular/material/select';

@Component({
  selector: 'app-header',
  templateUrl: 'header.component.html',
  styleUrls: ['./header.component.scss'],
  standalone: true,
  imports: [MatToolbarModule, MatButtonModule, MatIconModule, MatSelectModule, UserPanelComponent]
})
export class HeaderComponent {
  @Output()
  menuToggle = new EventEmitter<boolean>();

  @Input()
  menuToggleEnabled = false;

  @Input()
  title!: string;

  owners: string[];
  repos: string[];

  private _ownerRepos: OwnerRepos[] = [];

  constructor(
    public repoManager: RepoManager,
    private _route: ActivatedRoute
  ) {
    firstValueFrom(this._route.queryParamMap).then(param => {
      this.repoManager.owner = param.get('owner');
      this.repoManager.repo = param.get('repo');
    });

    this.repoManager.ownerReposChanged$
      .pipe(takeUntilDestroyed())
      .subscribe(ownerRepos => this.updateOwnerRepos(ownerRepos));

    this.fetchOwnerRepos();
  }

  async fetchOwnerRepos() {
    this.repoManager.fetchOwnerRepos();
  }

  async selectedRepoChanged(event: MatSelectChange): Promise<void> {
    this.repoManager.repo = event.value;
  }

  async selectedOwnerChanged(event: MatSelectChange): Promise<void> {
    this.repoManager.owner = event.value;
    this.filterOwnerRepos();
  }

  private updateOwnerRepos(ownerRepos: OwnerRepos[]) {
    this._ownerRepos = ownerRepos || [];
    this.owners = this._ownerRepos.map(o => o.owner);
    if (!this.repoManager.owner || this.owners.includes(this.repoManager.owner.toLowerCase())) {
      this.repoManager.owner = this.owners[0];
    }

    this.filterOwnerRepos();
  }

  private filterOwnerRepos() {
    this.repos = this._ownerRepos.find(r => r.owner === this.repoManager.owner)?.repos || [];
    if (!this.repoManager.repo || !this.repos.includes(this.repoManager.repo.toLowerCase())) {
      this.repoManager.repo = this.repos ? this.repos[0] : null;
    }
  }

  toggleMenu = () => {
    this.menuToggle.emit();
  };
}
