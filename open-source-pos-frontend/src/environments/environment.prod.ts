export const environment = {
  production: true,

  /** Fallback when `app-runtime-config.json` is not deployed alongside the app. */
  apiBaseUrl: 'https://localhost:5001/api' as string | null,

  imageServerUrl: 'https://localhost:9096/',
};
