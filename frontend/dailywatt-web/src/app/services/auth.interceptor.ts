import { inject } from "@angular/core";
import {
  HttpRequest,
  HttpInterceptorFn,
  HttpResponse,
} from "@angular/common/http";
import { AuthTokenService } from "./auth-token.service";
import { tap } from "rxjs";

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenService = inject(AuthTokenService);
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
    })
  );
};
