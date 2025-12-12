import { Injectable, signal } from "@angular/core";

export interface Toast {
  id: number;
  message: string;
  type: "success" | "error" | "info";
  duration?: number;
}

@Injectable({ providedIn: "root" })
export class ToastService {
  private nextId = 1;
  toasts = signal<Toast[]>([]);

  show(
    message: string,
    type: "success" | "error" | "info" = "info",
    duration = 5000
  ) {
    const id = this.nextId++;
    const toast: Toast = { id, message, type, duration };

    this.toasts.update((toasts) => [...toasts, toast]);

    if (duration > 0) {
      setTimeout(() => this.dismiss(id), duration);
    }
  }

  success(message: string, duration = 5000) {
    this.show(message, "success", duration);
  }

  error(message: string, duration = 5000) {
    this.show(message, "error", duration);
  }

  info(message: string, duration = 5000) {
    this.show(message, "info", duration);
  }

  dismiss(id: number) {
    this.toasts.update((toasts) => toasts.filter((t) => t.id !== id));
  }
}
