import { Component, Inject } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-forbidden',
  templateUrl: './forbidden.component.html',
  styleUrls: ['./forbidden.component.scss'],
  standalone: true,
  imports: [RouterLink]
})
export class ForbiddenComponent {
  constructor(
  ) {}
}
