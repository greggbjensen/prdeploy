import { Component, Input, Output, EventEmitter, OnInit, DestroyRef } from '@angular/core';
import { UserPanelComponent } from '../user-panel/user-panel.component';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DxToolbarModule } from 'devextreme-angular/ui/toolbar';
import { AuthService } from '../../services';
import { DxSelectBoxModule } from 'devextreme-angular';
import { RepoManager } from '../../managers';
import { firstValueFrom } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { EnabledOwnerReposGQL, OwnerRepos } from '../../graphql';
import { SelectionChangedEvent } from 'devextreme/ui/select_box';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
@Component({
  selector: 'app-header',
  templateUrl: 'header.component.html',
  styleUrls: ['./header.component.scss'],
  standalone: true,
  imports: [DxToolbarModule, DxButtonModule, DxSelectBoxModule, UserPanelComponent]
})
export class HeaderComponent implements OnInit {
  @Output()
  menuToggle = new EventEmitter<boolean>();

  @Input()
  menuToggleEnabled = false;

  @Input()
  title!: string;

  owners: string[];
  repos: string[];
  userMenuItems = [
    {
      text: 'Logout',
      icon: 'runner',
      onClick: () => {
        this._authService.logout();
      }
    }
  ];

  private _ownerRepos: OwnerRepos[] = [];

  constructor(
    public repoManager: RepoManager,
    private _authService: AuthService,
    private _route: ActivatedRoute,
    private _router: Router,
    private _enabledOwnerReposGQL: EnabledOwnerReposGQL,
    private _destoryRef: DestroyRef
  ) {}

  ngOnInit(): void {
    firstValueFrom(this._route.queryParamMap).then(param => {
      this.repoManager.owner = param.get('owner');
      this.repoManager.repo = param.get('repo');
    });

    this.fetchOwnerRepos();

    this.repoManager.ownerReposChanged$
      .pipe(takeUntilDestroyed(this._destoryRef))
      .subscribe(ownerRepos => this.updateOwnerRepos(ownerRepos));
  }

  async fetchOwnerRepos() {
    const result = await firstValueFrom(this._enabledOwnerReposGQL.fetch());
    this.updateOwnerRepos(result.data.enabledOwnerRepos);
  }

  async selectedRepoChanged(event: SelectionChangedEvent): Promise<void> {
    this.repoManager.repo = event.selectedItem;
  }

  async selectedOwnerChanged(event: SelectionChangedEvent): Promise<void> {
    this.repoManager.owner = event.selectedItem;
    this.filterOwnerRepos();
  }

  private updateOwnerRepos(ownerRepos: OwnerRepos[]) {
    this._ownerRepos = ownerRepos || [];
    if (this._ownerRepos.length === 0) {
    }

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
