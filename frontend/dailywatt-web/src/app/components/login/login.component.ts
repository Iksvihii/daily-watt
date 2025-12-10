import { Component, inject, signal } from "@angular/core";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { Router, RouterLink } from "@angular/router";
import { AuthService } from "../../services/auth.service";
import { LoginRequest } from "../../models/auth.models";

@Component({
  selector: "app-login",
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: "./login.component.html",
  styleUrl: "./login.component.less",
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  error = signal<string | undefined>(undefined);
  loading = signal(false);

  form = this.fb.group({
    email: ["", [Validators.required, Validators.email]],
    password: ["", [Validators.required]],
  });

  submit() {
    if (this.form.invalid) {
      return;
    }
    this.loading.set(true);
    const credentials = this.form.value as LoginRequest;
    this.auth.login(credentials).subscribe({
      next: () => this.router.navigate(["/dashboard"]),
      error: (err: { error?: { error?: string } }) => {
        this.error.set(err.error?.error || "Login failed");
        this.loading.set(false);
      },
    });
  }
}
