import { Component, inject, OnInit, effect } from "@angular/core";
import { Router, RouterLink, RouterOutlet } from "@angular/router";
import { CommonModule } from "@angular/common";
import { AuthService } from "./services/auth.service";
import { BackendHealthService } from "./services/backend-health.service";
import { BackendErrorComponent } from "./components/backend-error/backend-error.component";
@Component({
  selector: "app-root",
  standalone: true,
  imports: [RouterOutlet, RouterLink, CommonModule, BackendErrorComponent],
  templateUrl: "./app.component.html",
  styleUrl: "./app.component.less",
})
export class AppComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  protected backendHealth = inject(BackendHealthService);

  title = "DailyWatt";
  loggedIn = this.authService.isLoggedIn;

  constructor() {
    // Auto-redirect when backend comes back online
    effect(() => {
      const isAvailable = this.backendHealth.isBackendAvailable();
      if (isAvailable === true) {
        // Backend is now available; navigate to appropriate page
        if (this.authService.hasValidSession()) {
          this.router.navigate(["/dashboard"]);
        } else {
          this.router.navigate(["/login"]);
        }
      }
    });
  }

  ngOnInit() {
    // Check session validity on app initialization
    this.authService.hasValidSession();
  }

  logout() {
    this.authService.logout();
    this.router.navigate(["/login"]);
  }
}
