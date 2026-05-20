import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { of } from 'rxjs';

/** Loaded from /assets/app-runtime-config.json (optional; use PosNetworkSetup tool to generate). */
export interface AppRuntimeConfigJson {
  apiBaseUrl?: string | null;
  apiBaseUrlHttps?: string | null;
  imageServerUrl?: string | null;
  imageServerUrlHttps?: string | null;
}

@Injectable()
export class RuntimeConfigService {
  apiBaseUrl: string | null = null;
  apiBaseUrlHttps: string | null = null;
  imageServerUrl: string | null = null;
  imageServerUrlHttps: string | null = null;

  constructor(private http: HttpClient) {}

  load(): Promise<void> {
    return firstValueFrom(
      this.http.get<AppRuntimeConfigJson>('/assets/app-runtime-config.json').pipe(
        catchError(() => of<AppRuntimeConfigJson | null>(null))
      )
    ).then((cfg) => {
      if (!cfg) {
        return;
      }
      this.apiBaseUrl = cfg.apiBaseUrl?.trim() || null;
      this.apiBaseUrlHttps = cfg.apiBaseUrlHttps?.trim() || null;
      this.imageServerUrl = cfg.imageServerUrl?.trim() || null;
      this.imageServerUrlHttps = cfg.imageServerUrlHttps?.trim() || null;
    });
  }
}

export function initRuntimeConfig(runtime: RuntimeConfigService): () => Promise<void> {
  return () => runtime.load();
}
