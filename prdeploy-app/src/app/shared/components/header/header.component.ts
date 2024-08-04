import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { UserPanelComponent } from '../user-panel/user-panel.component';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DxToolbarModule } from 'devextreme-angular/ui/toolbar';
import { AuthService } from '../../services';
import { DxSelectBoxModule } from 'devextreme-angular';
import { RepoManager } from '../../managers';
import { first, firstValueFrom } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { PrDeployEnabledRepositoriesGQL, Repository } from '../../graphql';
import { uniq } from 'lodash';
import { SelectionChangedEvent } from 'devextreme/ui/select_box';
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

  private _repositories: Repository[] = [];

  constructor(
    public repoManager: RepoManager,
    private _authService: AuthService,
    private _route: ActivatedRoute,
    private _prDeployEnabledRepositoriesGQL: PrDeployEnabledRepositoriesGQL
  ) {}

  ngOnInit(): void {
    firstValueFrom(this._route.queryParamMap).then(param => {
      this.repoManager.owner = param.get('owner');
      this.repoManager.repo = param.get('repo');
    });

    this._prDeployEnabledRepositoriesGQL
      .fetch()
      .pipe(first())
      .subscribe(r => {
        this._repositories = r.data.prDeployEnabledRepositories;
        this.owners = uniq(this._repositories.map(r => r.owner));
        if (!this.repoManager.owner || this.owners.includes(this.repoManager.owner.toLowerCase())) {
          this.repoManager.owner = this.owners[0];
        }

        this.updateOwnerRepos();
      });
  }

  async selectedRepoChanged(event: SelectionChangedEvent): Promise<void> {
    this.repoManager.repo = event.selectedItem;
  }

  async selectedOwnerChanged(event: SelectionChangedEvent): Promise<void> {
    this.repoManager.owner = event.selectedItem;
    this.updateOwnerRepos();
  }

  private updateOwnerRepos() {
    this.repos = this._repositories.filter(r => r.owner === this.repoManager.owner).map(r => r.repo);
    if (!this.repoManager.repo || !this.repos.includes(this.repoManager.repo.toLowerCase())) {
      this.repoManager.repo = this.repos ? this.repos[0] : null;
    }
  }

  toggleMenu = () => {
    this.menuToggle.emit();
  };
}
