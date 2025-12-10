import { Component, inject, OnInit } from "@angular/core";
import { Router, RouterLink, RouterOutlet } from "@angular/router";
import { AuthService } from "./services/auth.service";

@Component({
  selector: "app-root",
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  templateUrl: "./app.component.html",
  styleUrl: "./app.component.less",
})
export class AppComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);

  title = "DailyWatt";
  loggedIn = this.authService.isLoggedIn;

  ngOnInit() {
    // Check session validity on app initialization
    this.authService.hasValidSession();
  }

  logout() {
    this.authService.logout();
    this.router.navigate(["/login"]);
  }
}
