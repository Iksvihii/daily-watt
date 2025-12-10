import { Component, inject } from "@angular/core";
import { RouterLink, RouterOutlet } from "@angular/router";
import { AuthService } from "./services/auth.service";

@Component({
  selector: "app-root",
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  templateUrl: "./app.component.html",
  styleUrl: "./app.component.less",
})
export class AppComponent {
  private authService = inject(AuthService);

  title = "DailyWatt";
  loggedIn = this.authService.isLoggedIn;

  logout() {
    this.authService.logout();
  }
}
