import { CanActivateFn, Router } from "@angular/router";
import { inject } from "@angular/core";
import { AuthService } from "../services/auth.service";
import { BackendHealthService } from "../services/backend-health.service";

export const authGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const backendHealth = inject(BackendHealthService);

  if (!auth.hasValidSession()) {
    return router.createUrlTree(["/login"], {
      queryParams: { returnUrl: state.url },
    });
  }

  // Force a quick health check before allowing navigation
  const isBackendAccessible = await backendHealth.verifyBackendAccessible();
  if (!isBackendAccessible) {
    return false;
  }

  return true;
};

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.hasValidSession()) {
    return router.createUrlTree(["/dashboard"]);
  }
  return true;
};
