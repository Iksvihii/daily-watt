import { Component, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ToastService } from "../../services/toast.service";

@Component({
  selector: "app-toast-container",
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-container">
      @for (toast of toastService.toasts(); track toast.id) {
      <div
        class="toast"
        [class.toast-success]="toast.type === 'success'"
        [class.toast-error]="toast.type === 'error'"
        [class.toast-info]="toast.type === 'info'"
        tabindex="0"
        role="alert"
        (click)="toastService.dismiss(toast.id)"
        (keyup.enter)="toastService.dismiss(toast.id)"
        (keyup.space)="toastService.dismiss(toast.id)"
      >
        <span class="toast-message">{{ toast.message }}</span>
        <button
          class="toast-close"
          type="button"
          (click)="toastService.dismiss(toast.id); $event.stopPropagation()"
        >
          ×
        </button>
      </div>
      }
    </div>
  `,
  styles: [
    `
      .toast-container {
        position: fixed;
        top: 1rem;
        right: 1rem;
        z-index: 9999;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        max-width: 400px;
      }

      .toast {
        background: #1e293b;
        color: #f1f5f9;
        padding: 1rem 1.5rem;
        border-radius: 0.5rem;
        box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.3);
        display: flex;
        align-items: center;
        gap: 1rem;
        animation: slideIn 0.3s ease-out;
        cursor: pointer;
        border-left: 4px solid #64748b;
      }

      .toast-success {
        border-left-color: #10b981;
      }

      .toast-error {
        border-left-color: #ef4444;
      }

      .toast-info {
        border-left-color: #3b82f6;
      }

      .toast-message {
        flex: 1;
        font-size: 0.875rem;
      }

      .toast-close {
        background: transparent;
        border: none;
        color: #94a3b8;
        font-size: 1.5rem;
        cursor: pointer;
        padding: 0;
        line-height: 1;
        transition: color 0.2s;
      }

      .toast-close:hover {
        color: #f1f5f9;
      }

      @keyframes slideIn {
        from {
          transform: translateX(100%);
          opacity: 0;
        }
        to {
          transform: translateX(0);
          opacity: 1;
        }
      }
    `,
  ],
})
export class ToastContainerComponent {
  toastService = inject(ToastService);
}
