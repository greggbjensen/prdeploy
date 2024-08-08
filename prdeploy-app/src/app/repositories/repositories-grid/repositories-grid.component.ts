import { Component } from '@angular/core';
import { DxDataGridModule } from 'devextreme-angular';

@Component({
  selector: 'app-repositories-grid',
  standalone: true,
  imports: [DxDataGridModule],
  templateUrl: './repositories-grid.component.html',
  styleUrl: './repositories-grid.component.scss'
})
export class RepositoriesGridComponent {}
