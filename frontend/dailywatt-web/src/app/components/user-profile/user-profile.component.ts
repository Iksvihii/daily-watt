import { Component, OnInit, inject, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { AuthService } from "../../services/auth.service";
import { ChangePasswordRequest, UserProfile } from "../../models/auth.models";

@Component({
  selector: "app-user-profile",
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: "./user-profile.component.html",
  styleUrl: "./user-profile.component.less",
})
export class UserProfileComponent implements OnInit {
  private auth = inject(AuthService);
  private fb = inject(FormBuilder);

  profile = signal<UserProfile | null>(null);
  profileMessage = signal<string | undefined>(undefined);
  profileSaving = signal(false);
  passwordSaving = signal(false);
  loading = signal(true);

  profileForm = this.fb.group({
    username: ["", [Validators.required, Validators.minLength(2)]],
  });

  passwordForm = this.fb.group({
    currentPassword: ["", Validators.required],
    newPassword: ["", [Validators.required, Validators.minLength(6)]],
  });

  ngOnInit(): void {
    this.loadProfile();
  }

  private loadProfile(): void {
    this.auth.getProfile().subscribe({
      next: (profile: UserProfile) => {
        this.profile.set(profile);
        this.profileForm.patchValue({
          username: profile.username,
        });
        this.loading.set(false);
      },
      error: () => {
        this.profileMessage.set("Failed to load profile");
        this.loading.set(false);
      },
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) {
      return;
    }

    this.profileSaving.set(true);
    const username = this.profileForm.get("username")?.value || "";

    this.auth.updateProfile({ username }).subscribe({
      next: () => {
        this.profileMessage.set("Profile updated successfully");
        this.profileSaving.set(false);
        this.loadProfile();
      },
      error: () => {
        this.profileMessage.set("Failed to update profile");
        this.profileSaving.set(false);
      },
    });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) {
      return;
    }

    this.passwordSaving.set(true);
    const request: ChangePasswordRequest = {
      currentPassword: this.passwordForm.get("currentPassword")?.value || "",
      newPassword: this.passwordForm.get("newPassword")?.value || "",
    };

    this.auth.changePassword(request).subscribe({
      next: () => {
        this.profileMessage.set("Password changed successfully");
        this.passwordSaving.set(false);
        this.passwordForm.reset();
      },
      error: () => {
        this.profileMessage.set("Failed to change password");
        this.passwordSaving.set(false);
      },
    });
  }
}
