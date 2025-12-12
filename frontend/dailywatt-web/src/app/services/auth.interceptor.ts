import { inject } from "@angular/core";
import {
  HttpRequest,
  HttpInterceptorFn,
  HttpResponse,
  HttpErrorResponse,
} from "@angular/common/http";
import { AuthTokenService } from "./auth-token.service";
import { BackendHealthService } from "./backend-health.service";
import { catchError, tap, throwError } from "rxjs";

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenService = inject(AuthTokenService);
  const backendHealth = inject(BackendHealthService);
  const token = tokenService.getToken();

  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });
  }

  return next(req).pipe(
    tap({
      next: (event) => {
        if (event instanceof HttpResponse && event.status === 401) {
          tokenService.clearToken();
        }
      },
    }),
    catchError((error: HttpErrorResponse) => {
      const status = error.status;
      const isServerDown = status === 0 || status >= 500;

      if (isServerDown) {
        // Mark as down and trigger a single health probe
        backendHealth.markFailureAndProbe();
      }

      return throwError(() => error);
    })
  );
};
