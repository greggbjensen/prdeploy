import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { DxButtonModule } from 'devextreme-angular';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterModule, DxButtonModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent {}
