import { Component, inject } from "@angular/core";
import { BackendHealthService } from "../../services/backend-health.service";
import { CommonModule } from "@angular/common";

@Component({
  selector: "app-backend-error",
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="backend-error-container">
      <div class="error-content">
        <div class="error-icon">⚠️</div>
        <h1>Backend Indisponible</h1>
        <p>
          Le serveur API n'est pas accessible pour le moment. Veuillez attendre
          quelques instants...
        </p>
        <div class="spinner" *ngIf="backendHealth.isChecking()"></div>
        <p class="status-text" *ngIf="backendHealth.isChecking()">
          Vérification de la connexion...
        </p>
        <p class="status-text" *ngIf="!backendHealth.isChecking()">
          Assurez-vous que l'API est lancée sur
          <code>{{ apiUrl }}</code>
        </p>
      </div>
    </div>
  `,
  styles: `
    .backend-error-container {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto,
        Oxygen, Ubuntu, Cantarell, sans-serif;
    }

    .error-content {
      text-align: center;
      background: white;
      padding: 3rem;
      border-radius: 12px;
      box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
      max-width: 500px;
    }

    .error-icon {
      font-size: 4rem;
      margin-bottom: 1rem;
    }

    h1 {
      font-size: 2rem;
      color: #333;
      margin-bottom: 1rem;
    }

    p {
      color: #666;
      font-size: 1.1rem;
      line-height: 1.6;
      margin-bottom: 1.5rem;
    }

    .spinner {
      display: inline-block;
      width: 40px;
      height: 40px;
      border: 4px solid #f3f3f3;
      border-top: 4px solid #667eea;
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin: 2rem 0;
    }

    @keyframes spin {
      0% {
        transform: rotate(0deg);
      }
      100% {
        transform: rotate(360deg);
      }
    }

    .status-text {
      font-size: 0.95rem;
      color: #999;
      margin-top: 1rem;
    }

    code {
      background: #f5f5f5;
      padding: 0.25rem 0.5rem;
      border-radius: 4px;
      font-family: "Courier New", monospace;
      color: #333;
    }
  `,
})
export class BackendErrorComponent {
  backendHealth = inject(BackendHealthService);
  apiUrl = "http://localhost:5000";
}
