import { Routes } from "@angular/router";
import { LoginComponent } from "./components/login/login.component";
import { RegisterComponent } from "./components/register/register.component";
import { EnedisSettingsComponent } from "./components/enedis-settings/enedis-settings.component";
import { DashboardComponent } from "./components/dashboard/dashboard.component";
import { authGuard, guestGuard } from "./guards/auth.guard";

export const routes: Routes = [
  { path: "login", component: LoginComponent, canActivate: [guestGuard] },
  { path: "register", component: RegisterComponent, canActivate: [guestGuard] },
  {
    path: "enedis",
    component: EnedisSettingsComponent,
    canActivate: [authGuard],
  },
  {
    path: "dashboard",
    component: DashboardComponent,
    canActivate: [authGuard],
  },
  { path: "", pathMatch: "full", redirectTo: "login" },
];
