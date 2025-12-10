import { Component, signal } from "@angular/core";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { Router, RouterLink } from "@angular/router";
import { AuthService } from "../../services/auth.service";
import { RegisterRequest } from "../../models/auth.models";

@Component({
  selector: "app-register",
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: "./register.component.html",
  styleUrl: "./register.component.less",
})
export class RegisterComponent {
  error = signal<string | undefined>(undefined);
  loading = signal(false);

  form = this.fb.group({
    email: ["", [Validators.required, Validators.email]],
    username: ["", [Validators.required, Validators.minLength(2)]],
    password: ["", [Validators.required, Validators.minLength(6)]],
  });

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router
  ) {}

  submit() {
    if (this.form.invalid) {
      return;
    }
    this.loading.set(true);
    const payload = this.form.value as RegisterRequest;
    this.auth.register(payload).subscribe({
      next: () => this.router.navigate(["/dashboard"]),
      error: (err: { error?: { errors?: string[]; error?: string } }) => {
        this.error.set(
          err.error?.errors?.join(", ") ||
            err.error?.error ||
            "Registration failed"
        );
        this.loading.set(false);
      },
    });
  }
}
