import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { DxButtonModule } from 'devextreme-angular';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterModule, DxButtonModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  forbidden = false;
  constructor(private _activatedRoute: ActivatedRoute) {}

  ngOnInit(): void {
    this.forbidden = this._activatedRoute.snapshot.queryParams['forbidden'] == 'true';
  }
}
