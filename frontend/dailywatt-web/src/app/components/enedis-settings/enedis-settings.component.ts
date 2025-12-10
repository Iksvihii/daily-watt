import { Component, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { EnedisService } from '../../services/enedis.service';
import { ImportJob } from '../../models/enedis.models';

@Component({
  selector: 'app-enedis-settings',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './enedis-settings.component.html',
  styleUrl: './enedis-settings.component.less'
})
export class EnedisSettingsComponent {
  message = signal<string | undefined>(undefined);
  job = signal<ImportJob | undefined>(undefined);
  saving = signal(false);
  importing = signal(false);

  credentialsForm = this.fb.group({
    login: ['', Validators.required],
    password: ['', Validators.required]
  });

  importForm = this.fb.group({
    from: [this.defaultFrom(), Validators.required],
    to: [new Date().toISOString().slice(0, 16), Validators.required]
  });

  constructor(private fb: FormBuilder, private enedis: EnedisService) {}

  saveCredentials() {
    if (this.credentialsForm.invalid) {
      return;
    }
    this.saving.set(true);
    this.enedis.saveCredentials(this.credentialsForm.value as any).subscribe({
      next: () => {
        this.message.set('Credentials saved');
        this.saving.set(false);
      },
      error: err => {
        this.message.set(err.error?.error || 'Failed to save credentials');
        this.saving.set(false);
      }
    });
  }

  startImport() {
    if (this.importForm.invalid) {
      return;
    }
    this.importing.set(true);
    const payload = {
      fromUtc: new Date(this.importForm.value.from as string).toISOString(),
      toUtc: new Date(this.importForm.value.to as string).toISOString()
    };

    this.enedis.createImportJob(payload).subscribe({
      next: job => {
        this.job.set(job);
        this.pollJob(job.id);
      },
      error: err => {
        this.message.set(err.error?.error || 'Failed to start import');
        this.importing.set(false);
      }
    });
  }

  private pollJob(id: string) {
    this.enedis.pollJobUntilDone(id).subscribe(job => {
      this.job.set(job);
      if (job.status === 'Completed' || job.status === 'Failed') {
        this.importing.set(false);
      }
    });
  }

  private defaultFrom(): string {
    const d = new Date();
    d.setDate(d.getDate() - 2);
    return d.toISOString().slice(0, 16);
  }
}
