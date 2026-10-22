import { Component } from '@angular/core';
import { RequestListComponent } from './features/requests/request-list/request-list.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RequestListComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {}
